using SmartHomeDashboard.Models;

namespace SmartHomeDashboard.Services
{
    public class TcpDeviceService
    {
        private readonly ILogger<TcpDeviceService> _logger;
        private readonly TcpServerService _tcpServerService;
        private readonly DeviceDataService _deviceDataService;
        private readonly Dictionary<string, TcpDevice> _devices;

        public TcpDeviceService(ILogger<TcpDeviceService> logger, TcpServerService tcpServerService, DeviceDataService deviceDataService)
        {
            _logger = logger;
            _tcpServerService = tcpServerService;
            _deviceDataService = deviceDataService;
            _devices = new Dictionary<string, TcpDevice>();

            _tcpServerService.OnDeviceConnected += OnDeviceConnected;
            _tcpServerService.OnDeviceDisconnected += OnDeviceDisconnected;
            _tcpServerService.OnTelemetryReceived += OnTelemetryReceived;
        }

        private void OnDeviceConnected(object? sender, TcpDevice device)
        {
            _logger.LogInformation($"设备连接: {device.DeviceName} ({device.DeviceId})");
            _devices[device.DeviceId] = device;
        }

        private void OnDeviceDisconnected(object? sender, TcpDevice device)
        {
            _logger.LogInformation($"设备断开: {device.DeviceName} ({device.DeviceId})");
            if (_devices.ContainsKey(device.DeviceId))
            {
                _devices[device.DeviceId].IsOnline = false;
                _devices[device.DeviceId].LastSeen = DateTime.Now;
            }
        }

        private void OnTelemetryReceived(object? sender, TelemetryData telemetry)
        {
            // 遥测数据已在TcpServerService中处理
        }

        public async Task SendCommandAsync(string deviceId, string command, Dictionary<string, object>? parameters = null)
        {
            await _tcpServerService.SendCommandAsync(deviceId, command, parameters);
        }

        public async Task TurnOnAsync(string deviceId)
        {
            await SendCommandAsync(deviceId, "turn_on");
        }

        public async Task TurnOffAsync(string deviceId)
        {
            await SendCommandAsync(deviceId, "turn_off");
        }

        public async Task SetTemperatureAsync(string deviceId, double temperature)
        {
            var parameters = new Dictionary<string, object> { ["temperature"] = temperature };
            await SendCommandAsync(deviceId, "set_temperature", parameters);
        }

        public async Task SetHumidityAsync(string deviceId, int humidity)
        {
            var parameters = new Dictionary<string, object> { ["humidity"] = humidity };
            await SendCommandAsync(deviceId, "set_humidity", parameters);
        }

        public async Task SetBrightnessAsync(string deviceId, int brightness)
        {
            var parameters = new Dictionary<string, object> { ["brightness"] = brightness };
            await SendCommandAsync(deviceId, "set_brightness", parameters);
        }

        public async Task SetMotorSpeedAsync(string deviceId, int speed)
        {
            var parameters = new Dictionary<string, object> { ["speed"] = speed };
            await SendCommandAsync(deviceId, "set_speed", parameters);
        }

        public async Task LockAsync(string deviceId)
        {
            await SendCommandAsync(deviceId, "lock");
        }

        public async Task UnlockAsync(string deviceId, string code)
        {
            var parameters = new Dictionary<string, object> { ["code"] = code };
            await SendCommandAsync(deviceId, "unlock", parameters);
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