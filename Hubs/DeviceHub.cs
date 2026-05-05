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

        // 服务器调用方法通知所有客户端 - 发送简化版的设备数据
        public async Task NotifyDeviceUpdate(string deviceId, object data)
        {
            await Clients.Group(deviceId).SendAsync("DeviceUpdated", deviceId, data);
        }

        public async Task NotifyAllDevicesUpdate(object data)
        {
            // 确保发送的数据不包含循环引用
            await Clients.All.SendAsync("AllDevicesUpdated", data);
        }

        public async Task NotifyTelemetryUpdate(string deviceId, TelemetryData telemetry)
        {
            await Clients.Group(deviceId).SendAsync("TelemetryUpdated", deviceId, telemetry);
        }

        // 发送设备列表的简化版本（无循环引用）
        public async Task SendDevicesList(List<DeviceModel> devices)
        {
            var simplifiedDevices = devices.Select(d => new
            {
                d.Id,
                d.Name,
                d.DeviceNumber,
                d.FullDeviceId,
                d.RoomIdentifier,
                d.TypeIdentifier,
                d.Icon,
                d.IsOn,
                d.StatusText,
                d.Detail,
                d.Progress,
                d.ProgressColor,
                d.Temperature,
                d.Humidity,
                d.MotorSpeed,
                d.Mode,
                d.Direction,
                d.CreatedAt
            }).ToList();

            await Clients.All.SendAsync("DevicesUpdated", simplifiedDevices);
        }
    }
}