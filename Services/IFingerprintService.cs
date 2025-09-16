using FingerprintWebAPI.Models;

namespace FingerprintWebAPI.Services
{
    public interface IFingerprintService
    {
        Task<bool> InitializeAsync();
        Task<bool> CloseAsync();
        Task<DeviceStatus> GetDeviceStatusAsync();
        Task<CaptureResponse> CaptureImageAsync(CaptureRequest request);
        Task<TemplateResponse> CaptureTemplateAsync(TemplateRequest request);
        Task<SplitResponse> SplitFourRightFingersAsync(SplitRequest request);
        Task<SplitResponse> SplitTwoThumbsAsync(SplitRequest request);
        
        // NEW MISSING METHODS FROM ORIGINAL SAMPLE CODE
        Task<RollCaptureResponse> CaptureRollFingerprintAsync(RollCaptureRequest request);
        Task<CompareTemplatesResponse> CompareTemplatesAsync(CompareTemplatesRequest request);
        Task<FingerTypeResponse> CaptureFingerTypeAsync(FingerTypeRequest request);
        Task<bool> SetDeviceSettingsAsync(DeviceSettingsRequest settings);
        Task<bool> PlayBeepAsync(int beepType = 1);
        Task<bool> SetLedLightAsync(int imageIndex);
        Task<bool> SetLcdImageAsync(int imageIndex);
        Task<bool> SetFingerDryWetAsync(int level);
        
        // Template storage for comparison (like fp1list, fp2list in original)
        Task<bool> StoreTemplateAsync(string templateId, string templateData);
        Task<string?> GetStoredTemplateAsync(string templateId);
        Task<bool> ClearStoredTemplatesAsync();
        
        bool IsDeviceConnected { get; }
        bool IsInitialized { get; }
    }
}
