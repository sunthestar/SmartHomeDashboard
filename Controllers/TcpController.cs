using Microsoft.AspNetCore.Mvc;
using SmartHomeDashboard.Services;
using SmartHomeDashboard.Models;

namespace SmartHomeDashboard.Controllers
{
    [Route("api/tcp")]
    [ApiController]
    public class TcpController : ControllerBase
    {
        private readonly TcpServerService _tcpServerService;
        private readonly TcpDeviceService _tcpDeviceService;
        private readonly DeviceDataService _deviceDataService;
        private readonly ILogger<TcpController> _logger;

        public TcpController(
            TcpServerService tcpServerService,
            TcpDeviceService tcpDeviceService,
            DeviceDataService deviceDataService,
            ILogger<TcpController> logger)
        {
            _tcpServerService = tcpServerService;
            _tcpDeviceService = tcpDeviceService;
            _deviceDataService = deviceDataService;
            _logger = logger;
        }

        /// <summary>
        /// 获取TCP服务器状态
        /// </summary>
        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            var devices = _tcpDeviceService.GetAllDevices();
            return Ok(new
            {
                success = true,
                isRunning = true,
                onlineDevices = devices.Count(d => d.IsOnline),
                totalDevices = devices.Count
            });
        }

        /// <summary>
        /// 获取所有TCP设备
        /// </summary>
        [HttpGet("devices")]
        public IActionResult GetDevices()
        {
            var devices = _tcpDeviceService.GetAllDevices();
            return Ok(new { success = true, devices });
        }

        /// <summary>
        /// 获取在线TCP设备
        /// </summary>
        [HttpGet("devices/online")]
        public IActionResult GetOnlineDevices()
        {
            var devices = _tcpDeviceService.GetOnlineDevices();
            return Ok(new { success = true, devices });
        }

        /// <summary>
        /// 获取单个TCP设备
        /// </summary>
        [HttpGet("devices/{deviceId}")]
        public IActionResult GetDevice(string deviceId)
        {
            var device = _tcpDeviceService.GetDevice(deviceId);
            if (device == null)
                return NotFound(new { success = false, message = "设备不存在或不在线" });

            return Ok(new { success = true, device });
        }

        /// <summary>
        /// 发送命令到设备 - 支持数字ID和字符串ID
        /// </summary>
        [HttpPost("devices/{deviceId}/command")]
        public async Task<IActionResult> SendCommand(string deviceId, [FromBody] TcpCommandRequest request)
        {
            try
            {
                string fullDeviceId = deviceId;

                // 如果是数字ID，先查找对应的FullDeviceId
                if (int.TryParse(deviceId, out int intDeviceId))
                {
                    var device = await _deviceDataService.GetDeviceByIdAsync(intDeviceId);
                    if (device == null)
                    {
                        return NotFound(new { success = false, message = $"设备不存在 (ID: {deviceId})" });
                    }
                    fullDeviceId = device.FullDeviceId;
                    _logger.LogInformation($"转换设备ID: {deviceId} -> {fullDeviceId}");
                }

                // 检查设备是否在线
                var tcpDevice = _tcpDeviceService.GetDevice(fullDeviceId);
                if (tcpDevice == null || !tcpDevice.IsOnline)
                {
                    return BadRequest(new { success = false, message = $"设备 {fullDeviceId} 不在线" });
                }

                await _tcpDeviceService.SendCommandAsync(fullDeviceId, request.Command, request.Parameters);
                return Ok(new { success = true, message = "命令已发送" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送命令失败");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 打开设备
        /// </summary>
        [HttpPost("devices/{deviceId}/turn-on")]
        public async Task<IActionResult> TurnOn(string deviceId)
        {
            try
            {
                string fullDeviceId = await GetFullDeviceIdAsync(deviceId);
                await _tcpDeviceService.TurnOnAsync(fullDeviceId);
                return Ok(new { success = true, message = "开启命令已发送" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 关闭设备
        /// </summary>
        [HttpPost("devices/{deviceId}/turn-off")]
        public async Task<IActionResult> TurnOff(string deviceId)
        {
            try
            {
                string fullDeviceId = await GetFullDeviceIdAsync(deviceId);
                await _tcpDeviceService.TurnOffAsync(fullDeviceId);
                return Ok(new { success = true, message = "关闭命令已发送" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 设置温度
        /// </summary>
        [HttpPost("devices/{deviceId}/temperature")]
        public async Task<IActionResult> SetTemperature(string deviceId, [FromBody] TemperatureRequest request)
        {
            try
            {
                string fullDeviceId = await GetFullDeviceIdAsync(deviceId);
                await _tcpDeviceService.SetTemperatureAsync(fullDeviceId, request.Temperature);
                return Ok(new { success = true, message = "温度设置已发送" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 设置湿度
        /// </summary>
        [HttpPost("devices/{deviceId}/humidity")]
        public async Task<IActionResult> SetHumidity(string deviceId, [FromBody] HumidityRequest request)
        {
            try
            {
                string fullDeviceId = await GetFullDeviceIdAsync(deviceId);
                await _tcpDeviceService.SetHumidityAsync(fullDeviceId, request.Humidity);
                return Ok(new { success = true, message = "湿度设置已发送" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 设置亮度
        /// </summary>
        [HttpPost("devices/{deviceId}/brightness")]
        public async Task<IActionResult> SetBrightness(string deviceId, [FromBody] BrightnessRequest request)
        {
            try
            {
                string fullDeviceId = await GetFullDeviceIdAsync(deviceId);
                await _tcpDeviceService.SetBrightnessAsync(fullDeviceId, request.Brightness);
                return Ok(new { success = true, message = "亮度设置已发送" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 设置电机转速
        /// </summary>
        [HttpPost("devices/{deviceId}/motor-speed")]
        public async Task<IActionResult> SetMotorSpeed(string deviceId, [FromBody] MotorSpeedRequest request)
        {
            try
            {
                string fullDeviceId = await GetFullDeviceIdAsync(deviceId);
                await _tcpDeviceService.SetMotorSpeedAsync(fullDeviceId, request.Speed);
                return Ok(new { success = true, message = "转速设置已发送" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 上锁
        /// </summary>
        [HttpPost("devices/{deviceId}/lock")]
        public async Task<IActionResult> Lock(string deviceId)
        {
            try
            {
                string fullDeviceId = await GetFullDeviceIdAsync(deviceId);
                await _tcpDeviceService.LockAsync(fullDeviceId);
                return Ok(new { success = true, message = "上锁命令已发送" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 解锁
        /// </summary>
        [HttpPost("devices/{deviceId}/unlock")]
        public async Task<IActionResult> Unlock(string deviceId, [FromBody] UnlockRequest request)
        {
            try
            {
                string fullDeviceId = await GetFullDeviceIdAsync(deviceId);
                await _tcpDeviceService.UnlockAsync(fullDeviceId, request.Code);
                return Ok(new { success = true, message = "解锁命令已发送" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// 辅助方法：将数字ID转换为FullDeviceId
        /// </summary>
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
                return device.FullDeviceId;
            }

            throw new Exception($"无效的设备ID格式: {deviceId}");
        }
    }

    // 请求模型
    public class TcpCommandRequest
    {
        public string Command { get; set; } = "";
        public Dictionary<string, object>? Parameters { get; set; }
    }

    public class TemperatureRequest
    {
        public double Temperature { get; set; }
    }

    public class HumidityRequest
    {
        public int Humidity { get; set; }
    }

    public class BrightnessRequest
    {
        public int Brightness { get; set; }
    }

    public class MotorSpeedRequest
    {
        public int Speed { get; set; }
    }

    public class UnlockRequest
    {
        public string Code { get; set; } = "";
    }
}