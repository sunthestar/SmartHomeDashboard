using Microsoft.AspNetCore.Mvc;
using SmartHomeDashboard.Services;

namespace SmartHomeDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIAssistantController : ControllerBase
    {
        private readonly AIAssistantService _aiService;
        private readonly ILogger<AIAssistantController> _logger;

        public AIAssistantController(AIAssistantService aiService, ILogger<AIAssistantController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { success = false, message = "消息不能为空" });
                }

                var username = HttpContext.Session.GetString("Username") ?? "用户";
                var response = await _aiService.ProcessCommandAsync(request.Message, username);

                return Ok(new { success = true, response = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI对话处理失败");
                return BadRequest(new { success = false, message = "处理失败，请稍后再试" });
            }
        }

        public class ChatRequest
        {
            public string Message { get; set; } = "";
        }
    }
}