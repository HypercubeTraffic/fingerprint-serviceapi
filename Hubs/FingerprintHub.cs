using Microsoft.AspNetCore.SignalR;
using FingerprintWebAPI.Services;
using FingerprintWebAPI.Models;
using Newtonsoft.Json;

namespace FingerprintWebAPI.Hubs
{
    public class FingerprintHub : Hub
    {
        private readonly ILogger<FingerprintHub> _logger;
        private readonly IFingerprintService _fingerprintService;
        private readonly IPreviewService _previewService;
        private readonly IWebSocketService _webSocketService;

        public FingerprintHub(ILogger<FingerprintHub> logger, IFingerprintService fingerprintService, IPreviewService previewService, IWebSocketService webSocketService)
        {
            _logger = logger;
            _fingerprintService = fingerprintService;
            _previewService = previewService;
            _webSocketService = webSocketService;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            
            // Register connection with WebSocket service
            _webSocketService.RegisterConnection(Context.ConnectionId);
            
            // Send connection confirmation
            await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
            {
                Type = "connection",
                Data = new { message = "Connected to fingerprint service", connectionId = Context.ConnectionId }
            });

            // Send device status
            var deviceStatus = await _fingerprintService.GetDeviceStatusAsync();
            await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
            {
                Type = "status",
                Data = deviceStatus
            });

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            
            // Unregister connection from WebSocket service
            _webSocketService.UnregisterConnection(Context.ConnectionId);
            
            // Stop preview if no more active connections
            if (!_webSocketService.HasActiveConnections)
            {
                await _previewService.StopPreviewAsync();
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string message)
        {
            try
            {
                _logger.LogInformation("Received message from {ConnectionId}: {Message}", Context.ConnectionId, message);
                
                var command = JsonConvert.DeserializeObject<dynamic>(message);
                string commandType = command?.command ?? "";

                switch (commandType.ToLower())
                {
                    case "start_preview":
                        await HandleStartPreview(command);
                        break;

                    case "stop_preview":
                        await HandleStopPreview();
                        break;

                    case "capture":
                        await HandleCapture(command);
                        break;

                    case "capture_template":
                        await HandleCaptureTemplate(command);
                        break;

                    case "split_four_right":
                        await HandleSplitFourRight(command);
                        break;

                    case "split_two_thumbs":
                        await HandleSplitTwoThumbs(command);
                        break;

                    case "capture_roll":
                        await HandleCaptureRoll(command);
                        break;

                    case "compare_templates":
                        await HandleCompareTemplates(command);
                        break;

                    case "capture_finger_type":
                        await HandleCaptureFingerType(command);
                        break;

                    case "set_device_settings":
                        await HandleSetDeviceSettings(command);
                        break;

                    case "play_beep":
                        await HandlePlayBeep(command);
                        break;

                    case "set_led":
                        await HandleSetLed(command);
                        break;

                    case "set_lcd":
                        await HandleSetLcd(command);
                        break;

                    case "set_dry_wet":
                        await HandleSetDryWet(command);
                        break;

                    case "capture_two_thumbs":
                        await HandleCaptureTwoThumbs(command);
                        break;

                    default:
                        await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                        {
                            Type = "error",
                            Data = new { message = $"Unknown command: {commandType}" }
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error processing command: {ex.Message}" }
                });
            }
        }

        private async Task HandleStartPreview(dynamic command)
        {
            try
            {
                int channel = command?.channel ?? 0;
                int width = command?.width ?? 1600;
                int height = command?.height ?? 1500;

                bool success = await _previewService.StartPreviewAsync(channel, width, height);

                if (success)
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                    {
                        Type = "preview_started",
                        Data = new { width, height, channel, fps = 30 }
                    });
                }
                else
                {
                    await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                    {
                        Type = "error",
                        Data = new { message = "Failed to start preview" }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting preview");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error starting preview: {ex.Message}" }
                });
            }
        }

        private async Task HandleStopPreview()
        {
            try
            {
                bool success = await _previewService.StopPreviewAsync();

                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "preview_stopped",
                    Data = new { success }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping preview");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error stopping preview: {ex.Message}" }
                });
            }
        }

        private async Task HandleCapture(dynamic command)
        {
            try
            {
                var request = new CaptureRequest
                {
                    Channel = command?.channel ?? 0,
                    Width = command?.width ?? 1600,
                    Height = command?.height ?? 1500
                };

                var result = await _fingerprintService.CaptureImageAsync(request);

                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "capture_result",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing image");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error capturing image: {ex.Message}" }
                });
            }
        }

        private async Task HandleCaptureTemplate(dynamic command)
        {
            try
            {
                var request = new TemplateRequest
                {
                    Format = command?.format ?? "ISO",
                    Channel = command?.channel ?? 0,
                    Width = command?.width ?? 1600,
                    Height = command?.height ?? 1500
                };

                var result = await _fingerprintService.CaptureTemplateAsync(request);

                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "template_result",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing template");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error capturing template: {ex.Message}" }
                });
            }
        }

        private async Task HandleSplitFourRight(dynamic command)
        {
            try
            {
                var request = new SplitRequest
                {
                    Channel = command?.channel ?? 0,
                    Width = command?.width ?? 1600,
                    Height = command?.height ?? 1500,
                    SplitWidth = command?.splitWidth ?? 300,
                    SplitHeight = command?.splitHeight ?? 400
                };

                var result = await _fingerprintService.SplitFourRightFingersAsync(request);

                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "split_result",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error splitting four right fingers");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error splitting fingers: {ex.Message}" }
                });
            }
        }

        private async Task HandleSplitTwoThumbs(dynamic command)
        {
            try
            {
                var request = new SplitRequest
                {
                    Channel = command?.channel ?? 0,
                    Width = command?.width ?? 1600,
                    Height = command?.height ?? 1500,
                    SplitWidth = command?.splitWidth ?? 300,
                    SplitHeight = command?.splitHeight ?? 400
                };

                var result = await _fingerprintService.SplitTwoThumbsAsync(request);

                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "split_thumbs_result",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error splitting two thumbs");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error splitting thumbs: {ex.Message}" }
                });
            }
        }

        // NEW MISSING HANDLER METHODS FROM ORIGINAL SAMPLE CODE

        private async Task HandleCaptureRoll(dynamic command)
        {
            try
            {
                var request = new RollCaptureRequest
                {
                    Channel = command?.channel ?? 0,
                    Width = command?.width ?? 800,
                    Height = command?.height ?? 750
                };

                var result = await _fingerprintService.CaptureRollFingerprintAsync(request);

                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "roll_capture_result",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing roll fingerprint");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error capturing roll fingerprint: {ex.Message}" }
                });
            }
        }

        private async Task HandleCompareTemplates(dynamic command)
        {
            try
            {
                var request = new CompareTemplatesRequest
                {
                    Template1 = command?.template1 ?? "",
                    Template2 = command?.template2 ?? ""
                };

                var result = await _fingerprintService.CompareTemplatesAsync(request);

                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "compare_result",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing templates");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error comparing templates: {ex.Message}" }
                });
            }
        }

        private async Task HandleCaptureFingerType(dynamic command)
        {
            try
            {
                var request = new FingerTypeRequest
                {
                    FingerType = command?.fingerType ?? 1,
                    Channel = command?.channel ?? 0,
                    Width = command?.width ?? 1600,
                    Height = command?.height ?? 1500
                };

                var result = await _fingerprintService.CaptureFingerTypeAsync(request);

                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "finger_type_result",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing finger type");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error capturing finger type: {ex.Message}" }
                });
            }
        }

        private async Task HandleSetDeviceSettings(dynamic command)
        {
            try
            {
                var request = new DeviceSettingsRequest
                {
                    DryWetLevel = command?.dryWetLevel ?? 4,
                    LedImageIndex = command?.ledImageIndex ?? 0,
                    LcdImageIndex = command?.lcdImageIndex ?? 0,
                    UseBeep = command?.useBeep ?? true
                };

                var result = await _fingerprintService.SetDeviceSettingsAsync(request);

                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "settings_result",
                    Data = new { success = result }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting device settings");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error setting device settings: {ex.Message}" }
                });
            }
        }

        private async Task HandlePlayBeep(dynamic command)
        {
            try
            {
                int beepType = command?.beepType ?? 1;
                var result = await _fingerprintService.PlayBeepAsync(beepType);

                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "beep_result",
                    Data = new { success = result }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error playing beep");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error playing beep: {ex.Message}" }
                });
            }
        }

        private async Task HandleSetLed(dynamic command)
        {
            try
            {
                int imageIndex = command?.imageIndex ?? 0;
                var result = await _fingerprintService.SetLedLightAsync(imageIndex);

                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "led_result",
                    Data = new { success = result }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting LED");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error setting LED: {ex.Message}" }
                });
            }
        }

        private async Task HandleSetLcd(dynamic command)
        {
            try
            {
                int imageIndex = command?.imageIndex ?? 0;
                var result = await _fingerprintService.SetLcdImageAsync(imageIndex);

                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "lcd_result",
                    Data = new { success = result }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting LCD");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error setting LCD: {ex.Message}" }
                });
            }
        }

        private async Task HandleSetDryWet(dynamic command)
        {
            try
            {
                int level = command?.level ?? 4;
                var result = await _fingerprintService.SetFingerDryWetAsync(level);

                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "dry_wet_result",
                    Data = new { success = result }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting dry/wet level");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error setting dry/wet level: {ex.Message}" }
                });
            }
        }

        private async Task HandleCaptureTwoThumbs(dynamic command)
        {
            try
            {
                var request = new CaptureRequest
                {
                    Channel = command?.channel ?? 0,
                    Width = command?.width ?? 1600,
                    Height = command?.height ?? 1500
                };

                // Convert to FingerTypeRequest for two thumbs (type 3)
                var fingerTypeRequest = new FingerTypeRequest
                {
                    FingerType = 3, // Type 3 = Two thumbs
                    Channel = request.Channel,
                    Width = request.Width,
                    Height = request.Height
                };

                var result = await _fingerprintService.CaptureFingerTypeAsync(fingerTypeRequest);

                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "two_thumbs_capture_result",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing two thumbs");
                await Clients.Caller.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "error",
                    Data = new { message = $"Error capturing two thumbs: {ex.Message}" }
                });
            }
        }

        // Removed OnPreviewDataReceived - now using direct WebSocket service calls to avoid disposal issues
    }
}
