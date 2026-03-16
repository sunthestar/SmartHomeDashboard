using Microsoft.AspNetCore.SignalR;
using SmartHomeDashboard.Models;

namespace SmartHomeDashboard.Hubs
{
    public class DeviceHub : Hub
    {
        private static readonly Dictionary<string, string> _connections = new();

        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"客户端连接: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"客户端断开: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SubscribeToDevice(string deviceId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, deviceId);
            Console.WriteLine($"客户端 {Context.ConnectionId} 订阅设备 {deviceId}");
        }

        public async Task UnsubscribeFromDevice(string deviceId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, deviceId);
        }

        // 服务器调用方法通知所有客户端
        public async Task NotifyDeviceUpdate(string deviceId, object data)
        {
            await Clients.Group(deviceId).SendAsync("DeviceUpdated", deviceId, data);
        }

        public async Task NotifyAllDevicesUpdate(object data)
        {
            await Clients.All.SendAsync("AllDevicesUpdated", data);
        }

        public async Task NotifyTelemetryUpdate(string deviceId, TelemetryData telemetry)
        {
            await Clients.Group(deviceId).SendAsync("TelemetryUpdated", deviceId, telemetry);
        }
    }
}