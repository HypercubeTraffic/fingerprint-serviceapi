using Microsoft.AspNetCore.Mvc;
using FingerprintWebAPI.Services;
using FingerprintWebAPI.Models;

namespace FingerprintWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FingerprintController : ControllerBase
    {
        private readonly ILogger<FingerprintController> _logger;
        private readonly IFingerprintService _fingerprintService;
        private readonly IPreviewService _previewService;

        public FingerprintController(ILogger<FingerprintController> logger, 
            IFingerprintService fingerprintService,
            IPreviewService previewService)
        {
            _logger = logger;
            _fingerprintService = fingerprintService;
            _previewService = previewService;
        }

        /// <summary>
        /// Initialize the fingerprint device
        /// </summary>
        [HttpPost("initialize")]
        public async Task<ActionResult<bool>> Initialize()
        {
            try
            {
                _logger.LogInformation("Initialize device request received");
                var result = await _fingerprintService.InitializeAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing device");
                return StatusCode(500, $"Error initializing device: {ex.Message}");
            }
        }

        /// <summary>
        /// Close the fingerprint device
        /// </summary>
        [HttpPost("close")]
        public async Task<ActionResult<bool>> Close()
        {
            try
            {
                _logger.LogInformation("Close device request received");
                var result = await _fingerprintService.CloseAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing device");
                return StatusCode(500, $"Error closing device: {ex.Message}");
            }
        }

        /// <summary>
        /// Get device status
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<DeviceStatus>> GetStatus()
        {
            try
            {
                var status = await _fingerprintService.GetDeviceStatusAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device status");
                return StatusCode(500, $"Error getting device status: {ex.Message}");
            }
        }

        /// <summary>
        /// Capture a fingerprint image
        /// </summary>
        [HttpPost("capture")]
        public async Task<ActionResult<CaptureResponse>> CaptureImage([FromBody] CaptureRequest request)
        {
            try
            {
                _logger.LogInformation("Capture image request: {Width}x{Height} on channel {Channel}", 
                    request.Width, request.Height, request.Channel);
                
                var result = await _fingerprintService.CaptureImageAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing image");
                return StatusCode(500, $"Error capturing image: {ex.Message}");
            }
        }

        /// <summary>
        /// Capture fingerprint template
        /// </summary>
        [HttpPost("template")]
        public async Task<ActionResult<TemplateResponse>> CaptureTemplate([FromBody] TemplateRequest request)
        {
            try
            {
                _logger.LogInformation("Capture template request: {Format} format, {Width}x{Height} on channel {Channel}", 
                    request.Format, request.Width, request.Height, request.Channel);
                
                var result = await _fingerprintService.CaptureTemplateAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing template");
                return StatusCode(500, $"Error capturing template: {ex.Message}");
            }
        }

        /// <summary>
        /// Split four right fingers
        /// </summary>
        [HttpPost("split/four-right")]
        public async Task<ActionResult<SplitResponse>> SplitFourRightFingers([FromBody] SplitRequest request)
        {
            try
            {
                _logger.LogInformation("Split four right fingers request: {Width}x{Height} -> {SplitWidth}x{SplitHeight}", 
                    request.Width, request.Height, request.SplitWidth, request.SplitHeight);
                
                var result = await _fingerprintService.SplitFourRightFingersAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error splitting four right fingers");
                return StatusCode(500, $"Error splitting fingers: {ex.Message}");
            }
        }

        /// <summary>
        /// Split two thumbs
        /// </summary>
        [HttpPost("split/two-thumbs")]
        public async Task<ActionResult<SplitResponse>> SplitTwoThumbs([FromBody] SplitRequest request)
        {
            try
            {
                _logger.LogInformation("Split two thumbs request: {Width}x{Height} -> {SplitWidth}x{SplitHeight}", 
                    request.Width, request.Height, request.SplitWidth, request.SplitHeight);
                
                var result = await _fingerprintService.SplitTwoThumbsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error splitting two thumbs");
                return StatusCode(500, $"Error splitting thumbs: {ex.Message}");
            }
        }

        // NEW MISSING API ENDPOINTS FROM ORIGINAL SAMPLE CODE

        /// <summary>
        /// Capture roll fingerprint (like btn_roll in original)
        /// </summary>
        [HttpPost("capture/roll")]
        public async Task<ActionResult<RollCaptureResponse>> CaptureRollFingerprint([FromBody] RollCaptureRequest request)
        {
            try
            {
                _logger.LogInformation("Roll capture request: {Width}x{Height} on channel {Channel}", 
                    request.Width, request.Height, request.Channel);
                
                var result = await _fingerprintService.CaptureRollFingerprintAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing roll fingerprint");
                return StatusCode(500, $"Error capturing roll fingerprint: {ex.Message}");
            }
        }

        /// <summary>
        /// Compare two fingerprint templates (like btn_match in original)
        /// </summary>
        [HttpPost("compare")]
        public async Task<ActionResult<CompareTemplatesResponse>> CompareTemplates([FromBody] CompareTemplatesRequest request)
        {
            try
            {
                _logger.LogInformation("Template comparison request");
                
                var result = await _fingerprintService.CompareTemplatesAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing templates");
                return StatusCode(500, $"Error comparing templates: {ex.Message}");
            }
        }

        /// <summary>
        /// Capture specific finger type (like btn_leftfp, btn_rightfp, btn_twofp in original)
        /// </summary>
        [HttpPost("capture/finger-type")]
        public async Task<ActionResult<FingerTypeResponse>> CaptureFingerType([FromBody] FingerTypeRequest request)
        {
            try
            {
                _logger.LogInformation("Finger type capture request: Type {FingerType}, {Width}x{Height} on channel {Channel}", 
                    request.FingerType, request.Width, request.Height, request.Channel);
                
                var result = await _fingerprintService.CaptureFingerTypeAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing finger type");
                return StatusCode(500, $"Error capturing finger type: {ex.Message}");
            }
        }

        /// <summary>
        /// Set device settings (dry/wet, LED, LCD, beep)
        /// </summary>
        [HttpPost("settings")]
        public async Task<ActionResult<bool>> SetDeviceSettings([FromBody] DeviceSettingsRequest settings)
        {
            try
            {
                _logger.LogInformation("Device settings request: DryWet={DryWet}, LED={Led}, LCD={Lcd}", 
                    settings.DryWetLevel, settings.LedImageIndex, settings.LcdImageIndex);
                
                var result = await _fingerprintService.SetDeviceSettingsAsync(settings);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting device settings");
                return StatusCode(500, $"Error setting device settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Play beep sound
        /// </summary>
        [HttpPost("beep/{beepType:int?}")]
        public async Task<ActionResult<bool>> PlayBeep(int beepType = 1)
        {
            try
            {
                var result = await _fingerprintService.PlayBeepAsync(beepType);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error playing beep");
                return StatusCode(500, $"Error playing beep: {ex.Message}");
            }
        }

        /// <summary>
        /// Set LED light
        /// </summary>
        [HttpPost("led/{imageIndex:int}")]
        public async Task<ActionResult<bool>> SetLedLight(int imageIndex)
        {
            try
            {
                var result = await _fingerprintService.SetLedLightAsync(imageIndex);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting LED light");
                return StatusCode(500, $"Error setting LED light: {ex.Message}");
            }
        }

        /// <summary>
        /// Set LCD image
        /// </summary>
        [HttpPost("lcd/{imageIndex:int}")]
        public async Task<ActionResult<bool>> SetLcdImage(int imageIndex)
        {
            try
            {
                var result = await _fingerprintService.SetLcdImageAsync(imageIndex);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting LCD image");
                return StatusCode(500, $"Error setting LCD image: {ex.Message}");
            }
        }

        /// <summary>
        /// Set finger dry/wet level
        /// </summary>
        [HttpPost("dry-wet/{level:int}")]
        public async Task<ActionResult<bool>> SetFingerDryWet(int level)
        {
            try
            {
                var result = await _fingerprintService.SetFingerDryWetAsync(level);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting finger dry/wet level");
                return StatusCode(500, $"Error setting finger dry/wet level: {ex.Message}");
            }
        }

        /// <summary>
        /// Store template for comparison (like fp1list, fp2list in original)
        /// </summary>
        [HttpPost("template/store/{templateId}")]
        public async Task<ActionResult<bool>> StoreTemplate(string templateId, [FromBody] string templateData)
        {
            try
            {
                var result = await _fingerprintService.StoreTemplateAsync(templateId, templateData);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing template");
                return StatusCode(500, $"Error storing template: {ex.Message}");
            }
        }

        /// <summary>
        /// Get stored template
        /// </summary>
        [HttpGet("template/{templateId}")]
        public async Task<ActionResult<string?>> GetStoredTemplate(string templateId)
        {
            try
            {
                var result = await _fingerprintService.GetStoredTemplateAsync(templateId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stored template");
                return StatusCode(500, $"Error getting stored template: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all stored templates
        /// </summary>
        [HttpPost("template/clear")]
        public async Task<ActionResult<bool>> ClearStoredTemplates()
        {
            try
            {
                var result = await _fingerprintService.ClearStoredTemplatesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing stored templates");
                return StatusCode(500, $"Error clearing stored templates: {ex.Message}");
            }
        }

        // NEW: DEDICATED TWO THUMBS CAPTURE ENDPOINT

        /// <summary>
        /// Capture two thumbs specifically (like btn_twofp in original Sample Code)
        /// </summary>
        [HttpPost("capture/two-thumbs")]
        public async Task<ActionResult<FingerTypeResponse>> CaptureTwoThumbs([FromBody] CaptureRequest request)
        {
            try
            {
                _logger.LogInformation("Two thumbs capture request: {Width}x{Height} on channel {Channel}", 
                    request.Width, request.Height, request.Channel);
                
                var fingerTypeRequest = new FingerTypeRequest
                {
                    FingerType = 3, // Type 3 = Two thumbs
                    Channel = request.Channel,
                    Width = request.Width,
                    Height = request.Height
                };

                var result = await _fingerprintService.CaptureFingerTypeAsync(fingerTypeRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing two thumbs");
                return StatusCode(500, $"Error capturing two thumbs: {ex.Message}");
            }
        }

        /// <summary>
        /// NEW CUSTOM ENDPOINT: Capture right four fingers as ISO/ANSI templates (individual fingers)
        /// This endpoint captures the right four fingers and creates templates in the specified format
        /// </summary>
        [HttpPost("capture/right-four-templates")]
        public async Task<ActionResult<RightFourFingersTemplateResponse>> CaptureRightFourFingersTemplates([FromBody] RightFourFingersTemplateRequest request)
        {
            try
            {
                _logger.LogInformation("Right four fingers template capture request: Format={Format}, {Width}x{Height} on channel {Channel}, MinQuality={MinQuality}", 
                    request.Format, request.Width, request.Height, request.Channel, request.MinQuality);
                
                var result = await _fingerprintService.CaptureRightFourFingersTemplatesAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing right four fingers templates");
                return StatusCode(500, $"Error capturing right four fingers templates: {ex.Message}");
            }
        }

        /// <summary>
        /// NEW CUSTOM ENDPOINT: Capture full right four fingers as one combined template
        /// This endpoint captures the right four fingers as a single combined template (no splitting)
        /// </summary>
        [HttpPost("capture/full-right-four")]
        public async Task<ActionResult<FullRightFourFingersResponse>> CaptureFullRightFourFingers([FromBody] FullRightFourFingersRequest request)
        {
            try
            {
                _logger.LogInformation("Full right four fingers template capture request: Format={Format}, {Width}x{Height} on channel {Channel}, MinQuality={MinQuality}", 
                    request.Format, request.Width, request.Height, request.Channel, request.MinQuality);
                
                var result = await _fingerprintService.CaptureFullRightFourFingersAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing full right four fingers template");
                return StatusCode(500, $"Error capturing full right four fingers template: {ex.Message}");
            }
        }
    }
}
