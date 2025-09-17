using Microsoft.AspNetCore.SignalR;
using FingerprintWebAPI.Hubs;
using FingerprintWebAPI.Models;
using System.Collections.Concurrent;

namespace FingerprintWebAPI.Services
{
    public class WebSocketService : IWebSocketService
    {
        private readonly IHubContext<FingerprintHub> _hubContext;
        private readonly ILogger<WebSocketService> _logger;
        private readonly ConcurrentHashSet<string> _activeConnections = new();

        public bool HasActiveConnections => _activeConnections.Count > 0;

        public WebSocketService(IHubContext<FingerprintHub> hubContext, ILogger<WebSocketService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public void RegisterConnection(string connectionId)
        {
            _activeConnections.Add(connectionId);
            _logger.LogInformation("Registered connection: {ConnectionId}. Total connections: {Count}", connectionId, _activeConnections.Count);
        }

        public void UnregisterConnection(string connectionId)
        {
            _activeConnections.TryRemove(connectionId);
            _logger.LogInformation("Unregistered connection: {ConnectionId}. Total connections: {Count}", connectionId, _activeConnections.Count);
        }

        public async Task SendPreviewDataAsync(FingerprintPreviewData previewData)
        {
            if (!HasActiveConnections)
                return;

            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = "preview",
                    Data = previewData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending preview data");
            }
        }

        public async Task SendMessageAsync(string type, object data)
        {
            if (!HasActiveConnections)
                return;

            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", new WebSocketMessage
                {
                    Type = type,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message: {Type}", type);
            }
        }
    }

    // Helper class for thread-safe HashSet
    public class ConcurrentHashSet<T> where T : notnull
    {
        private readonly HashSet<T> _hashSet = new();
        private readonly object _lock = new();

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _hashSet.Count;
                }
            }
        }

        public void Add(T item)
        {
            lock (_lock)
            {
                _hashSet.Add(item);
            }
        }

        public bool TryRemove(T item)
        {
            lock (_lock)
            {
                return _hashSet.Remove(item);
            }
        }
    }
}
