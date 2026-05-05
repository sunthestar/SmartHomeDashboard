using System.Text;
using System.Text.Json;

namespace SmartHomeDashboard.Services
{
    public class AIAssistantService
    {
        private readonly ILogger<AIAssistantService> _logger;
        private readonly DeviceDataService _deviceService;
        private readonly SceneService _sceneService;
        private readonly SystemLogService _logService;
        private readonly RoomService _roomService;

        public AIAssistantService(
            ILogger<AIAssistantService> logger,
            DeviceDataService deviceService,
            SceneService sceneService,
            SystemLogService logService,
            RoomService roomService)
        {
            _logger = logger;
            _deviceService = deviceService;
            _sceneService = sceneService;
            _logService = logService;
            _roomService = roomService;
        }

        public async Task<string> ProcessCommandAsync(string userInput, string username = "用户")
        {
            try
            {
                _logger.LogInformation($"收到用户指令: {userInput}");

                // 简单的命令匹配
                var input = userInput.ToLower().Trim();

                if (input.Contains("你好") || input.Contains("您好"))
                {
                    return $"你好！我是智能家居AI助手，有什么可以帮您？";
                }

                if (input.Contains("帮助") || input.Contains("help") || input.Contains("功能"))
                {
                    return GetHelpMessage();
                }

                if (input.Contains("设备") && input.Contains("状态"))
                {
                    return await GetDeviceStatusAsync();
                }

                if (input.Contains("房间") && input.Contains("状态"))
                {
                    return await GetRoomStatusAsync();
                }

                if (input.Contains("能耗") || input.Contains("功率") || input.Contains("用电"))
                {
                    return await GetEnergyStatusAsync();
                }

                if (input.Contains("场景"))
                {
                    return await GetSceneListAsync();
                }

                if ((input.Contains("打开") || input.Contains("开启")) && input.Contains("灯光"))
                {
                    return await ControlLightAsync(true);
                }

                if ((input.Contains("关闭") || input.Contains("关掉")) && input.Contains("灯光"))
                {
                    return await ControlLightAsync(false);
                }

                if (input.Contains("再见") || input.Contains("拜拜"))
                {
                    return "再见！随时欢迎回来，有需要随时叫我。";
                }

                return "抱歉，我没有理解您的意思。您可以试试说“帮助”查看我能做什么。";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理AI指令失败");
                return "抱歉，我遇到了一些问题，请稍后再试。";
            }
        }

        private async Task<string> GetDeviceStatusAsync()
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            var onlineCount = devices.Count(d => d.IsOn && d.StatusText != "离线");
            var offlineCount = devices.Count - onlineCount;

            var result = $"当前共有 {devices.Count} 台设备，其中在线 {onlineCount} 台，离线 {offlineCount} 台。\n";

            var onlineDevices = devices.Where(d => d.IsOn && d.StatusText != "离线").Take(5);
            if (onlineDevices.Any())
            {
                result += "在线设备：";
                foreach (var device in onlineDevices)
                {
                    result += $"{device.Name}、";
                }
                result = result.TrimEnd('、');
            }
            else
            {
                result += "当前没有在线设备。";
            }

            return result;
        }

        private async Task<string> GetRoomStatusAsync()
        {
            var rooms = await _roomService.GetAllRoomsAsync();
            var devices = await _deviceService.GetAllDevicesAsync();

            var result = "房间状态：\n";
            foreach (var room in rooms)
            {
                var roomDevices = devices.Where(d => d.RoomIdentifier == room.RoomId).ToList();
                var onlineCount = roomDevices.Count(d => d.IsOn && d.StatusText != "离线");
                result += $"• {room.RoomName}：{roomDevices.Count}台设备，{onlineCount}台在线\n";
            }

            return result;
        }

        private async Task<string> GetEnergyStatusAsync()
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            var onlineDevices = devices.Where(d => d.IsOn && d.StatusText != "离线").ToList();

            return $"当前在线设备数量：{onlineDevices.Count}台。";
        }

        private async Task<string> GetSceneListAsync()
        {
            var scenes = await _sceneService.GetAllScenesAsync();
            if (!scenes.Any())
            {
                return "当前没有配置任何自动化场景。";
            }

            var result = "自动化场景列表：\n";
            foreach (var scene in scenes)
            {
                var status = scene.IsActive ? "✅ 已启用" : "⭕ 已禁用";
                result += $"• {scene.SceneName}：{status} - {scene.Description}\n";
            }

            return result;
        }

        private async Task<string> ControlLightAsync(bool turnOn)
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            var lights = devices.Where(d => d.TypeIdentifier == "light").ToList();

            if (!lights.Any())
            {
                return "没有找到灯光设备。";
            }

            var action = turnOn ? "打开" : "关闭";
            var successCount = 0;
            var lightNames = new List<string>();

            foreach (var light in lights)
            {
                if (!light.IsOn && turnOn)
                {
                    light.IsOn = true;
                    light.StatusText = "开启";
                    await _deviceService.UpdateDeviceStatusAsync(light.Id, true, "开启");
                    lightNames.Add(light.Name);
                    successCount++;
                }
                else if (light.IsOn && !turnOn)
                {
                    light.IsOn = false;
                    light.StatusText = "关闭";
                    await _deviceService.UpdateDeviceStatusAsync(light.Id, false, "关闭");
                    lightNames.Add(light.Name);
                    successCount++;
                }
            }

            if (successCount > 0)
            {
                await _logService.AddDeviceLogAsync(turnOn ? "on" : "off", string.Join("、", lightNames), null, $"AI助手{action}灯光");
                return $"已{action} {successCount} 个灯光设备：{string.Join("、", lightNames)}。";
            }

            return $"所有灯光设备已经是{action}状态。";
        }

        private string GetHelpMessage()
        {
            return @"🤖 我是智能家居AI助手，可以帮助您：

【设备控制】
• 打开灯光 - 说“打开灯光”
• 关闭灯光 - 说“关闭灯光”

【状态查询】
• 查询设备状态 - 说“设备状态”
• 查询房间状态 - 说“房间状态”
• 查询能耗情况 - 说“能耗”

【场景管理】
• 查看场景列表 - 说“场景列表”

【系统功能】
• 获取帮助 - 说“帮助”
• 问候 - 说“你好”
• 告别 - 说“再见”

试试对我说“设备状态”吧！";
        }
    }
}