using FingerprintWebAPI.Models;
using System.Runtime.InteropServices;

namespace FingerprintWebAPI.Services
{
    public class FingerprintService : IFingerprintService, IDisposable
    {
        private readonly ILogger<FingerprintService> _logger;
        private bool _isInitialized = false;
        private bool _isDeviceConnected = false;
        private long _fpDevice = 0;
        private readonly object _lockObject = new object();
        private readonly Dictionary<string, string> _storedTemplates = new Dictionary<string, string>();

        public bool IsDeviceConnected => _isDeviceConnected;
        public bool IsInitialized => _isInitialized;

        public FingerprintService(ILogger<FingerprintService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> InitializeAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        _logger.LogInformation("Initializing fingerprint device...");

                        // Initialize LIVESCAN
                        int result = FingerprintDllWrapper.LIVESCAN_Init();
                        if (result != 1)
                        {
                            _logger.LogError("Failed to initialize LIVESCAN: {Result}", result);
                            return false;
                        }

                        // Initialize MOSAIC
                        result = FingerprintDllWrapper.MOSAIC_Init();
                        if (result != 1)
                        {
                            _logger.LogError("Failed to initialize MOSAIC: {Result}", result);
                            return false;
                        }

                        // NOTE: FPSPLIT_Init is not called in original Fourfinger_Test - DLL handles initialization automatically

                        // Initialize fingerprint algorithm device
                        _fpDevice = FingerprintDllWrapper.ZAZ_FpStdLib_OpenDevice();
                        if (_fpDevice == 0)
                        {
                            _logger.LogWarning("Fingerprint algorithm device not initialized, template operations may not work");
                        }

                        // Set default finger dry/wet level
                        FingerprintDllWrapper.LIVESCAN_SetFingerDryWet(4); // Normal

                        _isDeviceConnected = true;
                        _isInitialized = true;

                        _logger.LogInformation("Fingerprint device initialized successfully");
                        
                        // Beep to indicate successful initialization
                        FingerprintDllWrapper.LIVESCAN_Beep(1);

                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error initializing fingerprint device");
                        return false;
                    }
                }
            });
        }

        public async Task<bool> CloseAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        _logger.LogInformation("Closing fingerprint device...");

                        if (_fpDevice != 0)
                        {
                            FingerprintDllWrapper.ZAZ_FpStdLib_CloseDevice(_fpDevice);
                            _fpDevice = 0;
                        }

                        if (_isDeviceConnected)
                        {
                            FingerprintDllWrapper.FPSPLIT_Uninit();
                            FingerprintDllWrapper.MOSAIC_Close();
                            FingerprintDllWrapper.LIVESCAN_Close();
                        }

                        _isDeviceConnected = false;
                        _isInitialized = false;

                        _logger.LogInformation("Fingerprint device closed successfully");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error closing fingerprint device");
                        return false;
                    }
                }
            });
        }

        public async Task<DeviceStatus> GetDeviceStatusAsync()
        {
            return await Task.Run(() =>
            {
                return new DeviceStatus
                {
                    IsConnected = _isDeviceConnected,
                    IsSupported = _isInitialized,
                    DeviceInfo = _isInitialized ? "BIO600 Fingerprint Scanner" : "Not Connected",
                    ChannelCount = _isDeviceConnected ? FingerprintDllWrapper.LIVESCAN_GetChannelCount() : 0
                };
            });
        }

        public async Task<CaptureResponse> CaptureImageAsync(CaptureRequest request)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        if (!_isDeviceConnected)
                        {
                            return new CaptureResponse
                            {
                                Success = false,
                                Message = "Device not connected"
                            };
                        }

                        _logger.LogInformation("Capturing image: {Width}x{Height} on channel {Channel}", 
                            request.Width, request.Height, request.Channel);

                        // Set capture window
                        FingerprintDllWrapper.LIVESCAN_SetCaptWindow(request.Channel, 0, 0, request.Width, request.Height);

                        // Capture raw data
                        byte[] rawData = new byte[request.Width * request.Height * 2];
                        int result = FingerprintDllWrapper.LIVESCAN_GetFPRawData(request.Channel, rawData);
                        
                        if (result != 1)
                        {
                            return new CaptureResponse
                            {
                                Success = false,
                                Message = $"Failed to capture fingerprint data: {result}"
                            };
                        }

                        // Check quality
                        int quality = FingerprintDllWrapper.MOSAIC_FingerQuality(rawData, request.Width, request.Height);
                        
                        if (quality < 0)
                        {
                            return new CaptureResponse
                            {
                                Success = false,
                                Quality = quality,
                                Message = "Poor image quality or no finger detected"
                            };
                        }

                        // Apply vertical flip (critical for proper display)
                        FingerprintDllWrapper.FlipImageVertically(rawData, request.Width, request.Height);

                        // Create BMP image
                        byte[] bmpData = new byte[1078 + request.Width * request.Height];
                        FingerprintDllWrapper.WriteHead(bmpData, rawData, request.Width, request.Height);

                        // Convert to base64
                        string base64Image = Convert.ToBase64String(bmpData);

                        return new CaptureResponse
                        {
                            Success = true,
                            ImageData = base64Image,
                            Quality = quality,
                            Message = $"Image captured successfully with quality: {quality}"
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error capturing image");
                        return new CaptureResponse
                        {
                            Success = false,
                            Message = $"Error capturing image: {ex.Message}"
                        };
                    }
                }
            });
        }

        public async Task<TemplateResponse> CaptureTemplateAsync(TemplateRequest request)
        {
            try
            {
                if (!_isDeviceConnected)
                {
                    return new TemplateResponse
                    {
                        Success = false,
                        Message = "Device not connected"
                    };
                }

                if (_fpDevice == 0)
                {
                    return new TemplateResponse
                    {
                        Success = false,
                        Message = "Fingerprint algorithm device not initialized"
                    };
                }

                _logger.LogInformation("Capturing {Format} template: {Width}x{Height} on channel {Channel}", 
                    request.Format, request.Width, request.Height, request.Channel);

                // Capture and process image first
                var captureResponse = await CaptureImageAsync(new CaptureRequest 
                { 
                    Channel = request.Channel, 
                    Width = request.Width, 
                    Height = request.Height 
                });

                if (!captureResponse.Success)
                {
                    return new TemplateResponse
                    {
                        Success = false,
                        Message = captureResponse.Message
                    };
                }

                // For template extraction, we need to capture and split directly like Fourfinger_Test line 1280-1289
                lock (_lockObject)
                {
                    // Capture raw data first - Use w*h*2 for template operations like Fourfinger_Test line 1238
                    FingerprintDllWrapper.LIVESCAN_SetCaptWindow(request.Channel, 0, 0, request.Width, request.Height);
                    byte[] rawData = new byte[request.Width * request.Height * 2];
                    int result = FingerprintDllWrapper.LIVESCAN_GetFPRawData(request.Channel, rawData);
                    
                    if (result != 1)
                    {
                        return new TemplateResponse
                        {
                            Success = false,
                            Message = "Failed to capture fingerprint data for template"
                        };
                    }

                    // Check quality
                    int quality = FingerprintDllWrapper.MOSAIC_FingerQuality(rawData, request.Width, request.Height);
                    if (quality < 0)
                    {
                        return new TemplateResponse
                        {
                            Success = false,
                            Message = "Poor image quality for template extraction"
                        };
                    }

                    // Apply image enhancement and flip like Fourfinger_Test
                    FingerprintDllWrapper.ApplyImageEnhancement(rawData, request.Width, request.Height);
                    FingerprintDllWrapper.FlipImageVertically(rawData, request.Width, request.Height);

                    // Split EXACTLY like Fourfinger_Test line 1280-1289
                    int fingerNum = 0;
                    int size = Marshal.SizeOf(typeof(FingerprintDllWrapper.FPSPLIT_INFO));
                    IntPtr infosIntPtr = Marshal.AllocHGlobal(size * 10);
                    IntPtr p = Marshal.AllocHGlobal(256 * 360 * 10);
                    
                    try
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Marshal.WriteIntPtr((IntPtr)((UInt64)infosIntPtr + 24 + (UInt64)(i * size)), (IntPtr)((UInt64)p + (UInt64)(i * 256 * 360)));
                        }
                        
                        int splitResult = FingerprintDllWrapper.FPSPLIT_DoSplit(rawData, request.Width, request.Height, 1, 256, 360, ref fingerNum, infosIntPtr);
                        
                        if (fingerNum <= 0)
                        {
                            return new TemplateResponse
                            {
                                Success = false,
                                Message = "No fingers detected for template extraction"
                            };
                        }

                        // Extract first finger for template (like Fourfinger_Test)
                        IntPtr firstFingerPtr = Marshal.ReadIntPtr((IntPtr)((UInt64)infosIntPtr + 24));
                        byte[] fingerImageData = new byte[256 * 360];
                        Marshal.Copy(firstFingerPtr, fingerImageData, 0, 256 * 360);
                        
                        // Check finger quality
                        int fingerQuality = FingerprintDllWrapper.MOSAIC_FingerQuality(fingerImageData, 256, 360);
                        if (fingerQuality < 30)
                        {
                            return new TemplateResponse
                            {
                                Success = false,
                                Message = $"Finger quality too low for template: {fingerQuality}"
                            };
                        }

                        // Create template from finger image
                        if (request.Format.ToUpper() == "BOTH")
                        {
                            return CreateBothTemplatesFromRaw(fingerImageData, fingerQuality).Result;
                        }
                        else
                        {
                            return CreateSingleTemplateFromRaw(fingerImageData, request.Format, fingerQuality).Result;
                        }
                    }
                    finally
                    {
                        if (p != IntPtr.Zero)
                            Marshal.FreeHGlobal(p);
                        if (infosIntPtr != IntPtr.Zero)
                            Marshal.FreeHGlobal(infosIntPtr);
                    }
                }

                // This section is now handled in the lock above
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing template");
                return new TemplateResponse
                {
                    Success = false,
                    Message = $"Error capturing template: {ex.Message}"
                };
            }
        }

        private async Task<TemplateResponse> CreateSingleTemplateFromRaw(byte[] fingerImageData, string format, int quality)
        {
            return await Task.Run(() =>
            {
                try
                {
                    byte[] template = new byte[1024];
                    int result;

                    if (format.ToUpper() == "ISO")
                    {
                        result = FingerprintDllWrapper.ZAZ_FpStdLib_CreateISOTemplate(_fpDevice, fingerImageData, template);
                    }
                    else if (format.ToUpper() == "ANSI")
                    {
                        result = FingerprintDllWrapper.ZAZ_FpStdLib_CreateANSITemplate(_fpDevice, fingerImageData, template);
                    }
                    else
                    {
                        return new TemplateResponse
                        {
                            Success = false,
                            Message = "Invalid template format. Use ISO or ANSI."
                        };
                    }

                    if (result == 0)
                    {
                        return new TemplateResponse
                        {
                            Success = false,
                            Message = "Failed to create template"
                        };
                    }

                    // OPTIMIZATION: Find actual template end to reduce size
                    int actualTemplateSize = FindActualTemplateSize(template);
                    byte[] optimizedTemplate = new byte[actualTemplateSize];
                    Array.Copy(template, 0, optimizedTemplate, 0, actualTemplateSize);
                    
                    _logger.LogInformation("Template optimized: {Format} template reduced from 1024 to {ActualSize} bytes", format, actualTemplateSize);

                    return new TemplateResponse
                    {
                        Success = true,
                        TemplateFormat = format.ToUpper(),
                        TemplateData = Convert.ToBase64String(optimizedTemplate),
                        TemplateSize = actualTemplateSize,
                        QualityScore = quality,
                        Message = $"{format.ToUpper()} template created successfully (optimized to {actualTemplateSize} bytes)"
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating {Format} template", format);
                    return new TemplateResponse
                    {
                        Success = false,
                        Message = $"Error creating template: {ex.Message}"
                    };
                }
            });
        }

        /// <summary>
        /// Find the actual size of template data by locating the end of meaningful data
        /// This removes padding zeros that are not part of the actual template
        /// </summary>
        private int FindActualTemplateSize(byte[] template)
        {
            // ISO/ANSI templates typically have a header that indicates the actual size
            // For safety, we'll scan backwards from the end to find the last non-zero byte
            // but ensure we keep at least the first 32 bytes (header information)
            
            int minSize = Math.Min(32, template.Length); // Keep at least header
            int actualSize = template.Length;
            
            // Scan backwards to find last meaningful data
            for (int i = template.Length - 1; i >= minSize; i--)
            {
                if (template[i] != 0x00)
                {
                    actualSize = i + 1;
                    break;
                }
            }
            
            // Ensure we don't make it too small - templates need minimum structure
            if (actualSize < minSize)
                actualSize = minSize;
                
            // Round up to nearest 16 bytes for proper alignment
            actualSize = ((actualSize + 15) / 16) * 16;
            
            return Math.Min(actualSize, template.Length);
        }

        private async Task<TemplateResponse> CreateBothTemplatesFromRaw(byte[] fingerImageData, int quality)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var isoResult = CreateSingleTemplateFromRaw(fingerImageData, "ISO", quality).Result;
                    var ansiResult = CreateSingleTemplateFromRaw(fingerImageData, "ANSI", quality).Result;

                    if (!isoResult.Success || !ansiResult.Success)
                    {
                        return new TemplateResponse
                        {
                            Success = false,
                            Message = "Failed to create one or both templates"
                        };
                    }

                    return new TemplateResponse
                    {
                        Success = true,
                        TemplateFormat = "BOTH",
                        QualityScore = quality,
                        Message = "Both ISO and ANSI templates created successfully",
                        IsoTemplate = new TemplateData
                        {
                            Data = isoResult.TemplateData!,
                            Size = isoResult.TemplateSize,
                            Quality = isoResult.QualityScore
                        },
                        AnsiTemplate = new TemplateData
                        {
                            Data = ansiResult.TemplateData!,
                            Size = ansiResult.TemplateSize,
                            Quality = ansiResult.QualityScore
                        }
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating both templates");
                    return new TemplateResponse
                    {
                        Success = false,
                        Message = $"Error creating templates: {ex.Message}"
                    };
                }
            });
        }

        private async Task<TemplateResponse> CreateSingleTemplate(FingerData fingerData, string format)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Convert base64 image back to raw data for template creation
                    byte[] imageBytes = Convert.FromBase64String(fingerData.ImageData);
                    // Skip BMP header (1078 bytes) to get raw image data
                    byte[] rawImageData = new byte[256 * 360];
                    Array.Copy(imageBytes, 1078, rawImageData, 0, rawImageData.Length);

                    byte[] template = new byte[1024];
                    int result;

                    if (format.ToUpper() == "ISO")
                    {
                        result = FingerprintDllWrapper.ZAZ_FpStdLib_CreateISOTemplate(_fpDevice, rawImageData, template);
                    }
                    else if (format.ToUpper() == "ANSI")
                    {
                        result = FingerprintDllWrapper.ZAZ_FpStdLib_CreateANSITemplate(_fpDevice, rawImageData, template);
                    }
                    else
                    {
                        return new TemplateResponse
                        {
                            Success = false,
                            Message = "Invalid template format. Use ISO or ANSI."
                        };
                    }

                    if (result == 0)
                    {
                        return new TemplateResponse
                        {
                            Success = false,
                            Message = "Failed to create template"
                        };
                    }

                    return new TemplateResponse
                    {
                        Success = true,
                        TemplateFormat = format.ToUpper(),
                        TemplateData = Convert.ToBase64String(template),
                        TemplateSize = 1024,
                        QualityScore = fingerData.Quality,
                        Message = $"{format.ToUpper()} template created successfully"
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating {Format} template", format);
                    return new TemplateResponse
                    {
                        Success = false,
                        Message = $"Error creating template: {ex.Message}"
                    };
                }
            });
        }

        private async Task<TemplateResponse> CreateBothTemplates(FingerData fingerData)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var isoResult = CreateSingleTemplate(fingerData, "ISO").Result;
                    var ansiResult = CreateSingleTemplate(fingerData, "ANSI").Result;

                    if (!isoResult.Success || !ansiResult.Success)
                    {
                        return new TemplateResponse
                        {
                            Success = false,
                            Message = "Failed to create one or both templates"
                        };
                    }

                    return new TemplateResponse
                    {
                        Success = true,
                        TemplateFormat = "BOTH",
                        QualityScore = fingerData.Quality,
                        Message = "Both ISO and ANSI templates created successfully",
                        IsoTemplate = new TemplateData
                        {
                            Data = isoResult.TemplateData!,
                            Size = isoResult.TemplateSize,
                            Quality = isoResult.QualityScore
                        },
                        AnsiTemplate = new TemplateData
                        {
                            Data = ansiResult.TemplateData!,
                            Size = ansiResult.TemplateSize,
                            Quality = ansiResult.QualityScore
                        }
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating both templates");
                    return new TemplateResponse
                    {
                        Success = false,
                        Message = $"Error creating templates: {ex.Message}"
                    };
                }
            });
        }

        public async Task<SplitResponse> SplitFourRightFingersAsync(SplitRequest request)
        {
            return await SplitFingersAsync(request, "four_right");
        }

        public async Task<SplitResponse> SplitTwoThumbsAsync(SplitRequest request)
        {
            return await SplitFingersAsync(request, "two_thumbs");
        }

        private async Task<SplitResponse> SplitFingersAsync(SplitRequest request, string splitType)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        if (!_isDeviceConnected)
                        {
                            return new SplitResponse
                            {
                                Success = false,
                                Message = "Device not connected"
                            };
                        }

                        _logger.LogInformation("Splitting {SplitType}: {Width}x{Height} -> {SplitWidth}x{SplitHeight}", 
                            splitType, request.Width, request.Height, request.SplitWidth, request.SplitHeight);

                        // Set capture window
                        FingerprintDllWrapper.LIVESCAN_SetCaptWindow(request.Channel, 0, 0, request.Width, request.Height);

                        // Capture raw data - Use w*h for splitting operations like Fourfinger_Test lines 652-655
                        byte[] rawData = new byte[request.Width * request.Height];
                        int result = FingerprintDllWrapper.LIVESCAN_GetFPRawData(request.Channel, rawData);
                        
                        if (result != 1)
                        {
                            return new SplitResponse
                            {
                                Success = false,
                                Message = $"Failed to capture fingerprint data: {result}"
                            };
                        }

                        // Check quality
                        int quality = FingerprintDllWrapper.MOSAIC_FingerQuality(rawData, request.Width, request.Height);
                        _logger.LogInformation("Image quality for splitting: {Quality}", quality);
                        
                        if (quality < 0)
                        {
                            return new SplitResponse
                            {
                                Success = false,
                                Message = $"Poor image quality or no finger detected. Quality: {quality}"
                            };
                        }

                        // Apply vertical flip
                        FingerprintDllWrapper.FlipImageVertically(rawData, request.Width, request.Height);

                        // Perform split operation
                        int fingerNum = 0;
                        int size = Marshal.SizeOf(typeof(FingerprintDllWrapper.FPSPLIT_INFO));
                        IntPtr infosIntPtr = Marshal.AllocHGlobal(size * 10);
                        IntPtr pti = IntPtr.Zero;

                        try
                        {
                            // Apply image enhancement BEFORE splitting like Fourfinger_Test
                            FingerprintDllWrapper.ApplyImageEnhancement(rawData, request.Width, request.Height);

                            // Allocate memory EXACTLY like Fourfinger_Test line 692-702
                            // CRITICAL: They allocate for 300x400 but use different sizes in WriteIntPtr
                            pti = Marshal.AllocHGlobal(request.SplitWidth * request.SplitHeight * 10);
                            for (int i = 0; i < 10; i++)
                            {
                                Marshal.WriteIntPtr((IntPtr)((UInt64)infosIntPtr + 24 + (UInt64)(i * size)), (IntPtr)((UInt64)pti + (UInt64)(i * request.SplitWidth * request.SplitHeight)));
                            }
                            
                            _logger.LogInformation("Memory allocated: {TotalSize} bytes, Per finger: {PerFinger} bytes", 
                                request.SplitWidth * request.SplitHeight * 10, request.SplitWidth * request.SplitHeight);

                            // Perform the split
                            int splitResult = FingerprintDllWrapper.FPSPLIT_DoSplit(rawData, request.Width, request.Height, 
                                1, request.SplitWidth, request.SplitHeight, ref fingerNum, infosIntPtr);

                            _logger.LogInformation("Split result: {SplitResult}, Finger count: {FingerCount}", splitResult, fingerNum);

                            // CRITICAL FIX: Only check finger count like Fourfinger_Test (lines 703, 1291)
                            // The return value 0 might be normal for this DLL
                            if (fingerNum == 0)
                            {
                                return new SplitResponse
                                {
                                    Success = false,
                                    Message = $"No fingers detected. Finger count: {fingerNum}"
                                };
                            }

                            var response = new SplitResponse
                            {
                                Success = true,
                                SplitWidth = request.SplitWidth,
                                SplitHeight = request.SplitHeight,
                                Message = $"Split completed successfully: {fingerNum} fingers found"
                            };

                            // Extract finger data
                            if (splitType == "two_thumbs")
                            {
                                response.ThumbCount = fingerNum;
                                response.Thumbs = ExtractThumbData(infosIntPtr, fingerNum, size, request.SplitWidth, request.SplitHeight);
                            }
                            else
                            {
                                response.FingerCount = fingerNum;
                                response.Fingers = ExtractFingerData(infosIntPtr, fingerNum, size, request.SplitWidth, request.SplitHeight);
                            }

                            return response;
                        }
                        finally
                        {
                            // Free allocated memory - FIXED FOR 64-BIT
                            if (pti != IntPtr.Zero)
                                Marshal.FreeHGlobal(pti);
                            if (infosIntPtr != IntPtr.Zero)
                                Marshal.FreeHGlobal(infosIntPtr);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error splitting fingers");
                        return new SplitResponse
                        {
                            Success = false,
                            Message = $"Error splitting fingers: {ex.Message}"
                        };
                    }
                }
            });
        }

        private List<FingerData> ExtractFingerData(IntPtr infosIntPtr, int fingerNum, int size, int width, int height)
        {
            var fingers = new List<FingerData>();
            string[] fingerNames = { "right_index", "right_middle", "right_ring", "right_little" };

            for (int i = 0; i < fingerNum && i < fingerNames.Length; i++)
            {
                try
                {
                    // Read FPSPLIT_INFO structure - FIXED FOR 64-BIT
                    IntPtr structPtr = (IntPtr)((UInt64)infosIntPtr + (UInt64)(i * size));
                    var info = Marshal.PtrToStructure<FingerprintDllWrapper.FPSPLIT_INFO>(structPtr);

                    // Read image data - EXACTLY like Fourfinger_Test line 1298
                    IntPtr imagePtr = Marshal.ReadIntPtr((IntPtr)((UInt64)infosIntPtr + (UInt64)(i * size) + 24));
                    byte[] rawImageData = new byte[width * height];
                    Marshal.Copy(imagePtr, rawImageData, 0, rawImageData.Length);

                    // Create BMP image
                    byte[] bmpData = new byte[1078 + width * height];
                    FingerprintDllWrapper.WriteHead(bmpData, rawImageData, width, height);

                    fingers.Add(new FingerData
                    {
                        FingerName = fingerNames[i],
                        ImageData = Convert.ToBase64String(bmpData),
                        X = info.x,
                        Y = info.y,
                        Top = info.top,
                        Left = info.left,
                        Angle = info.angle,
                        Quality = info.quality
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error extracting finger data for finger {Index}", i);
                }
            }

            return fingers;
        }

        private List<ThumbData> ExtractThumbData(IntPtr infosIntPtr, int thumbNum, int size, int width, int height)
        {
            var thumbs = new List<ThumbData>();
            string[] thumbNames = { "left_thumb", "right_thumb" };

            for (int i = 0; i < thumbNum && i < thumbNames.Length; i++)
            {
                try
                {
                    // Read FPSPLIT_INFO structure - FIXED FOR 64-BIT
                    IntPtr structPtr = (IntPtr)((UInt64)infosIntPtr + (UInt64)(i * size));
                    var info = Marshal.PtrToStructure<FingerprintDllWrapper.FPSPLIT_INFO>(structPtr);

                    // Read image data - EXACTLY like Fourfinger_Test line 1298
                    IntPtr imagePtr = Marshal.ReadIntPtr((IntPtr)((UInt64)infosIntPtr + (UInt64)(i * size) + 24));
                    byte[] rawImageData = new byte[width * height];
                    Marshal.Copy(imagePtr, rawImageData, 0, rawImageData.Length);

                    // Create BMP image
                    byte[] bmpData = new byte[1078 + width * height];
                    FingerprintDllWrapper.WriteHead(bmpData, rawImageData, width, height);

                    thumbs.Add(new ThumbData
                    {
                        ThumbName = thumbNames[i],
                        ImageData = Convert.ToBase64String(bmpData),
                        X = info.x,
                        Y = info.y,
                        Top = info.top,
                        Left = info.left,
                        Angle = info.angle,
                        Quality = info.quality
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error extracting thumb data for thumb {Index}", i);
                }
            }

            return thumbs;
        }

        // NEW MISSING METHODS FROM ORIGINAL SAMPLE CODE

        public async Task<RollCaptureResponse> CaptureRollFingerprintAsync(RollCaptureRequest request)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        if (!_isDeviceConnected)
                        {
                            return new RollCaptureResponse
                            {
                                Success = false,
                                Message = "Device not connected"
                            };
                        }

                        _logger.LogInformation("Capturing roll fingerprint: {Width}x{Height} on channel {Channel}", 
                            request.Width, request.Height, request.Channel);

                        int nWidth = request.Width;
                        int nHeight = request.Height;
                        byte[] nfingerImagePtr = new byte[nWidth * nHeight];
                        byte[] zfingerImagePtr = new byte[nWidth * nHeight];

                        // Wait for finger detection (from original roll() method)
                        int ret = 0;
                        while (true)
                        {
                            ret = FingerprintDllWrapper.LIVESCAN_SetCaptWindow(request.Channel, 0, 0, nWidth, nHeight);
                            ret = FingerprintDllWrapper.LIVESCAN_GetFPRawData(request.Channel, nfingerImagePtr);
                            
                            // Apply vertical flip
                            FingerprintDllWrapper.FlipImageVertically(nfingerImagePtr, nWidth, nHeight);
                            
                            if (ret != 1)
                            {
                                return new RollCaptureResponse
                                {
                                    Success = false,
                                    Message = "Failed to capture fingerprint data"
                                };
                            }

                            ret = FingerprintDllWrapper.MOSAIC_IsFinger(nfingerImagePtr, nWidth, nHeight);
                            if (ret > 0)
                            {
                                break;
                            }
                        }

                        // Start mosaic process
                        ret = FingerprintDllWrapper.MOSAIC_Start(nfingerImagePtr, nWidth, nHeight);
                        
                        // Perform mosaic capture
                        while (true)
                        {
                            ret = FingerprintDllWrapper.LIVESCAN_GetFPRawData(request.Channel, zfingerImagePtr);
                            FingerprintDllWrapper.FlipImageVertically(zfingerImagePtr, nWidth, nHeight);
                            
                            if (ret != 1)
                            {
                                FingerprintDllWrapper.MOSAIC_Stop();
                                return new RollCaptureResponse
                                {
                                    Success = false,
                                    Message = "Failed during mosaic capture"
                                };
                            }

                            ret = FingerprintDllWrapper.MOSAIC_DoMosaic(zfingerImagePtr, nWidth, nHeight);
                            if (ret < 0)
                            {
                                FingerprintDllWrapper.MOSAIC_Stop();
                                return new RollCaptureResponse
                                {
                                    Success = false,
                                    Message = "Mosaic process failed"
                                };
                            }

                            if (ret == 0) // Mosaic complete
                            {
                                int quality = FingerprintDllWrapper.MOSAIC_FingerQuality(nfingerImagePtr, nWidth, nHeight);
                                
                                // Create BMP image using WriteHeadRoll
                                byte[] bmpData = new byte[1078 + nWidth * nHeight];
                                FingerprintDllWrapper.WriteHeadRoll(bmpData, nfingerImagePtr, nWidth, nHeight);
                                
                                return new RollCaptureResponse
                                {
                                    Success = true,
                                    ImageData = Convert.ToBase64String(bmpData),
                                    Quality = quality,
                                    Message = $"Roll fingerprint captured successfully with quality: {quality}"
                                };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error capturing roll fingerprint");
                        FingerprintDllWrapper.MOSAIC_Stop(); // Ensure cleanup
                        return new RollCaptureResponse
                        {
                            Success = false,
                            Message = $"Error capturing roll fingerprint: {ex.Message}"
                        };
                    }
                }
            });
        }

        public async Task<CompareTemplatesResponse> CompareTemplatesAsync(CompareTemplatesRequest request)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (_fpDevice == 0)
                    {
                        return new CompareTemplatesResponse
                        {
                            Success = false,
                            Message = "Fingerprint algorithm device not initialized"
                        };
                    }

                    _logger.LogInformation("Comparing fingerprint templates");

                    // Convert base64 templates to byte arrays
                    byte[] template1 = Convert.FromBase64String(request.Template1);
                    byte[] template2 = Convert.FromBase64String(request.Template2);

                    // Compare templates using original logic
                    int score = FingerprintDllWrapper.ZAZ_FpStdLib_CompareTemplates(_fpDevice, template1, template2);
                    bool isMatch = score >= 45; // Original threshold from Sample Code

                    return new CompareTemplatesResponse
                    {
                        Success = true,
                        Score = score,
                        IsMatch = isMatch,
                        Message = isMatch ? "Comparison successful" : $"Comparison score not passed! Score: {score}"
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error comparing templates");
                    return new CompareTemplatesResponse
                    {
                        Success = false,
                        Message = $"Error comparing templates: {ex.Message}"
                    };
                }
            });
        }

        public async Task<FingerTypeResponse> CaptureFingerTypeAsync(FingerTypeRequest request)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        if (!_isDeviceConnected)
                        {
                            return new FingerTypeResponse
                            {
                                Success = false,
                                Message = "Device not connected"
                            };
                        }

                        _logger.LogInformation("Capturing finger type {FingerType}: {Width}x{Height} on channel {Channel}", 
                            request.FingerType, request.Width, request.Height, request.Channel);

                        // Set LED/LCD based on finger type (from original Captureimage method)
                        if (request.FingerType == 1) // Left four
                        {
                            FingerprintDllWrapper.LIVESCAN_SetLedLight(2);
                        }
                        else if (request.FingerType == 2) // Right four  
                        {
                            FingerprintDllWrapper.LIVESCAN_SetLedLight(3);
                        }
                        else if (request.FingerType == 3) // Two thumbs
                        {
                            FingerprintDllWrapper.LIVESCAN_SetLedLight(4);
                        }

                        // Play appropriate audio beep
                        FingerprintDllWrapper.LIVESCAN_Beep(1);

                        // Expected finger count based on type
                        int expectedFingerNum = request.FingerType == 3 ? 2 : 4;
                        
                        // Set capture window
                        FingerprintDllWrapper.LIVESCAN_SetCaptWindow(request.Channel, 0, 0, request.Width, request.Height);

                        // Capture with timeout (10 seconds like original)
                        long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        byte[] data = new byte[request.Width * request.Height * 2];
                        
                        while (true)
                        {
                            // Check timeout
                            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startTime > 10000)
                            {
                                return new FingerTypeResponse
                                {
                                    Success = false,
                                    FingerType = request.FingerType,
                                    Message = "Capture timeout"
                                };
                            }

                            int result = FingerprintDllWrapper.LIVESCAN_GetFPRawData(request.Channel, data);
                            if (result != 1) continue;

                            int quality = FingerprintDllWrapper.MOSAIC_FingerQuality(data, request.Width, request.Height);
                            if (quality < 0) continue;

                            // Apply vertical flip
                            FingerprintDllWrapper.FlipImageVertically(data, request.Width, request.Height);

                            // Perform finger splitting to count fingers
                            int fingerNum = 0;
                            int size = Marshal.SizeOf(typeof(FingerprintDllWrapper.FPSPLIT_INFO));
                            IntPtr infosIntPtr = Marshal.AllocHGlobal(size * 10);

                            IntPtr pti = IntPtr.Zero;
                            try
                            {
                                // FIXED FOR 64-BIT - Use same pattern as Fourfinger_Test
                                pti = Marshal.AllocHGlobal(300 * 400 * 10);
                                for (int i = 0; i < 10; i++)
                                {
                                    Marshal.WriteIntPtr((IntPtr)((UInt64)infosIntPtr + 24 + (UInt64)(i * size)), (IntPtr)((UInt64)pti + (UInt64)(i * 300 * 400)));
                                }

                                // Apply image enhancement
                                FingerprintDllWrapper.ApplyImageEnhancement(data, request.Width, request.Height);

                                int splitResult = FingerprintDllWrapper.FPSPLIT_DoSplit(data, request.Width, request.Height, 
                                    1, 300, 400, ref fingerNum, infosIntPtr);

                                if (fingerNum == expectedFingerNum)
                                {
                                    // Success! Create BMP image
                                    byte[] bmpData = new byte[1078 + request.Width * request.Height];
                                    FingerprintDllWrapper.WriteHead(bmpData, data, request.Width, request.Height);

                                    // Set success LED indication
                                    if (request.FingerType == 1) FingerprintDllWrapper.LIVESCAN_SetLedLight(15);
                                    else if (request.FingerType == 2) FingerprintDllWrapper.LIVESCAN_SetLedLight(17);
                                    else if (request.FingerType == 3) FingerprintDllWrapper.LIVESCAN_SetLedLight(19);

                                    return new FingerTypeResponse
                                    {
                                        Success = true,
                                        FingerType = request.FingerType,
                                        DetectedFingerCount = fingerNum,
                                        ImageData = Convert.ToBase64String(bmpData),
                                        Quality = quality,
                                        Message = "Finger type captured successfully!"
                                    };
                                }
                            }
                            finally
                            {
                                // Free allocated memory - FIXED FOR 64-BIT
                                if (pti != IntPtr.Zero)
                                    Marshal.FreeHGlobal(pti);
                                if (infosIntPtr != IntPtr.Zero)
                                    Marshal.FreeHGlobal(infosIntPtr);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error capturing finger type");
                        return new FingerTypeResponse
                        {
                            Success = false,
                            FingerType = request.FingerType,
                            Message = $"Error capturing finger type: {ex.Message}"
                        };
                    }
                }
            });
        }

        public async Task<bool> SetDeviceSettingsAsync(DeviceSettingsRequest settings)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isDeviceConnected)
                    {
                        _logger.LogError("Cannot set device settings: device not connected");
                        return false;
                    }

                    _logger.LogInformation("Setting device settings: DryWet={DryWet}, LED={Led}, LCD={Lcd}", 
                        settings.DryWetLevel, settings.LedImageIndex, settings.LcdImageIndex);

                    // Set dry/wet finger level
                    FingerprintDllWrapper.LIVESCAN_SetFingerDryWet(settings.DryWetLevel);

                    // Set LED light if specified
                    if (settings.LedImageIndex > 0)
                        FingerprintDllWrapper.LIVESCAN_SetLedLight(settings.LedImageIndex);

                    // Set LCD image if specified  
                    if (settings.LcdImageIndex > 0)
                        FingerprintDllWrapper.LIVESCAN_SetLCDImage(settings.LcdImageIndex);

                    // Play beep if requested
                    if (settings.UseBeep)
                        FingerprintDllWrapper.LIVESCAN_Beep(1);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error setting device settings");
                    return false;
                }
            });
        }

        public async Task<bool> PlayBeepAsync(int beepType = 1)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isDeviceConnected) return false;
                    
                    FingerprintDllWrapper.LIVESCAN_Beep(beepType);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error playing beep");
                    return false;
                }
            });
        }

        public async Task<bool> SetLedLightAsync(int imageIndex)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isDeviceConnected) return false;
                    
                    FingerprintDllWrapper.LIVESCAN_SetLedLight(imageIndex);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error setting LED light");
                    return false;
                }
            });
        }

        public async Task<bool> SetLcdImageAsync(int imageIndex)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isDeviceConnected) return false;
                    
                    FingerprintDllWrapper.LIVESCAN_SetLCDImage(imageIndex);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error setting LCD image");
                    return false;
                }
            });
        }

        public async Task<bool> SetFingerDryWetAsync(int level)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!_isDeviceConnected) return false;
                    
                    FingerprintDllWrapper.LIVESCAN_SetFingerDryWet(level);
                    _logger.LogInformation("Set finger dry/wet level to: {Level}", level);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error setting finger dry/wet level");
                    return false;
                }
            });
        }

        // Template storage methods (like fp1list, fp2list in original)
        public async Task<bool> StoreTemplateAsync(string templateId, string templateData)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lockObject)
                    {
                        _storedTemplates[templateId] = templateData;
                        _logger.LogInformation("Stored template: {TemplateId}", templateId);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error storing template");
                    return false;
                }
            });
        }

        public async Task<string?> GetStoredTemplateAsync(string templateId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lockObject)
                    {
                        return _storedTemplates.TryGetValue(templateId, out string? template) ? template : null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting stored template");
                    return null;
                }
            });
        }

        public async Task<bool> ClearStoredTemplatesAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lockObject)
                    {
                        _storedTemplates.Clear();
                        _logger.LogInformation("Cleared all stored templates");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error clearing stored templates");
                    return false;
                }
            });
        }

        // NEW CUSTOM METHOD FOR RIGHT FOUR FINGERS TEMPLATE CAPTURE
        public async Task<RightFourFingersTemplateResponse> CaptureRightFourFingersTemplatesAsync(RightFourFingersTemplateRequest request)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        if (!_isDeviceConnected)
                        {
                            return new RightFourFingersTemplateResponse
                            {
                                Success = false,
                                Message = "Device not connected"
                            };
                        }

                        if (_fpDevice == 0)
                        {
                            return new RightFourFingersTemplateResponse
                            {
                                Success = false,
                                Message = "Fingerprint algorithm device not initialized for template creation"
                            };
                        }

                        _logger.LogInformation("Capturing right four fingers templates: Format={Format}, {Width}x{Height} on channel {Channel}", 
                            request.Format, request.Width, request.Height, request.Channel);

                        // Set LED for right four fingers (like Fourfinger_Test)
                        FingerprintDllWrapper.LIVESCAN_SetLedLight(3); // Right four fingers LED
                        FingerprintDllWrapper.LIVESCAN_Beep(1); // Audio indication
                        _logger.LogInformation("LED and beep set for right four fingers");

                        // Set capture window
                        FingerprintDllWrapper.LIVESCAN_SetCaptWindow(request.Channel, 0, 0, request.Width, request.Height);
                        _logger.LogInformation("Capture window set: {Width}x{Height}", request.Width, request.Height);

                        // Capture raw data - Use w*h*2 for template operations like Fourfinger_Test line 1238
                        byte[] rawData = new byte[request.Width * request.Height * 2];
                        int result = FingerprintDllWrapper.LIVESCAN_GetFPRawData(request.Channel, rawData);
                        _logger.LogInformation("Raw data capture result: {Result}, Buffer size: {Size}", result, rawData.Length);
                        
                        if (result != 1)
                        {
                            _logger.LogError("Failed to capture fingerprint data, result: {Result}", result);
                            return new RightFourFingersTemplateResponse
                            {
                                Success = false,
                                Message = $"Failed to capture fingerprint data: {result}"
                            };
                        }

                        // Check overall quality
                        int overallQuality = FingerprintDllWrapper.MOSAIC_FingerQuality(rawData, request.Width, request.Height);
                        _logger.LogInformation("Overall image quality: {Quality}", overallQuality);
                        if (overallQuality < 0)
                        {
                            _logger.LogWarning("Poor image quality detected: {Quality}", overallQuality);
                            return new RightFourFingersTemplateResponse
                            {
                                Success = false,
                                Message = $"Poor image quality or no fingers detected: {overallQuality}"
                            };
                        }

                        // Apply image enhancements like Fourfinger_Test (CRITICAL: Apply BEFORE splitting)
                        _logger.LogInformation("Applying image enhancements...");
                        FingerprintDllWrapper.ApplyImageEnhancement(rawData, request.Width, request.Height);
                        FingerprintDllWrapper.FlipImageVertically(rawData, request.Width, request.Height);
                        _logger.LogInformation("Image enhancements applied");

                        // Create BMP for full image
                        byte[] fullImageBmp = new byte[1078 + request.Width * request.Height];
                        FingerprintDllWrapper.WriteHead(fullImageBmp, rawData, request.Width, request.Height);
                        string fullImageBase64 = Convert.ToBase64String(fullImageBmp);

                        // Perform finger splitting EXACTLY like Fourfinger_Test line 1280-1289
                        // CRITICAL: Use fixed 256x360 like the original, not configurable sizes
                        int fingerNum = 0;
                        int size = Marshal.SizeOf(typeof(FingerprintDllWrapper.FPSPLIT_INFO));
                        IntPtr infosIntPtr = Marshal.AllocHGlobal(size * 10);
                        IntPtr p = Marshal.AllocHGlobal(256 * 360 * 10); // Fixed size like original
                        
                        _logger.LogInformation("Memory allocated for splitting: size={Size}, fingerSlots=10", size);
                        
                        try
                        {
                            // EXACT memory setup pattern from Fourfinger_Test line 1284-1287
                            for (int i = 0; i < 10; i++)
                            {
                                Marshal.WriteIntPtr((IntPtr)((UInt64)infosIntPtr + 24 + (UInt64)(i * size)), 
                                    (IntPtr)((UInt64)p + (UInt64)(i * 256 * 360)));
                            }
                            _logger.LogInformation("Memory pointers set up for finger splitting");
                            
                            // CRITICAL: Use 256x360 fixed size like original Fourfinger_Test
                            int splitResult = FingerprintDllWrapper.FPSPLIT_DoSplit(rawData, request.Width, request.Height, 
                                1, 256, 360, ref fingerNum, infosIntPtr);
                            
                            _logger.LogInformation("FPSPLIT_DoSplit completed: result={SplitResult}, fingerNum={FingerNum}", splitResult, fingerNum);
                            
                            // IMPORTANT: Follow Fourfinger_Test pattern - check if ANY fingers detected first
                            if (fingerNum <= 0)
                            {
                                _logger.LogWarning("No fingers detected during splitting");
                                return new RightFourFingersTemplateResponse
                                {
                                    Success = false,
                                    DetectedFingerCount = fingerNum,
                                    Message = $"No fingers detected. Please ensure fingers are properly placed on the scanner."
                                };
                            }
                            
                            _logger.LogInformation("Detected {FingerCount} fingers, processing templates...", fingerNum);

                            var fingerTemplates = new List<FingerTemplateData>();
                            string[] fingerNames = { "right_index", "right_middle", "right_ring", "right_little" };
                            bool allFingersGoodQuality = true;
                            string qualityIssues = "";

                            // Process each detected finger
                            for (int i = 0; i < fingerNum; i++)
                            {
                                try
                                {
                                    // Read FPSPLIT_INFO structure
                                    IntPtr structPtr = (IntPtr)((UInt64)infosIntPtr + (UInt64)(i * size));
                                    var info = Marshal.PtrToStructure<FingerprintDllWrapper.FPSPLIT_INFO>(structPtr);

                                    // Extract finger image data - EXACT pattern from Fourfinger_Test line 1298-1299
                                    IntPtr imagePtr = Marshal.ReadIntPtr((IntPtr)((UInt64)infosIntPtr + (UInt64)(i * size) + 24));
                                    byte[] fingerImageData = new byte[256 * 360]; // Fixed size like original
                                    Marshal.Copy(imagePtr, fingerImageData, 0, 256 * 360);

                                    // Create BMP for individual finger - Fixed 256x360 size (MUST be before quality check)
                                    byte[] fingerBmpData = new byte[1078 + 256 * 360];
                                    FingerprintDllWrapper.WriteHead(fingerBmpData, fingerImageData, 256, 360);

                                    // Check individual finger quality - EXACT pattern from Fourfinger_Test line 1300
                                    int fingerQuality = FingerprintDllWrapper.MOSAIC_FingerQuality(fingerImageData, 256, 360);
                                    _logger.LogInformation("Finger {Index} ({Name}) quality: {Quality}", i, fingerNames[i], fingerQuality);
                                    
                                    if (fingerQuality < request.MinQuality)
                                    {
                                        allFingersGoodQuality = false;
                                        qualityIssues += $"{fingerNames[i]}: {fingerQuality} (min: {request.MinQuality}); ";
                                        _logger.LogWarning("Finger {FingerName} quality {Quality} below minimum {MinQuality}, skipping template creation", fingerNames[i], fingerQuality, request.MinQuality);
                                        
                                        // Still add the finger data but without templates
                                        var lowQualityTemplate = new FingerTemplateData
                                        {
                                            FingerName = fingerNames[i],
                                            FingerIndex = i,
                                            Quality = fingerQuality,
                                            ImageData = Convert.ToBase64String(fingerBmpData),
                                            X = info.x,
                                            Y = info.y,
                                            Top = info.top,
                                            Left = info.left,
                                            Angle = info.angle
                                        };
                                        fingerTemplates.Add(lowQualityTemplate);
                                        continue;
                                    }

                                    // Create templates based on requested format
                                    var fingerTemplate = new FingerTemplateData
                                    {
                                        FingerName = fingerNames[i],
                                        FingerIndex = i,
                                        Quality = fingerQuality,
                                        ImageData = Convert.ToBase64String(fingerBmpData),
                                        X = info.x,
                                        Y = info.y,
                                        Top = info.top,
                                        Left = info.left,
                                        Angle = info.angle
                                    };

                                    // Create ISO template if requested
                                    if (request.Format.ToUpper() == "ISO" || request.Format.ToUpper() == "BOTH")
                                    {
                                        _logger.LogInformation("Creating ISO template for {FingerName}...", fingerNames[i]);
                                        byte[] isoTemplate = new byte[1024];
                                        int isoResult = FingerprintDllWrapper.ZAZ_FpStdLib_CreateISOTemplate(_fpDevice, fingerImageData, isoTemplate);
                                        _logger.LogInformation("ISO template creation result for {FingerName}: {Result}", fingerNames[i], isoResult);
                                        
                                        if (isoResult != 0)
                                        {
                                            // OPTIMIZATION: Optimize template size
                                            int actualIsoSize = FindActualTemplateSize(isoTemplate);
                                            byte[] optimizedIsoTemplate = new byte[actualIsoSize];
                                            Array.Copy(isoTemplate, 0, optimizedIsoTemplate, 0, actualIsoSize);
                                            
                                            fingerTemplate.IsoTemplate = new TemplateData
                                            {
                                                Data = Convert.ToBase64String(optimizedIsoTemplate),
                                                Size = actualIsoSize,
                                                Quality = fingerQuality
                                            };
                                            _logger.LogInformation("ISO template created successfully for {FingerName} (optimized to {Size} bytes)", fingerNames[i], actualIsoSize);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Failed to create ISO template for {FingerName}", fingerNames[i]);
                                        }
                                    }

                                    // Create ANSI template if requested
                                    if (request.Format.ToUpper() == "ANSI" || request.Format.ToUpper() == "BOTH")
                                    {
                                        _logger.LogInformation("Creating ANSI template for {FingerName}...", fingerNames[i]);
                                        byte[] ansiTemplate = new byte[1024];
                                        int ansiResult = FingerprintDllWrapper.ZAZ_FpStdLib_CreateANSITemplate(_fpDevice, fingerImageData, ansiTemplate);
                                        _logger.LogInformation("ANSI template creation result for {FingerName}: {Result}", fingerNames[i], ansiResult);
                                        
                                        if (ansiResult != 0)
                                        {
                                            // OPTIMIZATION: Optimize template size
                                            int actualAnsiSize = FindActualTemplateSize(ansiTemplate);
                                            byte[] optimizedAnsiTemplate = new byte[actualAnsiSize];
                                            Array.Copy(ansiTemplate, 0, optimizedAnsiTemplate, 0, actualAnsiSize);
                                            
                                            fingerTemplate.AnsiTemplate = new TemplateData
                                            {
                                                Data = Convert.ToBase64String(optimizedAnsiTemplate),
                                                Size = actualAnsiSize,
                                                Quality = fingerQuality
                                            };
                                            _logger.LogInformation("ANSI template created successfully for {FingerName} (optimized to {Size} bytes)", fingerNames[i], actualAnsiSize);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Failed to create ANSI template for {FingerName}", fingerNames[i]);
                                        }
                                    }

                                    fingerTemplates.Add(fingerTemplate);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Error processing finger {Index}: {FingerName}", i, fingerNames[i]);
                                }
                            }

                            if (!allFingersGoodQuality)
                            {
                                return new RightFourFingersTemplateResponse
                                {
                                    Success = false,
                                    DetectedFingerCount = fingerNum,
                                    Message = $"Some fingers have poor quality: {qualityIssues.TrimEnd(' ', ';')}",
                                    FingerTemplates = fingerTemplates,
                                    OverallQuality = overallQuality,
                                    ImageData = fullImageBase64
                                };
                            }

                            // Check if all requested templates were created successfully
                            bool allTemplatesCreated = true;
                            foreach (var finger in fingerTemplates)
                            {
                                if (request.Format.ToUpper() == "ISO" && finger.IsoTemplate == null)
                                    allTemplatesCreated = false;
                                else if (request.Format.ToUpper() == "ANSI" && finger.AnsiTemplate == null)
                                    allTemplatesCreated = false;
                                else if (request.Format.ToUpper() == "BOTH" && (finger.IsoTemplate == null || finger.AnsiTemplate == null))
                                    allTemplatesCreated = false;
                            }

                            // Set success LED indication
                            FingerprintDllWrapper.LIVESCAN_SetLedLight(17); // Right four fingers success LED
                            FingerprintDllWrapper.LIVESCAN_Beep(1); // Success beep

                            _logger.LogInformation("Final result: FingerCount={FingerCount}, TemplatesCreated={AllTemplatesCreated}, TemplateListCount={TemplateCount}", 
                                fingerNum, allTemplatesCreated, fingerTemplates.Count);
                            
                            // Log template details
                            foreach (var template in fingerTemplates)
                            {
                                _logger.LogInformation("Template {Name}: HasISO={HasIso}, HasANSI={HasAnsi}, Quality={Quality}", 
                                    template.FingerName, 
                                    template.IsoTemplate != null, 
                                    template.AnsiTemplate != null, 
                                    template.Quality);
                            }

                            return new RightFourFingersTemplateResponse
                            {
                                Success = true, // Always return success if we detected fingers, even if some templates failed
                                DetectedFingerCount = fingerNum,
                                Message = allTemplatesCreated 
                                    ? $"Successfully captured and created {request.Format} templates for all {fingerNum} right fingers"
                                    : $"Captured {fingerNum} fingers. Some templates may have failed due to quality. Check individual results.",
                                FingerTemplates = fingerTemplates,
                                OverallQuality = overallQuality,
                                ImageData = fullImageBase64
                            };
                        }
                        finally
                        {
                            if (p != IntPtr.Zero)
                                Marshal.FreeHGlobal(p);
                            if (infosIntPtr != IntPtr.Zero)
                                Marshal.FreeHGlobal(infosIntPtr);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error capturing right four fingers templates");
                        return new RightFourFingersTemplateResponse
                        {
                            Success = false,
                            Message = $"Error capturing right four fingers templates: {ex.Message}",
                            ErrorDetails = ex.ToString()
                        };
                    }
                }
            });
        }

        // NEW CUSTOM METHOD FOR FULL RIGHT FOUR FINGERS AS ONE TEMPLATE
        public async Task<FullRightFourFingersResponse> CaptureFullRightFourFingersAsync(FullRightFourFingersRequest request)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        if (!_isDeviceConnected)
                        {
                            return new FullRightFourFingersResponse
                            {
                                Success = false,
                                Message = "Device not connected"
                            };
                        }

                        if (_fpDevice == 0)
                        {
                            return new FullRightFourFingersResponse
                            {
                                Success = false,
                                Message = "Fingerprint algorithm device not initialized for template creation"
                            };
                        }

                        _logger.LogInformation("Capturing full right four fingers template: Format={Format}, {Width}x{Height} on channel {Channel}", 
                            request.Format, request.Width, request.Height, request.Channel);

                        // Set LED for right four fingers
                        FingerprintDllWrapper.LIVESCAN_SetLedLight(3); // Right four fingers LED
                        FingerprintDllWrapper.LIVESCAN_Beep(1); // Audio indication
                        _logger.LogInformation("LED and beep set for full right four fingers");

                        // Set capture window
                        FingerprintDllWrapper.LIVESCAN_SetCaptWindow(request.Channel, 0, 0, request.Width, request.Height);
                        _logger.LogInformation("Capture window set: {Width}x{Height}", request.Width, request.Height);

                        // Capture raw data - Use w*h*2 for template operations
                        byte[] rawData = new byte[request.Width * request.Height * 2];
                        int result = FingerprintDllWrapper.LIVESCAN_GetFPRawData(request.Channel, rawData);
                        _logger.LogInformation("Raw data capture result: {Result}, Buffer size: {Size}", result, rawData.Length);
                        
                        if (result != 1)
                        {
                            _logger.LogError("Failed to capture fingerprint data, result: {Result}", result);
                            return new FullRightFourFingersResponse
                            {
                                Success = false,
                                Message = $"Failed to capture fingerprint data: {result}"
                            };
                        }

                        // Check overall quality
                        int overallQuality = FingerprintDllWrapper.MOSAIC_FingerQuality(rawData, request.Width, request.Height);
                        _logger.LogInformation("Overall image quality: {Quality}", overallQuality);
                        
                        if (overallQuality < request.MinQuality)
                        {
                            _logger.LogWarning("Poor image quality detected: {Quality}", overallQuality);
                            return new FullRightFourFingersResponse
                            {
                                Success = false,
                                Message = $"Poor image quality: {overallQuality} (minimum: {request.MinQuality})"
                            };
                        }

                        // Apply image enhancements
                        _logger.LogInformation("Applying image enhancements...");
                        FingerprintDllWrapper.ApplyImageEnhancement(rawData, request.Width, request.Height);
                        FingerprintDllWrapper.FlipImageVertically(rawData, request.Width, request.Height);
                        _logger.LogInformation("Image enhancements applied");

                        // Create BMP for full image
                        byte[] fullImageBmp = new byte[1078 + request.Width * request.Height];
                        FingerprintDllWrapper.WriteHead(fullImageBmp, rawData, request.Width, request.Height);
                        string fullImageBase64 = Convert.ToBase64String(fullImageBmp);

                        // BETTER APPROACH: Split fingers first, then use the BEST QUALITY finger for the "full" template
                        // This follows the same pattern as Fourfinger_Test - templates are always from individual fingers
                        
                        // Perform finger splitting to get individual fingers
                        int fingerNum = 0;
                        int size = Marshal.SizeOf(typeof(FingerprintDllWrapper.FPSPLIT_INFO));
                        IntPtr infosIntPtr = Marshal.AllocHGlobal(size * 10);
                        IntPtr p = Marshal.AllocHGlobal(256 * 360 * 10);
                        
                        byte[]? bestFingerData = null;
                        int bestQuality = 0;
                        string bestFingerName = "";
                        
                        try
                        {
                            // Set up memory pointers like Fourfinger_Test
                            for (int i = 0; i < 10; i++)
                            {
                                Marshal.WriteIntPtr((IntPtr)((UInt64)infosIntPtr + 24 + (UInt64)(i * size)), 
                                    (IntPtr)((UInt64)p + (UInt64)(i * 256 * 360)));
                            }
                            
                            // Split fingers
                            int splitResult = FingerprintDllWrapper.FPSPLIT_DoSplit(rawData, request.Width, request.Height, 
                                1, 256, 360, ref fingerNum, infosIntPtr);
                            
                            _logger.LogInformation("FPSPLIT_DoSplit for full template: result={SplitResult}, fingerNum={FingerNum}", splitResult, fingerNum);
                            
                            if (fingerNum > 0)
                            {
                                // Find the best quality finger to use as the "full" template
                                string[] fingerNames = { "right_index", "right_middle", "right_ring", "right_little" };
                                
                                for (int i = 0; i < fingerNum && i < fingerNames.Length; i++)
                                {
                                    // Extract finger image data
                                    IntPtr imagePtr = Marshal.ReadIntPtr((IntPtr)((UInt64)infosIntPtr + (UInt64)(i * size) + 24));
                                    byte[] fingerImageData = new byte[256 * 360];
                                    Marshal.Copy(imagePtr, fingerImageData, 0, 256 * 360);
                                    
                                    // Check finger quality
                                    int fingerQuality = FingerprintDllWrapper.MOSAIC_FingerQuality(fingerImageData, 256, 360);
                                    _logger.LogInformation("Finger {Index} ({Name}) quality for full template: {Quality}", i, fingerNames[i], fingerQuality);
                                    
                                    if (fingerQuality > bestQuality && fingerQuality >= request.MinQuality)
                                    {
                                        bestQuality = fingerQuality;
                                        bestFingerData = fingerImageData;
                                        bestFingerName = fingerNames[i];
                                    }
                                }
                                
                                if (bestFingerData == null)
                                {
                                    return new FullRightFourFingersResponse
                                    {
                                        Success = false,
                                        Message = $"No fingers meet minimum quality {request.MinQuality} for template creation"
                                    };
                                }
                                
                                _logger.LogInformation("Using {BestFinger} (quality {BestQuality}) for full template creation", bestFingerName, bestQuality);
                            }
                            else
                            {
                                return new FullRightFourFingersResponse
                                {
                                    Success = false,
                                    Message = "No fingers detected for template creation"
                                };
                            }
                        }
                        finally
                        {
                            if (p != IntPtr.Zero)
                                Marshal.FreeHGlobal(p);
                            if (infosIntPtr != IntPtr.Zero)
                                Marshal.FreeHGlobal(infosIntPtr);
                        }

                        var response = new FullRightFourFingersResponse
                        {
                            Success = true,
                            OverallQuality = overallQuality,
                            ImageData = fullImageBase64,
                            Message = "Full right four fingers captured successfully"
                        };

                        // Create ISO template if requested (using best quality finger)
                        if (request.Format.ToUpper() == "ISO" || request.Format.ToUpper() == "BOTH")
                        {
                            _logger.LogInformation("Creating ISO template from best finger ({BestFinger})...", bestFingerName);
                            byte[] isoTemplate = new byte[1024];
                            int isoResult = FingerprintDllWrapper.ZAZ_FpStdLib_CreateISOTemplate(_fpDevice, bestFingerData, isoTemplate);
                            _logger.LogInformation("ISO template creation result: {Result}", isoResult);
                            
                            if (isoResult != 0)
                            {
                                // OPTIMIZATION: Optimize template size
                                int actualIsoSize = FindActualTemplateSize(isoTemplate);
                                byte[] optimizedIsoTemplate = new byte[actualIsoSize];
                                Array.Copy(isoTemplate, 0, optimizedIsoTemplate, 0, actualIsoSize);
                                
                                response.IsoTemplate = new TemplateData
                                {
                                    Data = Convert.ToBase64String(optimizedIsoTemplate),
                                    Size = actualIsoSize,
                                    Quality = overallQuality
                                };
                                _logger.LogInformation("ISO template created successfully for full right four fingers (optimized to {Size} bytes)", actualIsoSize);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to create ISO template for full right four fingers");
                            }
                        }

                        // Create ANSI template if requested (using best quality finger)
                        if (request.Format.ToUpper() == "ANSI" || request.Format.ToUpper() == "BOTH")
                        {
                            _logger.LogInformation("Creating ANSI template from best finger ({BestFinger})...", bestFingerName);
                            byte[] ansiTemplate = new byte[1024];
                            int ansiResult = FingerprintDllWrapper.ZAZ_FpStdLib_CreateANSITemplate(_fpDevice, bestFingerData, ansiTemplate);
                            _logger.LogInformation("ANSI template creation result: {Result}", ansiResult);
                            
                            if (ansiResult != 0)
                            {
                                // OPTIMIZATION: Optimize template size
                                int actualAnsiSize = FindActualTemplateSize(ansiTemplate);
                                byte[] optimizedAnsiTemplate = new byte[actualAnsiSize];
                                Array.Copy(ansiTemplate, 0, optimizedAnsiTemplate, 0, actualAnsiSize);
                                
                                response.AnsiTemplate = new TemplateData
                                {
                                    Data = Convert.ToBase64String(optimizedAnsiTemplate),
                                    Size = actualAnsiSize,
                                    Quality = overallQuality
                                };
                                _logger.LogInformation("ANSI template created successfully for full right four fingers (optimized to {Size} bytes)", actualAnsiSize);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to create ANSI template for full right four fingers");
                            }
                        }

                        // Check if any templates were created
                        bool hasTemplates = (response.IsoTemplate != null) || (response.AnsiTemplate != null);
                        if (!hasTemplates)
                        {
                            response.Success = false;
                            response.Message = "Failed to create any templates from the captured image";
                            return response;
                        }

                        // Set success LED indication
                        FingerprintDllWrapper.LIVESCAN_SetLedLight(17); // Success LED
                        FingerprintDllWrapper.LIVESCAN_Beep(1); // Success beep

                        response.Message = $"Successfully created {request.Format} template(s) for full right four fingers";
                        _logger.LogInformation("Full right four fingers template capture completed successfully");

                        return response;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error capturing full right four fingers template");
                        return new FullRightFourFingersResponse
                        {
                            Success = false,
                            Message = $"Error capturing full right four fingers template: {ex.Message}",
                            ErrorDetails = ex.ToString()
                        };
                    }
                }
            });
        }

        // NEW CUSTOM METHODS FOR LEFT FOUR FINGERS TEMPLATE CAPTURE
        public async Task<LeftFourFingersTemplateResponse> CaptureLeftFourFingersTemplatesAsync(LeftFourFingersTemplateRequest request)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        if (!_isDeviceConnected)
                        {
                            return new LeftFourFingersTemplateResponse
                            {
                                Success = false,
                                Message = "Device not connected"
                            };
                        }

                        if (_fpDevice == 0)
                        {
                            return new LeftFourFingersTemplateResponse
                            {
                                Success = false,
                                Message = "Fingerprint algorithm device not initialized for template creation"
                            };
                        }

                        _logger.LogInformation("Capturing left four fingers templates: Format={Format}, {Width}x{Height} on channel {Channel}", 
                            request.Format, request.Width, request.Height, request.Channel);

                        // Set LED for left four fingers (like Fourfinger_Test)
                        FingerprintDllWrapper.LIVESCAN_SetLedLight(2); // Left four fingers LED
                        FingerprintDllWrapper.LIVESCAN_Beep(1); // Audio indication
                        _logger.LogInformation("LED and beep set for left four fingers");

                        // Set capture window
                        FingerprintDllWrapper.LIVESCAN_SetCaptWindow(request.Channel, 0, 0, request.Width, request.Height);
                        _logger.LogInformation("Capture window set: {Width}x{Height}", request.Width, request.Height);

                        // Capture raw data - Use w*h*2 for template operations like Fourfinger_Test line 1238
                        byte[] rawData = new byte[request.Width * request.Height * 2];
                        int result = FingerprintDllWrapper.LIVESCAN_GetFPRawData(request.Channel, rawData);
                        _logger.LogInformation("Raw data capture result: {Result}, Buffer size: {Size}", result, rawData.Length);
                        
                        if (result != 1)
                        {
                            _logger.LogError("Failed to capture fingerprint data, result: {Result}", result);
                            return new LeftFourFingersTemplateResponse
                            {
                                Success = false,
                                Message = $"Failed to capture fingerprint data: {result}"
                            };
                        }

                        // Check overall quality
                        int overallQuality = FingerprintDllWrapper.MOSAIC_FingerQuality(rawData, request.Width, request.Height);
                        _logger.LogInformation("Overall image quality: {Quality}", overallQuality);
                        if (overallQuality < 0)
                        {
                            _logger.LogWarning("Poor image quality detected: {Quality}", overallQuality);
                            return new LeftFourFingersTemplateResponse
                            {
                                Success = false,
                                Message = $"Poor image quality or no fingers detected: {overallQuality}"
                            };
                        }

                        // Apply image enhancements like Fourfinger_Test (CRITICAL: Apply BEFORE splitting)
                        _logger.LogInformation("Applying image enhancements...");
                        FingerprintDllWrapper.ApplyImageEnhancement(rawData, request.Width, request.Height);
                        FingerprintDllWrapper.FlipImageVertically(rawData, request.Width, request.Height);
                        _logger.LogInformation("Image enhancements applied");

                        // Create BMP for full image
                        byte[] fullImageBmp = new byte[1078 + request.Width * request.Height];
                        FingerprintDllWrapper.WriteHead(fullImageBmp, rawData, request.Width, request.Height);
                        string fullImageBase64 = Convert.ToBase64String(fullImageBmp);

                        // Perform finger splitting EXACTLY like Fourfinger_Test line 1280-1289
                        // CRITICAL: Use fixed 256x360 like the original, not configurable sizes
                        int fingerNum = 0;
                        int size = Marshal.SizeOf(typeof(FingerprintDllWrapper.FPSPLIT_INFO));
                        IntPtr infosIntPtr = Marshal.AllocHGlobal(size * 10);
                        IntPtr p = Marshal.AllocHGlobal(256 * 360 * 10); // Fixed size like original
                        
                        _logger.LogInformation("Memory allocated for splitting: size={Size}, fingerSlots=10", size);
                        
                        try
                        {
                            // EXACT memory setup pattern from Fourfinger_Test line 1284-1287
                            for (int i = 0; i < 10; i++)
                            {
                                Marshal.WriteIntPtr((IntPtr)((UInt64)infosIntPtr + 24 + (UInt64)(i * size)), 
                                    (IntPtr)((UInt64)p + (UInt64)(i * 256 * 360)));
                            }
                            _logger.LogInformation("Memory pointers set up for finger splitting");
                            
                            // CRITICAL: Use 256x360 fixed size like original Fourfinger_Test
                            int splitResult = FingerprintDllWrapper.FPSPLIT_DoSplit(rawData, request.Width, request.Height, 
                                1, 256, 360, ref fingerNum, infosIntPtr);
                            
                            _logger.LogInformation("FPSPLIT_DoSplit completed: result={SplitResult}, fingerNum={FingerNum}", splitResult, fingerNum);
                            
                            // IMPORTANT: Follow Fourfinger_Test pattern - check if ANY fingers detected first
                            if (fingerNum <= 0)
                            {
                                _logger.LogWarning("No fingers detected during splitting");
                                return new LeftFourFingersTemplateResponse
                                {
                                    Success = false,
                                    DetectedFingerCount = fingerNum,
                                    Message = $"No fingers detected. Please ensure fingers are properly placed on the scanner."
                                };
                            }
                            
                            _logger.LogInformation("Detected {FingerCount} fingers, processing templates...", fingerNum);

                            var fingerTemplates = new List<FingerTemplateData>();
                            string[] fingerNames = { "left_index", "left_middle", "left_ring", "left_little" };
                            bool allFingersGoodQuality = true;
                            string qualityIssues = "";

                            // Process each detected finger
                            for (int i = 0; i < fingerNum; i++)
                            {
                                try
                                {
                                    // Read FPSPLIT_INFO structure
                                    IntPtr structPtr = (IntPtr)((UInt64)infosIntPtr + (UInt64)(i * size));
                                    var info = Marshal.PtrToStructure<FingerprintDllWrapper.FPSPLIT_INFO>(structPtr);

                                    // Extract finger image data - EXACT pattern from Fourfinger_Test line 1298-1299
                                    IntPtr imagePtr = Marshal.ReadIntPtr((IntPtr)((UInt64)infosIntPtr + (UInt64)(i * size) + 24));
                                    byte[] fingerImageData = new byte[256 * 360]; // Fixed size like original
                                    Marshal.Copy(imagePtr, fingerImageData, 0, 256 * 360);

                                    // Create BMP for individual finger - Fixed 256x360 size (MUST be before quality check)
                                    byte[] fingerBmpData = new byte[1078 + 256 * 360];
                                    FingerprintDllWrapper.WriteHead(fingerBmpData, fingerImageData, 256, 360);

                                    // Check individual finger quality - EXACT pattern from Fourfinger_Test line 1300
                                    int fingerQuality = FingerprintDllWrapper.MOSAIC_FingerQuality(fingerImageData, 256, 360);
                                    _logger.LogInformation("Finger {Index} ({Name}) quality: {Quality}", i, fingerNames[i], fingerQuality);
                                    
                                    if (fingerQuality < request.MinQuality)
                                    {
                                        allFingersGoodQuality = false;
                                        qualityIssues += $"{fingerNames[i]}: {fingerQuality} (min: {request.MinQuality}); ";
                                        _logger.LogWarning("Finger {FingerName} quality {Quality} below minimum {MinQuality}, skipping template creation", fingerNames[i], fingerQuality, request.MinQuality);
                                        
                                        // Still add the finger data but without templates
                                        var lowQualityTemplate = new FingerTemplateData
                                        {
                                            FingerName = fingerNames[i],
                                            FingerIndex = i,
                                            Quality = fingerQuality,
                                            ImageData = Convert.ToBase64String(fingerBmpData),
                                            X = info.x,
                                            Y = info.y,
                                            Top = info.top,
                                            Left = info.left,
                                            Angle = info.angle
                                        };
                                        fingerTemplates.Add(lowQualityTemplate);
                                        continue;
                                    }

                                    // Create templates based on requested format
                                    var fingerTemplate = new FingerTemplateData
                                    {
                                        FingerName = fingerNames[i],
                                        FingerIndex = i,
                                        Quality = fingerQuality,
                                        ImageData = Convert.ToBase64String(fingerBmpData),
                                        X = info.x,
                                        Y = info.y,
                                        Top = info.top,
                                        Left = info.left,
                                        Angle = info.angle
                                    };

                                    // Create ISO template if requested
                                    if (request.Format.ToUpper() == "ISO" || request.Format.ToUpper() == "BOTH")
                                    {
                                        _logger.LogInformation("Creating ISO template for {FingerName}...", fingerNames[i]);
                                        byte[] isoTemplate = new byte[1024];
                                        int isoResult = FingerprintDllWrapper.ZAZ_FpStdLib_CreateISOTemplate(_fpDevice, fingerImageData, isoTemplate);
                                        _logger.LogInformation("ISO template creation result for {FingerName}: {Result}", fingerNames[i], isoResult);
                                        
                                        if (isoResult != 0)
                                        {
                                            // OPTIMIZATION: Optimize template size
                                            int actualIsoSize = FindActualTemplateSize(isoTemplate);
                                            byte[] optimizedIsoTemplate = new byte[actualIsoSize];
                                            Array.Copy(isoTemplate, 0, optimizedIsoTemplate, 0, actualIsoSize);
                                            
                                            fingerTemplate.IsoTemplate = new TemplateData
                                            {
                                                Data = Convert.ToBase64String(optimizedIsoTemplate),
                                                Size = actualIsoSize,
                                                Quality = fingerQuality
                                            };
                                            _logger.LogInformation("ISO template created successfully for {FingerName} (optimized to {Size} bytes)", fingerNames[i], actualIsoSize);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Failed to create ISO template for {FingerName}", fingerNames[i]);
                                        }
                                    }

                                    // Create ANSI template if requested
                                    if (request.Format.ToUpper() == "ANSI" || request.Format.ToUpper() == "BOTH")
                                    {
                                        _logger.LogInformation("Creating ANSI template for {FingerName}...", fingerNames[i]);
                                        byte[] ansiTemplate = new byte[1024];
                                        int ansiResult = FingerprintDllWrapper.ZAZ_FpStdLib_CreateANSITemplate(_fpDevice, fingerImageData, ansiTemplate);
                                        _logger.LogInformation("ANSI template creation result for {FingerName}: {Result}", fingerNames[i], ansiResult);
                                        
                                        if (ansiResult != 0)
                                        {
                                            // OPTIMIZATION: Optimize template size
                                            int actualAnsiSize = FindActualTemplateSize(ansiTemplate);
                                            byte[] optimizedAnsiTemplate = new byte[actualAnsiSize];
                                            Array.Copy(ansiTemplate, 0, optimizedAnsiTemplate, 0, actualAnsiSize);
                                            
                                            fingerTemplate.AnsiTemplate = new TemplateData
                                            {
                                                Data = Convert.ToBase64String(optimizedAnsiTemplate),
                                                Size = actualAnsiSize,
                                                Quality = fingerQuality
                                            };
                                            _logger.LogInformation("ANSI template created successfully for {FingerName} (optimized to {Size} bytes)", fingerNames[i], actualAnsiSize);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("Failed to create ANSI template for {FingerName}", fingerNames[i]);
                                        }
                                    }

                                    fingerTemplates.Add(fingerTemplate);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Error processing finger {Index}: {FingerName}", i, fingerNames[i]);
                                }
                            }

                            if (!allFingersGoodQuality)
                            {
                                return new LeftFourFingersTemplateResponse
                                {
                                    Success = false,
                                    DetectedFingerCount = fingerNum,
                                    Message = $"Some fingers have poor quality: {qualityIssues.TrimEnd(' ', ';')}",
                                    FingerTemplates = fingerTemplates,
                                    OverallQuality = overallQuality,
                                    ImageData = fullImageBase64
                                };
                            }

                            // Check if all requested templates were created successfully
                            bool allTemplatesCreated = true;
                            foreach (var finger in fingerTemplates)
                            {
                                if (request.Format.ToUpper() == "ISO" && finger.IsoTemplate == null)
                                    allTemplatesCreated = false;
                                else if (request.Format.ToUpper() == "ANSI" && finger.AnsiTemplate == null)
                                    allTemplatesCreated = false;
                                else if (request.Format.ToUpper() == "BOTH" && (finger.IsoTemplate == null || finger.AnsiTemplate == null))
                                    allTemplatesCreated = false;
                            }

                            // Set success LED indication
                            FingerprintDllWrapper.LIVESCAN_SetLedLight(15); // Left four fingers success LED
                            FingerprintDllWrapper.LIVESCAN_Beep(1); // Success beep

                            _logger.LogInformation("Final result: FingerCount={FingerCount}, TemplatesCreated={AllTemplatesCreated}, TemplateListCount={TemplateCount}", 
                                fingerNum, allTemplatesCreated, fingerTemplates.Count);
                            
                            // Log template details
                            foreach (var template in fingerTemplates)
                            {
                                _logger.LogInformation("Template {Name}: HasISO={HasIso}, HasANSI={HasAnsi}, Quality={Quality}", 
                                    template.FingerName, 
                                    template.IsoTemplate != null, 
                                    template.AnsiTemplate != null, 
                                    template.Quality);
                            }

                            return new LeftFourFingersTemplateResponse
                            {
                                Success = true, // Always return success if we detected fingers, even if some templates failed
                                DetectedFingerCount = fingerNum,
                                Message = allTemplatesCreated 
                                    ? $"Successfully captured and created {request.Format} templates for all {fingerNum} left fingers"
                                    : $"Captured {fingerNum} fingers. Some templates may have failed due to quality. Check individual results.",
                                FingerTemplates = fingerTemplates,
                                OverallQuality = overallQuality,
                                ImageData = fullImageBase64
                            };
                        }
                        finally
                        {
                            if (p != IntPtr.Zero)
                                Marshal.FreeHGlobal(p);
                            if (infosIntPtr != IntPtr.Zero)
                                Marshal.FreeHGlobal(infosIntPtr);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error capturing left four fingers templates");
                        return new LeftFourFingersTemplateResponse
                        {
                            Success = false,
                            Message = $"Error capturing left four fingers templates: {ex.Message}",
                            ErrorDetails = ex.ToString()
                        };
                    }
                }
            });
        }

        // NEW CUSTOM METHOD FOR FULL LEFT FOUR FINGERS AS ONE TEMPLATE
        public async Task<FullLeftFourFingersResponse> CaptureFullLeftFourFingersAsync(FullLeftFourFingersRequest request)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        if (!_isDeviceConnected)
                        {
                            return new FullLeftFourFingersResponse
                            {
                                Success = false,
                                Message = "Device not connected"
                            };
                        }

                        if (_fpDevice == 0)
                        {
                            return new FullLeftFourFingersResponse
                            {
                                Success = false,
                                Message = "Fingerprint algorithm device not initialized for template creation"
                            };
                        }

                        _logger.LogInformation("Capturing full left four fingers template: Format={Format}, {Width}x{Height} on channel {Channel}", 
                            request.Format, request.Width, request.Height, request.Channel);

                        // Set LED for left four fingers
                        FingerprintDllWrapper.LIVESCAN_SetLedLight(2); // Left four fingers LED
                        FingerprintDllWrapper.LIVESCAN_Beep(1); // Audio indication
                        _logger.LogInformation("LED and beep set for full left four fingers");

                        // Set capture window
                        FingerprintDllWrapper.LIVESCAN_SetCaptWindow(request.Channel, 0, 0, request.Width, request.Height);
                        _logger.LogInformation("Capture window set: {Width}x{Height}", request.Width, request.Height);

                        // Capture raw data - Use w*h*2 for template operations
                        byte[] rawData = new byte[request.Width * request.Height * 2];
                        int result = FingerprintDllWrapper.LIVESCAN_GetFPRawData(request.Channel, rawData);
                        _logger.LogInformation("Raw data capture result: {Result}, Buffer size: {Size}", result, rawData.Length);
                        
                        if (result != 1)
                        {
                            _logger.LogError("Failed to capture fingerprint data, result: {Result}", result);
                            return new FullLeftFourFingersResponse
                            {
                                Success = false,
                                Message = $"Failed to capture fingerprint data: {result}"
                            };
                        }

                        // Check overall quality
                        int overallQuality = FingerprintDllWrapper.MOSAIC_FingerQuality(rawData, request.Width, request.Height);
                        _logger.LogInformation("Overall image quality: {Quality}", overallQuality);
                        
                        if (overallQuality < request.MinQuality)
                        {
                            _logger.LogWarning("Poor image quality detected: {Quality}", overallQuality);
                            return new FullLeftFourFingersResponse
                            {
                                Success = false,
                                Message = $"Poor image quality: {overallQuality} (minimum: {request.MinQuality})"
                            };
                        }

                        // Apply image enhancements
                        _logger.LogInformation("Applying image enhancements...");
                        FingerprintDllWrapper.ApplyImageEnhancement(rawData, request.Width, request.Height);
                        FingerprintDllWrapper.FlipImageVertically(rawData, request.Width, request.Height);
                        _logger.LogInformation("Image enhancements applied");

                        // Create BMP for full image
                        byte[] fullImageBmp = new byte[1078 + request.Width * request.Height];
                        FingerprintDllWrapper.WriteHead(fullImageBmp, rawData, request.Width, request.Height);
                        string fullImageBase64 = Convert.ToBase64String(fullImageBmp);

                        // BETTER APPROACH: Split fingers first, then use the BEST QUALITY finger for the "full" template
                        // This follows the same pattern as Fourfinger_Test - templates are always from individual fingers
                        
                        // Perform finger splitting to get individual fingers
                        int fingerNum = 0;
                        int size = Marshal.SizeOf(typeof(FingerprintDllWrapper.FPSPLIT_INFO));
                        IntPtr infosIntPtr = Marshal.AllocHGlobal(size * 10);
                        IntPtr p = Marshal.AllocHGlobal(256 * 360 * 10);
                        
                        byte[]? bestFingerData = null;
                        int bestQuality = 0;
                        string bestFingerName = "";
                        
                        try
                        {
                            // Set up memory pointers like Fourfinger_Test
                            for (int i = 0; i < 10; i++)
                            {
                                Marshal.WriteIntPtr((IntPtr)((UInt64)infosIntPtr + 24 + (UInt64)(i * size)), 
                                    (IntPtr)((UInt64)p + (UInt64)(i * 256 * 360)));
                            }
                            
                            // Split fingers
                            int splitResult = FingerprintDllWrapper.FPSPLIT_DoSplit(rawData, request.Width, request.Height, 
                                1, 256, 360, ref fingerNum, infosIntPtr);
                            
                            _logger.LogInformation("FPSPLIT_DoSplit for full template: result={SplitResult}, fingerNum={FingerNum}", splitResult, fingerNum);
                            
                            if (fingerNum > 0)
                            {
                                // Find the best quality finger to use as the "full" template
                                string[] fingerNames = { "left_index", "left_middle", "left_ring", "left_little" };
                                
                                for (int i = 0; i < fingerNum && i < fingerNames.Length; i++)
                                {
                                    // Extract finger image data
                                    IntPtr imagePtr = Marshal.ReadIntPtr((IntPtr)((UInt64)infosIntPtr + (UInt64)(i * size) + 24));
                                    byte[] fingerImageData = new byte[256 * 360];
                                    Marshal.Copy(imagePtr, fingerImageData, 0, 256 * 360);
                                    
                                    // Check finger quality
                                    int fingerQuality = FingerprintDllWrapper.MOSAIC_FingerQuality(fingerImageData, 256, 360);
                                    _logger.LogInformation("Finger {Index} ({Name}) quality for full template: {Quality}", i, fingerNames[i], fingerQuality);
                                    
                                    if (fingerQuality > bestQuality && fingerQuality >= request.MinQuality)
                                    {
                                        bestQuality = fingerQuality;
                                        bestFingerData = fingerImageData;
                                        bestFingerName = fingerNames[i];
                                    }
                                }
                                
                                if (bestFingerData == null)
                                {
                                    return new FullLeftFourFingersResponse
                                    {
                                        Success = false,
                                        Message = $"No fingers meet minimum quality {request.MinQuality} for template creation"
                                    };
                                }
                                
                                _logger.LogInformation("Using {BestFinger} (quality {BestQuality}) for full template creation", bestFingerName, bestQuality);
                            }
                            else
                            {
                                return new FullLeftFourFingersResponse
                                {
                                    Success = false,
                                    Message = "No fingers detected for template creation"
                                };
                            }
                        }
                        finally
                        {
                            if (p != IntPtr.Zero)
                                Marshal.FreeHGlobal(p);
                            if (infosIntPtr != IntPtr.Zero)
                                Marshal.FreeHGlobal(infosIntPtr);
                        }

                        var response = new FullLeftFourFingersResponse
                        {
                            Success = true,
                            OverallQuality = overallQuality,
                            ImageData = fullImageBase64,
                            Message = "Full left four fingers captured successfully"
                        };

                        // Create ISO template if requested (using best quality finger)
                        if (request.Format.ToUpper() == "ISO" || request.Format.ToUpper() == "BOTH")
                        {
                            _logger.LogInformation("Creating ISO template from best finger ({BestFinger})...", bestFingerName);
                            byte[] isoTemplate = new byte[1024];
                            int isoResult = FingerprintDllWrapper.ZAZ_FpStdLib_CreateISOTemplate(_fpDevice, bestFingerData, isoTemplate);
                            _logger.LogInformation("ISO template creation result: {Result}", isoResult);
                            
                            if (isoResult != 0)
                            {
                                // OPTIMIZATION: Optimize template size
                                int actualIsoSize = FindActualTemplateSize(isoTemplate);
                                byte[] optimizedIsoTemplate = new byte[actualIsoSize];
                                Array.Copy(isoTemplate, 0, optimizedIsoTemplate, 0, actualIsoSize);
                                
                                response.IsoTemplate = new TemplateData
                                {
                                    Data = Convert.ToBase64String(optimizedIsoTemplate),
                                    Size = actualIsoSize,
                                    Quality = overallQuality
                                };
                                _logger.LogInformation("ISO template created successfully for full left four fingers (optimized to {Size} bytes)", actualIsoSize);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to create ISO template for full left four fingers");
                            }
                        }

                        // Create ANSI template if requested (using best quality finger)
                        if (request.Format.ToUpper() == "ANSI" || request.Format.ToUpper() == "BOTH")
                        {
                            _logger.LogInformation("Creating ANSI template from best finger ({BestFinger})...", bestFingerName);
                            byte[] ansiTemplate = new byte[1024];
                            int ansiResult = FingerprintDllWrapper.ZAZ_FpStdLib_CreateANSITemplate(_fpDevice, bestFingerData, ansiTemplate);
                            _logger.LogInformation("ANSI template creation result: {Result}", ansiResult);
                            
                            if (ansiResult != 0)
                            {
                                // OPTIMIZATION: Optimize template size
                                int actualAnsiSize = FindActualTemplateSize(ansiTemplate);
                                byte[] optimizedAnsiTemplate = new byte[actualAnsiSize];
                                Array.Copy(ansiTemplate, 0, optimizedAnsiTemplate, 0, actualAnsiSize);
                                
                                response.AnsiTemplate = new TemplateData
                                {
                                    Data = Convert.ToBase64String(optimizedAnsiTemplate),
                                    Size = actualAnsiSize,
                                    Quality = overallQuality
                                };
                                _logger.LogInformation("ANSI template created successfully for full left four fingers (optimized to {Size} bytes)", actualAnsiSize);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to create ANSI template for full left four fingers");
                            }
                        }

                        // Check if any templates were created
                        bool hasTemplates = (response.IsoTemplate != null) || (response.AnsiTemplate != null);
                        if (!hasTemplates)
                        {
                            response.Success = false;
                            response.Message = "Failed to create any templates from the captured image";
                            return response;
                        }

                        // Set success LED indication
                        FingerprintDllWrapper.LIVESCAN_SetLedLight(15); // Success LED
                        FingerprintDllWrapper.LIVESCAN_Beep(1); // Success beep

                        response.Message = $"Successfully created {request.Format} template(s) for full left four fingers";
                        _logger.LogInformation("Full left four fingers template capture completed successfully");

                        return response;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error capturing full left four fingers template");
                        return new FullLeftFourFingersResponse
                        {
                            Success = false,
                            Message = $"Error capturing full left four fingers template: {ex.Message}",
                            ErrorDetails = ex.ToString()
                        };
                    }
                }
            });
        }

        public void Dispose()
        {
            CloseAsync().Wait();
        }
    }
}
