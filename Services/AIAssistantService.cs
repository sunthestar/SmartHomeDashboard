using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SmartHomeDashboard.Services
{
    public class DeepSeekSettings
    {
        public string ApiKey { get; set; } = "";
        public string ApiUrl { get; set; } = "https://api.deepseek.com/chat/completions";
        public string Model { get; set; } = "deepseek-chat";
    }

    public class AIAssistantService
    {
        private readonly ILogger<AIAssistantService> _logger;
        private readonly DeviceDataService _deviceService;
        private readonly SceneService _sceneService;
        private readonly RoomService _roomService;
        private readonly TcpDeviceService _tcpDeviceService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DeepSeekSettings _deepSeekSettings;

        private readonly Dictionary<string, List<ChatMessage>> _conversationHistory = new();
        private readonly int _maxHistoryCount = 20;

        private readonly Dictionary<string, string> _roomMapping = new()
        {
            { "客厅", "living" },
            { "主卧", "master-bedroom" },
            { "卧室", "master-bedroom" },
            { "次卧", "second-bedroom" },
            { "厨房", "kitchen" },
            { "浴室", "bathroom" },
            { "卫生间", "bathroom" },
            { "餐厅", "dining" },
            { "入口", "entrance" },
            { "玄关", "entrance" }
        };

        private DateTime _lastWeatherFetch = DateTime.MinValue;
        private WeatherInfo? _cachedWeather;
        private string? _cachedCity;

        public AIAssistantService(
            ILogger<AIAssistantService> logger,
            DeviceDataService deviceService,
            SceneService sceneService,
            RoomService roomService,
            TcpDeviceService tcpDeviceService,
            IHttpClientFactory httpClientFactory,
            IOptions<DeepSeekSettings> deepSeekSettings)
        {
            _logger = logger;
            _deviceService = deviceService;
            _sceneService = sceneService;
            _roomService = roomService;
            _tcpDeviceService = tcpDeviceService;
            _httpClientFactory = httpClientFactory;
            _deepSeekSettings = deepSeekSettings.Value;
        }

        public async Task<string> ProcessCommandAsync(string userInput, string username = "用户")
        {
            try
            {
                _logger.LogInformation($"收到用户指令: {userInput}");
                AddToHistory(username, "user", userInput);

                string input = userInput.ToLower().Trim();
                if (input == "重置对话" || input == "清空记忆")
                {
                    ClearHistory(username);
                    string resetResponse = "对话记忆已清空，我们可以重新开始聊天啦！";
                    AddToHistory(username, "assistant", resetResponse);
                    return resetResponse;
                }

                if (string.IsNullOrEmpty(_deepSeekSettings.ApiKey) || _deepSeekSettings.ApiKey == "your-deepseek-api-key-here")
                {
                    return "抱歉，AI服务未配置，请检查DeepSeek API Key设置。";
                }

                string aiResponse = await ProcessWithAIAsync(userInput, username);
                AddToHistory(username, "assistant", aiResponse);
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理AI指令失败");
                return "抱歉，我遇到了一些问题，请稍后再试。";
            }
        }

        private async Task<string> ProcessWithAIAsync(string userInput, string username)
        {
            try
            {
                string systemPrompt = BuildSystemPrompt();

                var messages = new List<object>();
                messages.Add(new { role = "system", content = systemPrompt });

                List<ChatMessage> history = GetHistory(username);
                for (int i = Math.Max(0, history.Count - 11); i < history.Count - 1; i++)
                {
                    messages.Add(new { role = history[i].Role, content = history[i].Content });
                }
                messages.Add(new { role = "user", content = userInput });

                var requestBody = new
                {
                    model = _deepSeekSettings.Model,
                    messages = messages,
                    stream = false,
                    temperature = 0.3
                };

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                string json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_deepSeekSettings.ApiKey}");

                HttpResponseMessage httpResponse = await client.PostAsync(_deepSeekSettings.ApiUrl, content);
                string responseJson = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"DeepSeek API返回错误: {httpResponse.StatusCode}");
                    return "抱歉，AI服务暂时不可用，请稍后再试。";
                }

                using var doc = JsonDocument.Parse(responseJson);
                string? aiResponse = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrEmpty(aiResponse))
                {
                    return "抱歉，我没有理解您的意思。";
                }

                _logger.LogInformation($"AI原始响应: {aiResponse}");

                string executionResult = await ExecuteCommandsAsync(aiResponse);
                return executionResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI处理失败");
                return "抱歉，处理您的请求时出现了问题，请稍后再试。";
            }
        }

        private string BuildSystemPrompt()
        {
            string availableScenes = GetAvailableScenesJson();

            string prompt = @"
你是智能家居AI助手，名叫小智。你的职责是理解用户的自然语言指令，并返回标准格式的JSON命令。

【重要规则】
- 对于设备控制，使用 action=control 格式
- 对于设备列表查询，使用 action=query, type=devices 格式
- 对于房间状态查询，使用 action=query, type=rooms 格式
- 对于能耗查询，使用 action=query, type=energy 格式
- 对于场景列表查询，使用 action=query, type=scenes 格式
- 对于时间查询，使用 action=query, type=time 格式
- 对于天气查询，使用 action=query, type=weather 格式
- 不要依赖上下文中的静态设备列表，通过query命令获取实时数据

【可用场景列表】
" + availableScenes + @"

【命令格式】

1. 设备控制命令：
{""action"": ""control"", ""deviceId"": ""设备的完整ID"", ""command"": ""命令类型"", ""parameters"": {}}

2. 查询命令：
{""action"": ""query"", ""type"": ""time/weather/devices/rooms/energy/scenes""}

3. 普通对话命令：
{""action"": ""respond"", ""message"": ""你的回复内容""}

【命令类型和参数说明】
- on: {""isOn"": true}
- off: {""isOn"": false}
- set_temperature: {""temperature"": 温度值(16-30)}
- set_mode: {""mode"": ""cool/heat/fan/dry/auto""}
- set_speed: {""speed"": 风速(1-5)}
- set_brightness: {""brightness"": 亮度(0-100)}
- lock: {}
- unlock: {}

【可用设备ID参考】
- light-liv-001 (客厅智能灯)
- ac-liv-001 (客厅空调)
- temp-liv-001 (客厅温度传感器)

【示例】
用户: 打开客厅灯
回复: {""action"": ""control"", ""deviceId"": ""light-liv-001"", ""command"": ""on"", ""parameters"": {""isOn"": true}}

用户: 关闭客厅空调
回复: {""action"": ""control"", ""deviceId"": ""ac-liv-001"", ""command"": ""off"", ""parameters"": {""isOn"": false}}

用户: 制冷24度
回复: [
  {""action"": ""control"", ""deviceId"": ""ac-liv-001"", ""command"": ""set_mode"", ""parameters"": {""mode"": ""cool""}},
  {""action"": ""control"", ""deviceId"": ""ac-liv-001"", ""command"": ""set_temperature"", ""parameters"": {""temperature"": 24}}
]

用户: 制热28度
回复: [
  {""action"": ""control"", ""deviceId"": ""ac-liv-001"", ""command"": ""set_mode"", ""parameters"": {""mode"": ""heat""}},
  {""action"": ""control"", ""deviceId"": ""ac-liv-001"", ""command"": ""set_temperature"", ""parameters"": {""temperature"": 28}}
]

用户: 空调调到26度
回复: {""action"": ""control"", ""deviceId"": ""ac-liv-001"", ""command"": ""set_temperature"", ""parameters"": {""temperature"": 26}}

用户: 现在有哪些设备在线
回复: {""action"": ""query"", ""type"": ""devices""}

用户: 客厅温度多少
回复: {""action"": ""query"", ""type"": ""temperature"", ""room"": ""客厅""}

用户: 今天天气怎么样
回复: {""action"": ""query"", ""type"": ""weather""}

用户: 现在几点
回复: {""action"": ""query"", ""type"": ""time""}

用户: 你好
回复: {""action"": ""respond"", ""message"": ""你好！我是小智，有什么可以帮您？""}

【重要】
- 当用户询问设备列表、房间状态、能耗、场景时，必须返回query命令
- 必须返回纯JSON格式，不要包含任何其他文字
- 使用数组格式时，必须使用合法的JSON数组语法
";

            return prompt;
        }

        private string GetAvailableScenesJson()
        {
            var scenes = _sceneService.GetAllScenesAsync().Result;
            var sceneList = scenes.Select(s => new { id = s.Id, name = s.SceneName }).ToList();
            return JsonSerializer.Serialize(sceneList, new JsonSerializerOptions { WriteIndented = false });
        }

        private async Task<string> ExecuteCommandsAsync(string aiResponse)
        {
            string trimmed = aiResponse.Trim();

            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                try
                {
                    var commands = JsonSerializer.Deserialize<List<JsonElement>>(trimmed);
                    if (commands != null && commands.Count > 0)
                    {
                        var results = new List<string>();
                        foreach (var cmd in commands)
                        {
                            string result = await ExecuteSingleCommandAsync(cmd.ToString());
                            results.Add(result);
                        }
                        return string.Join("\n", results);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "解析JSON数组失败");
                }
            }

            string commandJson = ExtractCommandJson(trimmed);
            if (!string.IsNullOrEmpty(commandJson))
            {
                return await ExecuteSingleCommandAsync(commandJson);
            }

            return aiResponse;
        }

        private async Task<string> ExecuteSingleCommandAsync(string commandJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(commandJson);
                JsonElement root = doc.RootElement;

                string? action = root.GetProperty("action").GetString();

                switch (action)
                {
                    case "control":
                        return await ExecuteControlCommand(root);

                    case "execute_scene":
                        return await ExecuteSceneCommand(root);

                    case "query":
                        return await ExecuteQueryCommand(root);

                    case "respond":
                        return root.GetProperty("message").GetString() ?? "好的";

                    default:
                        return $"未知命令类型: {action}";
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"JSON解析失败: {commandJson}");
                return "抱歉，命令格式解析失败。";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"执行命令失败: {commandJson}");
                return "抱歉，执行命令时出现问题。";
            }
        }

        private string ExtractCommandJson(string aiResponse)
        {
            if (aiResponse.Trim().StartsWith("{") && aiResponse.Trim().EndsWith("}"))
            {
                try
                {
                    JsonDocument.Parse(aiResponse);
                    return aiResponse.Trim();
                }
                catch { }
            }

            var jsonMatch = System.Text.RegularExpressions.Regex.Match(aiResponse, @"\{[\s\S]*\}");
            if (jsonMatch.Success)
            {
                try
                {
                    JsonDocument.Parse(jsonMatch.Value);
                    return jsonMatch.Value;
                }
                catch { }
            }

            return "";
        }

        private async Task<string> ExecuteControlCommand(JsonElement root)
        {
            string? deviceId = root.GetProperty("deviceId").GetString();
            string? command = root.GetProperty("command").GetString();
            JsonElement parameters = root.TryGetProperty("parameters", out var paramsElem) ? paramsElem : default;

            _logger.LogInformation($"执行控制命令: deviceId={deviceId}, command={command}");

            var allDevices = await _deviceService.GetAllDevicesAsync();
            var device = allDevices.FirstOrDefault(d => d.FullDeviceId == deviceId);

            if (device == null)
            {
                return $"找不到设备: {deviceId}";
            }

            if (device.StatusText == "离线")
            {
                return $"设备 [{device.Name}] 当前离线，无法控制";
            }

            try
            {
                switch (command)
                {
                    case "on":
                        var onParams = new Dictionary<string, object> { ["isOn"] = true };
                        await _tcpDeviceService.SendCommandAsync(device.FullDeviceId, "set_power", onParams);
                        await _deviceService.UpdateDeviceStatusAsync(device.Id, true, "开启");
                        _logger.LogInformation($"设备 {device.Name} 已开启");
                        return $"已开启 [{device.Name}]";

                    case "off":
                        var offParams = new Dictionary<string, object> { ["isOn"] = false };
                        await _tcpDeviceService.SendCommandAsync(device.FullDeviceId, "set_power", offParams);
                        await _deviceService.UpdateDeviceStatusAsync(device.Id, false, "关闭");
                        _logger.LogInformation($"设备 {device.Name} 已关闭");
                        return $"已关闭 [{device.Name}]";

                    case "set_temperature":
                        int temperature = parameters.GetProperty("temperature").GetInt32();
                        temperature = Math.Clamp(temperature, 16, 30);
                        await _tcpDeviceService.SetTemperatureAsync(device.FullDeviceId, temperature);
                        await _deviceService.UpdateDeviceAcTemperatureAsync(device.Id, temperature);
                        _logger.LogInformation($"设备 {device.Name} 温度设为 {temperature}°C");
                        return $"已将 [{device.Name}] 温度设为 {temperature}°C";

                    case "set_mode":
                        string? mode = parameters.GetProperty("mode").GetString();
                        string validMode = mode?.ToLower() ?? "cool";
                        if (validMode != "cool" && validMode != "heat" && validMode != "fan" && validMode != "dry" && validMode != "auto")
                        {
                            return $"无效的模式: {mode}";
                        }
                        var modeParams = new Dictionary<string, object> { ["mode"] = validMode };
                        await _tcpDeviceService.SendCommandAsync(device.FullDeviceId, "set_mode", modeParams);
                        await _deviceService.UpdateDeviceModeAsync(device.Id, validMode);
                        string modeText = validMode switch
                        {
                            "cool" => "制冷",
                            "heat" => "制热",
                            "fan" => "送风",
                            "dry" => "除湿",
                            "auto" => "自动",
                            _ => validMode
                        };
                        _logger.LogInformation($"设备 {device.Name} 模式设为 {modeText}");
                        return $"已将 [{device.Name}] 模式设为 {modeText}";

                    case "set_speed":
                        int speed = parameters.GetProperty("speed").GetInt32();
                        speed = Math.Clamp(speed, 1, 5);
                        var speedParams = new Dictionary<string, object> { ["speed"] = speed };
                        await _tcpDeviceService.SendCommandAsync(device.FullDeviceId, "set_speed", speedParams);
                        await _deviceService.UpdateDeviceMotorSpeedAsync(device.Id, speed);
                        _logger.LogInformation($"设备 {device.Name} 风速设为 {speed} 档");
                        return $"已将 [{device.Name}] 风速设为 {speed} 档";

                    case "set_brightness":
                        int brightness = parameters.GetProperty("brightness").GetInt32();
                        brightness = Math.Clamp(brightness, 0, 100);
                        await _tcpDeviceService.SetBrightnessAsync(device.FullDeviceId, brightness);
                        _logger.LogInformation($"设备 {device.Name} 亮度设为 {brightness}%");
                        return $"已将 [{device.Name}] 亮度设为 {brightness}%";

                    case "lock":
                        await _tcpDeviceService.LockAsync(device.FullDeviceId);
                        await _deviceService.UpdateDeviceStatusAsync(device.Id, true, "已上锁");
                        _logger.LogInformation($"设备 {device.Name} 已上锁");
                        return $"已上锁 [{device.Name}]";

                    case "unlock":
                        await _tcpDeviceService.UnlockAsync(device.FullDeviceId, "000000");
                        await _deviceService.UpdateDeviceStatusAsync(device.Id, false, "未上锁");
                        _logger.LogInformation($"设备 {device.Name} 已解锁");
                        return $"已解锁 [{device.Name}]";

                    default:
                        return $"未知命令: {command}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"执行设备命令失败: {device.Name}");
                return $"控制 [{device.Name}] 失败: {ex.Message}";
            }
        }

        private async Task<string> ExecuteSceneCommand(JsonElement root)
        {
            int sceneId = root.GetProperty("sceneId").GetInt32();

            _logger.LogInformation($"执行场景: sceneId={sceneId}");

            var scenes = await _sceneService.GetAllScenesAsync();
            var scene = scenes.FirstOrDefault(s => s.Id == sceneId);

            if (scene == null)
            {
                return $"找不到场景 ID: {sceneId}";
            }

            var result = await _sceneService.ExecuteSceneAsync(sceneId);

            if (result.success)
            {
                return $"场景 [{scene.SceneName}] 执行成功\n{result.message}";
            }
            else
            {
                return $"场景 [{scene.SceneName}] 执行失败\n{result.message}";
            }
        }

        private async Task<string> ExecuteQueryCommand(JsonElement root)
        {
            string? type = root.GetProperty("type").GetString();
            string? room = root.TryGetProperty("room", out var roomProp) ? roomProp.GetString() : "";

            switch (type)
            {
                case "time":
                    return GetCurrentTime();

                case "weather":
                    return await GetWeatherResponseAsync();

                case "devices":
                    return await GetDeviceStatusResponseAsync();

                case "rooms":
                    return await GetRoomStatusResponseAsync();

                case "energy":
                    return await GetEnergyResponseAsync();

                case "scenes":
                    return await GetSceneListResponseAsync();

                case "temperature":
                    return await GetTemperatureResponseAsync(room);

                case "humidity":
                    return await GetHumidityResponseAsync(room);

                default:
                    return $"未知查询类型: {type}";
            }
        }

        private string GetCurrentTime() => $"现在是 {DateTime.Now:HH:mm:ss}";

        private async Task<string> GetWeatherResponseAsync()
        {
            try
            {
                var weather = await GetWeatherDataAsync();
                if (weather == null)
                    return "抱歉，暂时无法获取天气信息。";

                return $"{weather.City} {weather.WeatherIcon} {weather.WeatherDescription}，温度 {weather.Temperature:F0}°C，湿度 {weather.Humidity}%，风速 {weather.WindSpeed:F1}m/s";
            }
            catch
            {
                return "获取天气信息失败。";
            }
        }

        private async Task<string> GetDeviceStatusResponseAsync()
        {
            var devices = await _deviceService.GetAllDevicesAsync();

            // StatusText 不为 "离线" 的是在线设备
            var onlineDevices = devices.Where(d => d.StatusText != "离线").ToList();
            var offlineDevices = devices.Where(d => d.StatusText == "离线").ToList();

            _logger.LogInformation($"设备统计: 总数={devices.Count}, 在线={onlineDevices.Count}, 离线={offlineDevices.Count}");
            foreach (var d in onlineDevices)
            {
                _logger.LogInformation($"在线设备: {d.Name}, StatusText={d.StatusText}, IsOn={d.IsOn}");
            }

            if (!onlineDevices.Any())
                return "当前没有在线设备。";

            var result = new StringBuilder();
            result.AppendLine($"当前有 {onlineDevices.Count} 台设备在线：\n");

            foreach (var roomGroup in onlineDevices.GroupBy(d => d.RoomIdentifier))
            {
                string roomName = GetRoomDisplayName(roomGroup.Key);
                result.AppendLine($"【{roomName}】");
                foreach (var device in roomGroup)
                {
                    string status = device.IsOn ? "●" : "○";
                    result.AppendLine($"  {status} {GetDeviceIcon(device.TypeIdentifier)} {device.Name}");
                }
                result.AppendLine();
            }

            if (offlineDevices.Any())
            {
                result.AppendLine($"另有 {offlineDevices.Count} 台设备离线");
            }

            return result.ToString();
        }

        private async Task<string> GetRoomStatusResponseAsync()
        {
            var rooms = await _roomService.GetAllRoomsAsync();
            var devices = await _deviceService.GetAllDevicesAsync();

            var result = new StringBuilder("房间设备分布：\n\n");
            foreach (var room in rooms)
            {
                var roomDevices = devices.Where(d => d.RoomIdentifier == room.RoomId).ToList();
                int onlineCount = roomDevices.Count(d => d.StatusText != "离线");

                if (roomDevices.Any())
                {
                    result.AppendLine($"【{room.RoomName}】（{onlineCount}/{roomDevices.Count}台在线）");
                    foreach (var device in roomDevices.Where(d => d.StatusText != "离线").Take(3))
                    {
                        result.AppendLine($"  {GetDeviceIcon(device.TypeIdentifier)} {device.Name}");
                    }
                    result.AppendLine();
                }
            }
            return result.ToString();
        }

        private async Task<string> GetEnergyResponseAsync()
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            var onlineDevices = devices.Where(d => d.StatusText != "离线").ToList();

            double totalPower = 0;
            foreach (var device in onlineDevices)
            {
                if (device.TypeIdentifier != "temp-sensor" && device.TypeIdentifier != "humidity-sensor")
                {
                    totalPower += device.PowerValue;
                }
            }

            if (totalPower < 0.01)
                return "当前没有用电设备。";

            return $"当前总功率：{totalPower:F2} kW";
        }

        private async Task<string> GetSceneListResponseAsync()
        {
            var scenes = await _sceneService.GetAllScenesAsync();
            if (!scenes.Any())
                return "当前没有配置任何智能场景。";

            var result = new StringBuilder("智能场景列表：\n\n");
            foreach (var scene in scenes)
            {
                string status = scene.IsActive ? "启用" : "禁用";
                result.AppendLine($"• {scene.SceneName}（{status}）：{scene.Description}");
            }
            return result.ToString();
        }

        private async Task<string> GetTemperatureResponseAsync(string roomName = "")
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            var tempSensors = devices.Where(d => d.TypeIdentifier == "temp-sensor" && d.StatusText != "离线").ToList();

            if (!tempSensors.Any())
                return "没有找到在线的温度传感器。";

            if (!string.IsNullOrEmpty(roomName))
            {
                string roomId = GetRoomId(roomName);
                if (!string.IsNullOrEmpty(roomId))
                {
                    tempSensors = tempSensors.Where(d => d.RoomIdentifier == roomId).ToList();
                    if (!tempSensors.Any())
                        return $"没有找到 {roomName} 的温度传感器。";
                }
            }

            var result = new StringBuilder();
            foreach (var sensor in tempSensors)
            {
                string roomDisplayName = GetRoomDisplayName(sensor.RoomIdentifier);
                double? temp = sensor.TemperatureValue ?? sensor.Temperature;
                if (temp.HasValue)
                {
                    result.AppendLine($"{roomDisplayName} {sensor.Name}：{temp.Value:F1}°C");
                }
            }

            return result.ToString().TrimEnd();
        }

        private async Task<string> GetHumidityResponseAsync(string roomName = "")
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            var humiditySensors = devices.Where(d => d.TypeIdentifier == "humidity-sensor" && d.StatusText != "离线").ToList();

            if (!humiditySensors.Any())
                return "没有找到在线的湿度传感器。";

            if (!string.IsNullOrEmpty(roomName))
            {
                string roomId = GetRoomId(roomName);
                if (!string.IsNullOrEmpty(roomId))
                {
                    humiditySensors = humiditySensors.Where(d => d.RoomIdentifier == roomId).ToList();
                    if (!humiditySensors.Any())
                        return $"没有找到 {roomName} 的湿度传感器。";
                }
            }

            var result = new StringBuilder();
            foreach (var sensor in humiditySensors)
            {
                string roomDisplayName = GetRoomDisplayName(sensor.RoomIdentifier);
                double? humidity = sensor.HumidityValue ?? sensor.Temperature;
                if (humidity.HasValue)
                {
                    result.AppendLine($"{roomDisplayName} {sensor.Name}：{humidity.Value:F0}%");
                }
            }

            return result.ToString().TrimEnd();
        }

        private string GetRoomId(string roomName)
        {
            foreach (var item in _roomMapping)
                if (item.Key == roomName) return item.Value;
            return "";
        }

        private string GetRoomDisplayName(string roomId)
        {
            foreach (var item in _roomMapping)
                if (item.Value == roomId) return item.Key;
            return roomId;
        }

        private string GetDeviceIcon(string deviceType)
        {
            return deviceType switch
            {
                "light" => "💡",
                "ac" => "❄️",
                "fan" => "🌀",
                "lock" => "🔒",
                "camera" => "📷",
                "temp-sensor" => "🌡️",
                "humidity-sensor" => "💧",
                "motor" => "⚙️",
                _ => "🔌"
            };
        }

        private async Task<WeatherInfo?> GetWeatherDataAsync()
        {
            try
            {
                if (_cachedWeather != null && (DateTime.Now - _lastWeatherFetch).TotalMinutes < 5)
                    return _cachedWeather;

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                string city = "南昌";
                double lat = 28.68;
                double lon = 115.86;

                try
                {
                    var ipResponse = await client.GetAsync("https://ipapi.co/json/");
                    if (ipResponse.IsSuccessStatusCode)
                    {
                        string ipJson = await ipResponse.Content.ReadAsStringAsync();
                        using var ipDoc = JsonDocument.Parse(ipJson);
                        city = ipDoc.RootElement.GetProperty("city").GetString() ?? "南昌";
                        lat = ipDoc.RootElement.GetProperty("latitude").GetDouble();
                        lon = ipDoc.RootElement.GetProperty("longitude").GetDouble();
                        _cachedCity = city;
                    }
                }
                catch { }

                string weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,relative_humidity_2m,weathercode,wind_speed_10m&timezone=Asia/Shanghai";
                var weatherResponse = await client.GetAsync(weatherUrl);

                if (weatherResponse.IsSuccessStatusCode)
                {
                    string weatherJson = await weatherResponse.Content.ReadAsStringAsync();
                    using var weatherDoc = JsonDocument.Parse(weatherJson);

                    JsonElement current = weatherDoc.RootElement.GetProperty("current");
                    double temperature = current.GetProperty("temperature_2m").GetDouble();
                    int humidity = current.GetProperty("relative_humidity_2m").GetInt32();
                    int weatherCode = current.GetProperty("weathercode").GetInt32();
                    double windSpeed = current.GetProperty("wind_speed_10m").GetDouble();

                    var weatherInfo = GetWeatherInfoByCode(weatherCode);

                    _cachedWeather = new WeatherInfo
                    {
                        City = _cachedCity ?? city,
                        Temperature = temperature,
                        Humidity = humidity,
                        WeatherDescription = weatherInfo.description,
                        WeatherIcon = weatherInfo.icon,
                        WindSpeed = windSpeed
                    };
                    _lastWeatherFetch = DateTime.Now;
                    return _cachedWeather;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private (string icon, string description) GetWeatherInfoByCode(int code)
        {
            return code switch
            {
                0 => ("☀️", "晴朗"),
                1 => ("🌤️", "晴间多云"),
                2 => ("⛅", "多云"),
                3 => ("☁️", "阴天"),
                45 => ("🌫️", "有雾"),
                48 => ("🌫️", "雾凇"),
                51 => ("🌧️", "毛毛雨"),
                53 => ("🌧️", "小雨"),
                55 => ("🌧️", "中雨"),
                61 => ("🌧️", "小雨"),
                63 => ("🌧️", "中雨"),
                65 => ("⛈️", "大雨"),
                71 => ("❄️", "小雪"),
                73 => ("❄️", "中雪"),
                75 => ("❄️", "大雪"),
                95 => ("⛈️", "雷雨"),
                _ => ("🌤️", "未知")
            };
        }

        private void AddToHistory(string username, string role, string content)
        {
            if (!_conversationHistory.ContainsKey(username))
                _conversationHistory[username] = new List<ChatMessage>();

            var history = _conversationHistory[username];
            history.Add(new ChatMessage { Role = role, Content = content });

            while (history.Count > _maxHistoryCount)
                history.RemoveAt(0);
        }

        private List<ChatMessage> GetHistory(string username)
        {
            return _conversationHistory.ContainsKey(username)
                ? _conversationHistory[username].ToList()
                : new List<ChatMessage>();
        }

        private void ClearHistory(string username)
        {
            if (_conversationHistory.ContainsKey(username))
                _conversationHistory[username].Clear();
        }
    }

    public class ChatMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    public class WeatherInfo
    {
        public string City { get; set; } = "";
        public double Temperature { get; set; }
        public int Humidity { get; set; }
        public string WeatherDescription { get; set; } = "";
        public string WeatherIcon { get; set; } = "";
        public double WindSpeed { get; set; }
    }
}