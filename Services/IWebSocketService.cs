using FingerprintWebAPI.Models;

namespace FingerprintWebAPI.Services
{
    public interface IWebSocketService
    {
        Task SendPreviewDataAsync(FingerprintPreviewData previewData);
        Task SendMessageAsync(string type, object data);
        void RegisterConnection(string connectionId);
        void UnregisterConnection(string connectionId);
        bool HasActiveConnections { get; }
    }
}
