using FingerprintWebAPI.Models;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.NetworkInformation;

namespace FingerprintWebAPI.Services
{
    public class PreviewService : IPreviewService, IDisposable
    {
        private readonly ILogger<PreviewService> _logger;
        private readonly IFingerprintService _fingerprintService;
        private readonly IWebSocketService _webSocketService;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _previewTask;
        private bool _isPreviewRunning = false;
        private int _currentChannel = 0;
        private int _currentWidth = 1600;
        private int _currentHeight = 1500;
        private int _currentFps = 0;
        private readonly object _lockObject = new object();
        private long _lastNetworkCheck = 0;
        private bool _isNetworkConnection = false;
        private int _adaptiveDelay = 33; // Start with 30 FPS

        public bool IsPreviewRunning => _isPreviewRunning;
        public event EventHandler<FingerprintPreviewData>? PreviewDataReceived;

        public PreviewService(ILogger<PreviewService> logger, IFingerprintService fingerprintService, IWebSocketService webSocketService)
        {
            _logger = logger;
            _fingerprintService = fingerprintService;
            _webSocketService = webSocketService;
        }

        public Task<bool> StartPreviewAsync(int channel, int width, int height)
        {
            lock (_lockObject)
            {
                if (_isPreviewRunning)
                {
                    _logger.LogWarning("Preview is already running");
                    return Task.FromResult(false);
                }

                if (!_fingerprintService.IsDeviceConnected)
                {
                    _logger.LogError("Cannot start preview: device not connected");
                    return Task.FromResult(false);
                }

                _logger.LogInformation("Starting preview: {Width}x{Height} on channel {Channel}", width, height, channel);

                _currentChannel = channel;
                _currentWidth = width;
                _currentHeight = height;
                _currentFps = 0;
                
                // Detect if this is a network connection and adapt performance
                DetectNetworkConnection();

                _cancellationTokenSource = new CancellationTokenSource();
                _previewTask = Task.Run(() => PreviewLoop(_cancellationTokenSource.Token));
                _isPreviewRunning = true;

                return Task.FromResult(true);
            }
        }

        public async Task<bool> StopPreviewAsync()
        {
            lock (_lockObject)
            {
                if (!_isPreviewRunning)
                {
                    _logger.LogWarning("Preview is not running");
                    return false;
                }

                _logger.LogInformation("Stopping preview");

                _cancellationTokenSource?.Cancel();
                _isPreviewRunning = false;
            }

            // Wait for the preview task to complete outside the lock
            if (_previewTask != null)
            {
                try
                {
                    await _previewTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelling
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping preview task");
                }
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _previewTask = null;
            _currentFps = 0;

            _logger.LogInformation("Preview stopped");
            return true;
        }

        public PreviewStatus GetPreviewStatus()
        {
            return new PreviewStatus
            {
                IsRunning = _isPreviewRunning,
                CurrentFps = _currentFps,
                Width = _currentWidth,
                Height = _currentHeight
            };
        }

        private async Task PreviewLoop(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            int frameCount = 0;
            long lastFpsUpdate = 0;

            try
            {
                // Set capture window
                FingerprintDllWrapper.LIVESCAN_SetCaptWindow(_currentChannel, 0, 0, _currentWidth, _currentHeight);

                while (!cancellationToken.IsCancellationRequested && _fingerprintService.IsDeviceConnected)
                {
                    try
                    {
                        // Capture raw data
                        byte[] rawData = new byte[_currentWidth * _currentHeight * 2];
                        int result = FingerprintDllWrapper.LIVESCAN_GetFPRawData(_currentChannel, rawData);

                        if (result != 1)
                        {
                            _logger.LogWarning("Failed to get fingerprint raw data: {Result}", result);
                            await Task.Delay(100, cancellationToken); // Wait before retrying
                            continue;
                        }

                        // Check quality and finger presence
                        int quality = FingerprintDllWrapper.MOSAIC_FingerQuality(rawData, _currentWidth, _currentHeight);
                        bool hasFinger = FingerprintDllWrapper.MOSAIC_IsFinger(rawData, _currentWidth, _currentHeight) > 0;

                        // Only send frames with valid quality or for debugging purposes
                        if (quality >= -10) // Allow some negative values for debugging
                        {
                            // Note: Removed vertical flip to test if it's causing upside-down display
                            // FingerprintDllWrapper.FlipImageVertically(rawData, _currentWidth, _currentHeight);

                            // Adaptive compression and frame rate based on network conditions
                            string base64Data;
                            
                            // Check if we're dealing with a network connection (not localhost)
                            if (_isNetworkConnection)
                            {
                                // For network connections, compress the data and reduce resolution if needed
                                base64Data = await CompressImageDataAsync(rawData, _currentWidth, _currentHeight);
                            }
                            else
                            {
                                // For localhost, send raw data as before
                                base64Data = Convert.ToBase64String(rawData, 0, _currentWidth * _currentHeight);
                            }

                            var previewData = new FingerprintPreviewData
                            {
                                ImageData = base64Data,
                                Width = _currentWidth,
                                Height = _currentHeight,
                                Quality = quality,
                                HasFinger = hasFinger,
                                Fps = _currentFps
                            };

                            // Send directly through WebSocket service to avoid disposal issues
                            await _webSocketService.SendPreviewDataAsync(previewData);
                            
                            // Also raise event for any other subscribers
                            PreviewDataReceived?.Invoke(this, previewData);
                        }

                        // Update FPS counter
                        frameCount++;
                        long currentTime = stopwatch.ElapsedMilliseconds;
                        if (currentTime - lastFpsUpdate >= 1000) // Update every second
                        {
                            _currentFps = (int)(frameCount * 1000.0 / (currentTime - lastFpsUpdate));
                            frameCount = 0;
                            lastFpsUpdate = currentTime;
                        }

                        // Adaptive frame rate control based on network conditions
                        await Task.Delay(_adaptiveDelay, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in preview loop");
                        await Task.Delay(100, cancellationToken); // Wait before retrying
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Preview loop cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in preview loop");
            }
            finally
            {
                stopwatch.Stop();
                _logger.LogInformation("Preview loop ended");
            }
        }

        private void DetectNetworkConnection()
        {
            try
            {
                // Check if we're serving on network IP (192.168.x.x) in addition to localhost
                var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "";
                var isServingNetwork = urls.Contains("192.168.") || urls.Contains("0.0.0.0");
                
                // For dual binding (localhost + network IP), we'll use adaptive mode
                // This provides good performance for localhost while being network-friendly
                if (isServingNetwork && _webSocketService.HasActiveConnections)
                {
                    _isNetworkConnection = true;
                    _adaptiveDelay = 40; // 25 FPS - balanced for both localhost and network
                    _logger.LogInformation("Dual binding detected (localhost + 192.168.100.162) - using balanced performance mode");
                }
                else
                {
                    _isNetworkConnection = false;
                    _adaptiveDelay = 33; // 30 FPS for localhost only
                    _logger.LogInformation("Localhost-only connection - using high performance mode");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not detect connection type, using default settings");
                _adaptiveDelay = 40; // Conservative default
            }
        }

        private async Task<string> CompressImageDataAsync(byte[] rawData, int width, int height)
        {
            try
            {
                // For network connections, we'll compress the image data
                // First, reduce the data size by taking only every other byte (reduces resolution by half)
                int reducedSize = rawData.Length / 2;
                byte[] reducedData = new byte[reducedSize];
                
                for (int i = 0; i < reducedSize; i++)
                {
                    reducedData[i] = rawData[i * 2]; // Take every other byte
                }

                // Then compress using GZip
                using (var memoryStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                    {
                        await gzipStream.WriteAsync(reducedData, 0, reducedData.Length);
                    }
                    
                    byte[] compressedData = memoryStream.ToArray();
                    
                    // Log compression ratio for monitoring
                    double compressionRatio = (double)compressedData.Length / rawData.Length;
                    if (DateTime.Now.Millisecond < 100) // Log occasionally to avoid spam
                    {
                        _logger.LogDebug("Image compressed: {Original}KB -> {Compressed}KB (ratio: {Ratio:P1})", 
                            rawData.Length / 1024, compressedData.Length / 1024, compressionRatio);
                    }
                    
                    return Convert.ToBase64String(compressedData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to compress image data, falling back to raw data");
                // Fallback to raw data if compression fails
                return Convert.ToBase64String(rawData, 0, width * height);
            }
        }

        public void Dispose()
        {
            StopPreviewAsync().Wait();
        }
    }
}
