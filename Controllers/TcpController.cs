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
        private readonly ILogger<TcpController> _logger;

        public TcpController(TcpServerService tcpServerService, TcpDeviceService tcpDeviceService, ILogger<TcpController> logger)
        {
            _tcpServerService = tcpServerService;
            _tcpDeviceService = tcpDeviceService;
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
        /// 发送命令到设备
        /// </summary>
        [HttpPost("devices/{deviceId}/command")]
        public async Task<IActionResult> SendCommand(string deviceId, [FromBody] TcpCommandRequest request)
        {
            try
            {
                await _tcpDeviceService.SendCommandAsync(deviceId, request.Command, request.Parameters);
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
                await _tcpDeviceService.TurnOnAsync(deviceId);
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
                await _tcpDeviceService.TurnOffAsync(deviceId);
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
                await _tcpDeviceService.SetTemperatureAsync(deviceId, request.Temperature);
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
                await _tcpDeviceService.SetHumidityAsync(deviceId, request.Humidity);
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
                await _tcpDeviceService.SetBrightnessAsync(deviceId, request.Brightness);
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
                await _tcpDeviceService.SetMotorSpeedAsync(deviceId, request.Speed);
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
                await _tcpDeviceService.LockAsync(deviceId);
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
                await _tcpDeviceService.UnlockAsync(deviceId, request.Code);
                return Ok(new { success = true, message = "解锁命令已发送" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
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