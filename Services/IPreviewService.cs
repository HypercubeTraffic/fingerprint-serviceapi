using FingerprintWebAPI.Models;

namespace FingerprintWebAPI.Services
{
    public interface IPreviewService
    {
        Task<bool> StartPreviewAsync(int channel, int width, int height);
        Task<bool> StopPreviewAsync();
        bool IsPreviewRunning { get; }
        PreviewStatus GetPreviewStatus();
        event EventHandler<FingerprintPreviewData>? PreviewDataReceived;
    }
}
