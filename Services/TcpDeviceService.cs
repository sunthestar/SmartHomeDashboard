using SmartHomeDashboard.Models;

namespace SmartHomeDashboard.Services
{
    public class TcpDeviceService
    {
        private readonly ILogger<TcpDeviceService> _logger;
        private readonly TcpServerService _tcpServerService;
        private readonly DeviceDataService _deviceDataService;

        public TcpDeviceService(ILogger<TcpDeviceService> logger, TcpServerService tcpServerService, DeviceDataService deviceDataService)
        {
            _logger = logger;
            _tcpServerService = tcpServerService;
            _deviceDataService = deviceDataService;
        }

        // 辅助方法：将设备ID转换为FullDeviceId
        private async Task<string> GetFullDeviceIdAsync(string deviceId)
        {
            // 如果已经是完整ID格式（包含-），直接返回
            if (deviceId.Contains('-'))
            {
                return deviceId;
            }

            // 尝试转换为数字ID
            if (int.TryParse(deviceId, out int intDeviceId))
            {
                var device = await _deviceDataService.GetDeviceByIdAsync(intDeviceId);
                if (device == null)
                {
                    throw new Exception($"设备不存在 (ID: {deviceId})");
                }
                _logger.LogInformation($"设备ID转换: {deviceId} -> {device.FullDeviceId}");
                return device.FullDeviceId;
            }

            throw new Exception($"无效的设备ID格式: {deviceId}");
        }

        public async Task SendCommandAsync(string deviceId, string command, Dictionary<string, object>? parameters = null)
        {
            var fullDeviceId = await GetFullDeviceIdAsync(deviceId);
            await _tcpServerService.SendCommandAsync(fullDeviceId, command, parameters);
        }

        public async Task TurnOnAsync(string deviceId)
        {
            var fullDeviceId = await GetFullDeviceIdAsync(deviceId);
            await SendCommandAsync(fullDeviceId, "turn_on");
        }

        public async Task TurnOffAsync(string deviceId)
        {
            var fullDeviceId = await GetFullDeviceIdAsync(deviceId);
            await SendCommandAsync(fullDeviceId, "turn_off");
        }

        public async Task SetTemperatureAsync(string deviceId, double temperature)
        {
            var fullDeviceId = await GetFullDeviceIdAsync(deviceId);
            var parameters = new Dictionary<string, object> { ["temperature"] = temperature };
            await SendCommandAsync(fullDeviceId, "set_temperature", parameters);
        }

        public async Task SetHumidityAsync(string deviceId, int humidity)
        {
            var fullDeviceId = await GetFullDeviceIdAsync(deviceId);
            var parameters = new Dictionary<string, object> { ["humidity"] = humidity };
            await SendCommandAsync(fullDeviceId, "set_humidity", parameters);
        }

        public async Task SetBrightnessAsync(string deviceId, int brightness)
        {
            var fullDeviceId = await GetFullDeviceIdAsync(deviceId);
            var parameters = new Dictionary<string, object> { ["brightness"] = brightness };
            await SendCommandAsync(fullDeviceId, "set_brightness", parameters);
        }

        public async Task SetMotorSpeedAsync(string deviceId, int speed)
        {
            var fullDeviceId = await GetFullDeviceIdAsync(deviceId);
            var parameters = new Dictionary<string, object> { ["speed"] = speed };
            await SendCommandAsync(fullDeviceId, "set_speed", parameters);
        }

        public async Task LockAsync(string deviceId)
        {
            var fullDeviceId = await GetFullDeviceIdAsync(deviceId);
            await SendCommandAsync(fullDeviceId, "lock");
        }

        public async Task UnlockAsync(string deviceId, string code)
        {
            var fullDeviceId = await GetFullDeviceIdAsync(deviceId);
            var parameters = new Dictionary<string, object> { ["code"] = code };
            await SendCommandAsync(fullDeviceId, "unlock", parameters);
        }

        public TcpDevice? GetDevice(string deviceId)
        {
            return _tcpServerService.GetDevice(deviceId);
        }

        public List<TcpDevice> GetAllDevices()
        {
            return _tcpServerService.GetAllDevices();
        }

        public List<TcpDevice> GetOnlineDevices()
        {
            return _tcpServerService.GetAllDevices().Where(d => d.IsOnline).ToList();
        }
    }
}