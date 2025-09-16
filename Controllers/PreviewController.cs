using Microsoft.AspNetCore.Mvc;
using FingerprintWebAPI.Services;
using FingerprintWebAPI.Models;

namespace FingerprintWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PreviewController : ControllerBase
    {
        private readonly ILogger<PreviewController> _logger;
        private readonly IPreviewService _previewService;

        public PreviewController(ILogger<PreviewController> logger, IPreviewService previewService)
        {
            _logger = logger;
            _previewService = previewService;
        }

        /// <summary>
        /// Start fingerprint preview
        /// </summary>
        [HttpPost("start")]
        public async Task<ActionResult<bool>> StartPreview([FromBody] StartPreviewCommand command)
        {
            try
            {
                _logger.LogInformation("Start preview request: {Width}x{Height} on channel {Channel}", 
                    command.Width, command.Height, command.Channel);
                
                var result = await _previewService.StartPreviewAsync(command.Channel, command.Width, command.Height);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting preview");
                return StatusCode(500, $"Error starting preview: {ex.Message}");
            }
        }

        /// <summary>
        /// Stop fingerprint preview
        /// </summary>
        [HttpPost("stop")]
        public async Task<ActionResult<bool>> StopPreview()
        {
            try
            {
                _logger.LogInformation("Stop preview request");
                var result = await _previewService.StopPreviewAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping preview");
                return StatusCode(500, $"Error stopping preview: {ex.Message}");
            }
        }

        /// <summary>
        /// Get preview status
        /// </summary>
        [HttpGet("status")]
        public ActionResult<PreviewStatus> GetPreviewStatus()
        {
            try
            {
                var status = _previewService.GetPreviewStatus();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting preview status");
                return StatusCode(500, $"Error getting preview status: {ex.Message}");
            }
        }
    }
}
