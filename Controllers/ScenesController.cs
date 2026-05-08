using Microsoft.AspNetCore.Mvc;
using SmartHomeDashboard.Models;
using SmartHomeDashboard.Services;
using System.Text.Json;

namespace SmartHomeDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScenesController : ControllerBase
    {
        private readonly SceneService _sceneService;
        private readonly ILogger<ScenesController> _logger;

        public ScenesController(SceneService sceneService, ILogger<ScenesController> logger)
        {
            _sceneService = sceneService;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetScene(int id)
        {
            try
            {
                var scene = await _sceneService.GetSceneByIdAsync(id);
                if (scene == null)
                {
                    return Ok(new { success = false, message = "场景不存在" });
                }

                var actions = JsonSerializer.Deserialize<List<SceneActionDto>>(scene.Actions) ?? new List<SceneActionDto>();
                var linkedScenes = JsonSerializer.Deserialize<List<LinkedSceneDto>>(scene.LinkedScenes) ?? new List<LinkedSceneDto>();
                var conditions = JsonSerializer.Deserialize<List<ConditionDto>>(scene.Conditions) ?? new List<ConditionDto>();

                return Ok(new
                {
                    success = true,
                    scene = new
                    {
                        scene.Id,
                        scene.SceneName,
                        scene.Icon,
                        scene.Description,
                        scene.TriggerType,
                        scene.ExecuteTime,
                        scene.RepeatDays,
                        Conditions = conditions,
                        scene.ConditionLogic,
                        Actions = actions,
                        LinkedScenes = linkedScenes
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取场景失败");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetSceneList()
        {
            try
            {
                var scenes = await _sceneService.GetAllScenesAsync();
                return Ok(new { success = true, scenes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取场景列表失败");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("available-for-link")]
        public async Task<IActionResult> GetAvailableScenesForLink(int excludeId = 0)
        {
            try
            {
                var scenes = await _sceneService.GetAllScenesAsync();
                var available = scenes.Where(s => s.Id != excludeId).Select(s => new { s.Id, s.SceneName, s.Icon }).ToList();
                return Ok(new { success = true, scenes = available });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用场景列表失败");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveScene([FromBody] SceneSaveModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.SceneName))
                {
                    return BadRequest(new { success = false, message = "场景名称不能为空" });
                }

                var actionsJson = JsonSerializer.Serialize(model.Actions);
                var linkedScenesJson = JsonSerializer.Serialize(model.LinkedScenes);
                var conditionsJson = JsonSerializer.Serialize(model.Conditions);

                var scene = new SceneModel
                {
                    Id = model.Id,
                    SceneName = model.SceneName,
                    Icon = string.IsNullOrEmpty(model.Icon) ? "fa-magic" : model.Icon,
                    Description = model.Description ?? "",
                    TriggerType = model.TriggerType ?? "manual",
                    TriggerCondition = "{}",
                    Actions = actionsJson,
                    LinkedScenes = linkedScenesJson,
                    Conditions = conditionsJson,
                    ConditionLogic = model.ConditionLogic ?? "and",
                    ExecuteTime = model.ExecuteTime ?? "",
                    RepeatDays = model.RepeatDays ?? "",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                SceneModel result;
                if (model.Id > 0)
                {
                    var existing = await _sceneService.GetSceneByIdAsync(model.Id);
                    if (existing == null)
                    {
                        return BadRequest(new { success = false, message = "场景不存在" });
                    }
                    scene.CreatedAt = existing.CreatedAt;
                    var updated = await _sceneService.UpdateSceneAsync(scene);
                    if (!updated)
                    {
                        return BadRequest(new { success = false, message = "更新场景失败" });
                    }
                    result = scene;
                }
                else
                {
                    result = await _sceneService.AddSceneAsync(scene);
                }

                return Ok(new { success = true, scene = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存场景失败");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("delete")]
        public async Task<IActionResult> DeleteScene([FromBody] DeleteSceneRequest request)
        {
            try
            {
                if (request.Id <= 0)
                {
                    return BadRequest(new { success = false, message = "无效的场景ID" });
                }

                var success = await _sceneService.DeleteSceneAsync(request.Id);
                if (success)
                {
                    return Ok(new { success = true, message = "场景删除成功" });
                }
                return BadRequest(new { success = false, message = "场景不存在或删除失败" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除场景失败");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("execute/{id}")]
        public async Task<IActionResult> ExecuteScene(int id)
        {
            try
            {
                var result = await _sceneService.ExecuteSceneAsync(id);
                if (result.success)
                {
                    return Ok(new { success = true, message = result.message, offlineDevices = result.offlineDevices });
                }
                return Ok(new { success = false, message = result.message, offlineDevices = result.offlineDevices });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行场景失败");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("execute/{id}/confirm")]
        public async Task<IActionResult> ExecuteSceneWithConfirm(int id, [FromBody] ExecuteConfirmRequest request)
        {
            try
            {
                var result = await _sceneService.ExecuteSceneWithConfirmAsync(id, request.SkipDeviceTypes);
                if (result.success)
                {
                    return Ok(new { success = true, message = result.message });
                }
                return BadRequest(new { success = false, message = result.message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行场景失败");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("check-conditions")]
        public async Task<IActionResult> CheckConditionScenes()
        {
            try
            {
                await _sceneService.CheckAndExecuteConditionScenesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查条件场景失败");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public class SceneSaveModel
        {
            public int Id { get; set; }
            public string SceneName { get; set; } = "";
            public string Icon { get; set; } = "";
            public string Description { get; set; } = "";
            public string? TriggerType { get; set; }
            public string? ExecuteTime { get; set; }
            public string? RepeatDays { get; set; }
            public List<ConditionDto> Conditions { get; set; } = new();
            public string? ConditionLogic { get; set; }
            public List<SceneActionDto> Actions { get; set; } = new();
            public List<LinkedSceneDto> LinkedScenes { get; set; } = new();
        }

        public class SceneActionDto
        {
            public string DeviceId { get; set; } = "";
            public string DeviceName { get; set; } = "";
            public string DeviceType { get; set; } = "";
            public string Action { get; set; } = "";
            public string? Value { get; set; }
        }

        public class LinkedSceneDto
        {
            public int SceneId { get; set; }
            public string SceneName { get; set; } = "";
            public string Action { get; set; } = "execute";
        }

        public class ConditionDto
        {
            public string DeviceId { get; set; } = "";
            public string DeviceName { get; set; } = "";
            public string DeviceType { get; set; } = "";
            public string Operator { get; set; } = "";
            public string Value { get; set; } = "";
        }

        public class DeleteSceneRequest
        {
            public int Id { get; set; }
        }

        public class ExecuteConfirmRequest
        {
            public List<string> SkipDeviceTypes { get; set; } = new();
        }
    }
}