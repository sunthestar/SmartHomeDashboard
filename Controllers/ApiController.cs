using Microsoft.AspNetCore.Mvc;
using SmartHomeDashboard.Services;
using SmartHomeDashboard.Models;
using System.Text.Json;

namespace SmartHomeDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicesController : ControllerBase
    {
        private readonly DeviceDataService _deviceService;

        public DevicesController(DeviceDataService deviceService)
        {
            _deviceService = deviceService;
        }

        [HttpGet("list")]
        public IActionResult GetList()
        {
            try
            {
                var devices = _deviceService.GetAllDevices();
                return Ok(new { success = true, devices = devices });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("add")]
        public IActionResult Add([FromBody] DeviceAddModel model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { success = false, message = "无效的设备数据" });

                if (string.IsNullOrEmpty(model.Name))
                    return BadRequest(new { success = false, message = "设备名称不能为空" });

                var device = _deviceService.AddDevice(model);

                return Ok(new { success = true, message = "设备添加成功", device });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("delete")]
        public IActionResult Delete([FromBody] DeleteRequest request)
        {
            try
            {
                if (request == null || request.Id <= 0)
                    return BadRequest(new { success = false, message = "无效的设备ID" });

                var success = _deviceService.DeleteDevice(request.Id);

                if (success)
                {
                    return Ok(new { success = true, message = "设备删除成功", id = request.Id });
                }
                else
                {
                    return NotFound(new { success = false, message = $"设备不存在 (ID: {request.Id})" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public class DeleteRequest
        {
            public int Id { get; set; }
        }
    }
}