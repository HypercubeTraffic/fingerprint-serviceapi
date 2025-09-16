using FingerprintWebAPI.Models;
using System.Diagnostics;

namespace FingerprintWebAPI.Services
{
    public class PreviewService : IPreviewService, IDisposable
    {
        private readonly ILogger<PreviewService> _logger;
        private readonly IFingerprintService _fingerprintService;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _previewTask;
        private bool _isPreviewRunning = false;
        private int _currentChannel = 0;
        private int _currentWidth = 1600;
        private int _currentHeight = 1500;
        private int _currentFps = 0;
        private readonly object _lockObject = new object();

        public bool IsPreviewRunning => _isPreviewRunning;
        public event EventHandler<FingerprintPreviewData>? PreviewDataReceived;

        public PreviewService(ILogger<PreviewService> logger, IFingerprintService fingerprintService)
        {
            _logger = logger;
            _fingerprintService = fingerprintService;
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
                            // Apply vertical flip (critical for proper display)
                            FingerprintDllWrapper.FlipImageVertically(rawData, _currentWidth, _currentHeight);

                            // Convert to base64 for web transmission (raw data, not BMP)
                            string base64Data = Convert.ToBase64String(rawData, 0, _currentWidth * _currentHeight);

                            var previewData = new FingerprintPreviewData
                            {
                                ImageData = base64Data,
                                Width = _currentWidth,
                                Height = _currentHeight,
                                Quality = quality,
                                HasFinger = hasFinger,
                                Fps = _currentFps
                            };

                            // Raise event for WebSocket transmission
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

                        // Control frame rate (aim for ~30 FPS max)
                        await Task.Delay(33, cancellationToken);
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

        public void Dispose()
        {
            StopPreviewAsync().Wait();
        }
    }
}
