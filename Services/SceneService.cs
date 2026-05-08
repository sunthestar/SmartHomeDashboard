using Microsoft.EntityFrameworkCore;
using SmartHomeDashboard.Data;
using SmartHomeDashboard.Models;
using System.Text.Json;

namespace SmartHomeDashboard.Services
{
    public class SceneService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<SceneService> _logger;
        private readonly DeviceDataService _deviceService;
        private readonly TcpDeviceService _tcpDeviceService;

        // 条件触发缓存
        private DateTime _lastConditionCheck = DateTime.MinValue;
        private readonly Dictionary<int, bool> _conditionSceneCache = new();
        private readonly object _cacheLock = new();

        public SceneService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<SceneService> logger,
            DeviceDataService deviceService,
            TcpDeviceService tcpDeviceService)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _deviceService = deviceService;
            _tcpDeviceService = tcpDeviceService;
        }

        // 获取所有场景
        public async Task<List<SceneModel>> GetAllScenesAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.Scenes.OrderBy(s => s.Id).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取场景列表失败");
                return new List<SceneModel>();
            }
        }

        // 获取场景详情
        public async Task<SceneModel?> GetSceneByIdAsync(int id)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.Scenes.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取场景失败 ID: {id}");
                return null;
            }
        }

        // 添加场景
        public async Task<SceneModel> AddSceneAsync(SceneModel scene)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                scene.CreatedAt = DateTime.Now;
                await context.Scenes.AddAsync(scene);
                await context.SaveChangesAsync();
                _logger.LogInformation($"场景添加成功: {scene.SceneName}");
                return scene;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加场景失败");
                throw;
            }
        }

        // 更新场景
        public async Task<bool> UpdateSceneAsync(SceneModel scene)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var existing = await context.Scenes.FindAsync(scene.Id);
                if (existing == null) return false;

                existing.SceneName = scene.SceneName;
                existing.Icon = scene.Icon;
                existing.Description = scene.Description;
                existing.TriggerType = scene.TriggerType;
                existing.TriggerCondition = scene.TriggerCondition;
                existing.Actions = scene.Actions;
                existing.LinkedScenes = scene.LinkedScenes;
                existing.Conditions = scene.Conditions;
                existing.ConditionLogic = scene.ConditionLogic;
                existing.ExecuteTime = scene.ExecuteTime;
                existing.RepeatDays = scene.RepeatDays;
                existing.TriggerSceneId = scene.TriggerSceneId;
                existing.TriggerSceneAction = scene.TriggerSceneAction;
                existing.IsActive = scene.IsActive;
                existing.UpdatedAt = DateTime.Now;

                await context.SaveChangesAsync();
                _logger.LogInformation($"场景更新成功: {scene.SceneName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新场景失败 ID: {scene.Id}");
                return false;
            }
        }

        // 删除场景
        public async Task<bool> DeleteSceneAsync(int id)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var scene = await context.Scenes.FindAsync(id);
                if (scene == null) return false;

                context.Scenes.Remove(scene);
                await context.SaveChangesAsync();
                _logger.LogInformation($"场景删除成功: {scene.SceneName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除场景失败 ID: {id}");
                return false;
            }
        }

        // 执行场景 - 返回执行结果和离线设备列表
        public async Task<(bool success, string message, List<string> offlineDevices)> ExecuteSceneAsync(int id, bool triggerLinked = true)
        {
            var offlineDevices = new List<string>();

            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                var scene = await context.Scenes.FindAsync(id);
                if (scene == null)
                {
                    await AddSceneLogAsync("系统", false, $"场景不存在 (ID: {id})");
                    return (false, "场景不存在", offlineDevices);
                }

                var actions = JsonSerializer.Deserialize<List<SceneAction>>(scene.Actions);
                if (actions == null || actions.Count == 0)
                {
                    await AddSceneLogAsync(scene.SceneName, false, "场景没有配置任何操作");
                    return (false, "场景没有配置操作", offlineDevices);
                }

                var allDevices = await _deviceService.GetAllDevicesAsync();
                var offlineDeviceNames = new List<string>();
                var validActions = new List<(DeviceModel device, SceneAction action)>();

                foreach (var action in actions)
                {
                    var targetDevice = allDevices.FirstOrDefault(d => d.FullDeviceId == action.DeviceId);

                    if (targetDevice == null)
                    {
                        offlineDeviceNames.Add(action.DeviceName ?? action.DeviceType);
                        offlineDevices.Add(action.DeviceName ?? action.DeviceType);
                    }
                    else if (targetDevice.StatusText == "离线")
                    {
                        offlineDeviceNames.Add(targetDevice.Name);
                        offlineDevices.Add(targetDevice.Name);
                    }
                    else
                    {
                        validActions.Add((targetDevice, action));
                    }
                }

                if (offlineDeviceNames.Count > 0)
                {
                    var errorMsg = $"以下设备离线: {string.Join(", ", offlineDeviceNames)}";
                    await AddSceneLogAsync(scene.SceneName, false, errorMsg);
                    return (false, errorMsg, offlineDevices);
                }

                int successCount = 0, failCount = 0;
                var failedActions = new List<string>();

                foreach (var (device, action) in validActions)
                {
                    var result = await ExecuteActionAsync(device, action);
                    if (result)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        failedActions.Add(device.Name);
                    }
                }

                scene.ExecuteCount++;
                scene.LastExecuteTime = DateTime.Now;
                scene.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();

                string message = $"场景执行完成，成功 {successCount} 项";
                if (failCount > 0)
                {
                    message += $"，失败 {failCount} 项: {string.Join(", ", failedActions)}";
                }

                if (failCount > 0)
                {
                    await AddSceneLogAsync(scene.SceneName, false, message);
                }
                else
                {
                    await AddSceneLogAsync(scene.SceneName, true, message);
                }

                if (triggerLinked)
                {
                    await CheckAndExecuteLinkedScenes(id, "on_execute");
                }

                return (true, message, offlineDevices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"执行场景失败 ID: {id}");
                await AddSceneLogAsync($"场景(ID:{id})", false, $"执行异常: {ex.Message}");
                return (false, $"执行场景失败: {ex.Message}", offlineDevices);
            }
        }

        // 执行场景（带确认）
        public async Task<(bool success, string message)> ExecuteSceneWithConfirmAsync(int id, List<string> skipDeviceNames = null)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                var scene = await context.Scenes.FindAsync(id);
                if (scene == null)
                {
                    await AddSceneLogAsync("系统", false, $"场景不存在 (ID: {id})");
                    return (false, "场景不存在");
                }

                var actions = JsonSerializer.Deserialize<List<SceneAction>>(scene.Actions);
                if (actions == null || actions.Count == 0)
                {
                    await AddSceneLogAsync(scene.SceneName, false, "场景没有配置任何操作");
                    return (false, "场景没有配置操作");
                }

                var allDevices = await _deviceService.GetAllDevicesAsync();
                int successCount = 0, failCount = 0;
                var failedActions = new List<string>();

                foreach (var action in actions)
                {
                    if (skipDeviceNames != null && skipDeviceNames.Contains(action.DeviceName))
                    {
                        continue;
                    }

                    var targetDevice = allDevices.FirstOrDefault(d => d.FullDeviceId == action.DeviceId);
                    if (targetDevice == null || targetDevice.StatusText == "离线")
                    {
                        failCount++;
                        failedActions.Add(action.DeviceName ?? action.DeviceType);
                        continue;
                    }

                    var result = await ExecuteActionAsync(targetDevice, action);
                    if (result)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        failedActions.Add(targetDevice.Name);
                    }
                }

                scene.ExecuteCount++;
                scene.LastExecuteTime = DateTime.Now;
                scene.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();

                var message = $"场景执行完成，成功 {successCount} 项";
                if (failCount > 0)
                {
                    message += $"，失败 {failCount} 项: {string.Join(", ", failedActions)}";
                }

                if (failCount > 0)
                {
                    await AddSceneLogAsync(scene.SceneName, false, message);
                }
                else
                {
                    await AddSceneLogAsync(scene.SceneName, true, message);
                }

                await CheckAndExecuteLinkedScenes(id, "on_execute");

                return (true, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"执行场景失败 ID: {id}");
                await AddSceneLogAsync($"场景(ID:{id})", false, $"执行异常: {ex.Message}");
                return (false, $"执行场景失败: {ex.Message}");
            }
        }

        // 执行单个动作
        private async Task<bool> ExecuteActionAsync(DeviceModel device, SceneAction action)
        {
            try
            {
                _logger.LogInformation($"执行动作: 设备={device.Name}, 类型={action.DeviceType}, 动作={action.Action}");

                switch (action.DeviceType)
                {
                    case "light":
                        if (action.Action == "on")
                        {
                            var parameters = new Dictionary<string, object> { ["isOn"] = true };
                            await _tcpDeviceService.SendCommandAsync(device.FullDeviceId, "set_power", parameters);
                        }
                        else if (action.Action == "off")
                        {
                            var parameters = new Dictionary<string, object> { ["isOn"] = false };
                            await _tcpDeviceService.SendCommandAsync(device.FullDeviceId, "set_power", parameters);
                        }
                        else if (action.Action == "set_brightness" && !string.IsNullOrEmpty(action.Value))
                        {
                            var brightness = int.Parse(action.Value);
                            await _tcpDeviceService.SetBrightnessAsync(device.FullDeviceId, brightness);
                        }
                        break;

                    case "ac":
                        if (action.Action == "on")
                        {
                            var parameters = new Dictionary<string, object> { ["isOn"] = true };
                            await _tcpDeviceService.SendCommandAsync(device.FullDeviceId, "set_power", parameters);
                        }
                        else if (action.Action == "off")
                        {
                            var parameters = new Dictionary<string, object> { ["isOn"] = false };
                            await _tcpDeviceService.SendCommandAsync(device.FullDeviceId, "set_power", parameters);
                        }
                        else if (action.Action == "set_temperature" && !string.IsNullOrEmpty(action.Value))
                        {
                            var temp = double.Parse(action.Value);
                            await _tcpDeviceService.SetTemperatureAsync(device.FullDeviceId, temp);
                        }
                        else if (action.Action == "set_mode" && !string.IsNullOrEmpty(action.Value))
                        {
                            var parameters = new Dictionary<string, object> { ["mode"] = action.Value };
                            await _tcpDeviceService.SendCommandAsync(device.FullDeviceId, "set_mode", parameters);
                        }
                        break;

                    case "fan":
                        if (action.Action == "on")
                        {
                            var parameters = new Dictionary<string, object> { ["isOn"] = true };
                            await _tcpDeviceService.SendCommandAsync(device.FullDeviceId, "set_power", parameters);
                        }
                        else if (action.Action == "off")
                        {
                            var parameters = new Dictionary<string, object> { ["isOn"] = false };
                            await _tcpDeviceService.SendCommandAsync(device.FullDeviceId, "set_power", parameters);
                        }
                        else if (action.Action == "set_speed" && !string.IsNullOrEmpty(action.Value))
                        {
                            var speed = int.Parse(action.Value);
                            await _tcpDeviceService.SetMotorSpeedAsync(device.FullDeviceId, speed);
                        }
                        break;

                    case "lock":
                        if (action.Action == "on")
                        {
                            await _tcpDeviceService.LockAsync(device.FullDeviceId);
                        }
                        break;

                    case "camera":
                        if (action.Action == "on")
                        {
                            var parameters = new Dictionary<string, object> { ["isOn"] = true };
                            await _tcpDeviceService.SendCommandAsync(device.FullDeviceId, "set_power", parameters);
                        }
                        else if (action.Action == "off")
                        {
                            var parameters = new Dictionary<string, object> { ["isOn"] = false };
                            await _tcpDeviceService.SendCommandAsync(device.FullDeviceId, "set_power", parameters);
                        }
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送设备命令失败: 设备={device.Name}");
                return false;
            }
        }

        // 检查并执行联动场景
        public async Task CheckAndExecuteLinkedScenes(int sourceSceneId, string triggerAction)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var sourceScene = await context.Scenes.FindAsync(sourceSceneId);
                if (sourceScene == null) return;

                var linkedScenes = JsonSerializer.Deserialize<List<LinkedSceneConfig>>(sourceScene.LinkedScenes);
                if (linkedScenes == null || linkedScenes.Count == 0) return;

                foreach (var link in linkedScenes)
                {
                    var targetScene = await context.Scenes.FindAsync(link.SceneId);
                    if (targetScene == null) continue;

                    _logger.LogInformation($"执行联动场景: {sourceScene.SceneName} -> {targetScene.SceneName}, 动作: {link.Action}");

                    switch (link.Action)
                    {
                        case "execute":
                            await ExecuteSceneAsync(link.SceneId, false);
                            break;
                        case "enable":
                            targetScene.IsActive = true;
                            targetScene.UpdatedAt = DateTime.Now;
                            await context.SaveChangesAsync();
                            break;
                        case "disable":
                            targetScene.IsActive = false;
                            targetScene.UpdatedAt = DateTime.Now;
                            await context.SaveChangesAsync();
                            break;
                        case "toggle":
                            targetScene.IsActive = !targetScene.IsActive;
                            targetScene.UpdatedAt = DateTime.Now;
                            await context.SaveChangesAsync();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"执行联动场景失败, 源场景ID: {sourceSceneId}");
            }
        }

        // 检查并执行定时场景（优化版）
        public async Task CheckAndExecuteScheduledScenesAsync()
        {
            try
            {
                var now = DateTime.Now;
                var currentTime = now.ToString("HH:mm");
                var currentDay = now.DayOfWeek.ToString().ToLower().Substring(0, 3);

                // 只在秒数为0或30时检查，避免频繁检查
                if (now.Second != 0 && now.Second != 30)
                {
                    return;
                }

                using var context = await _dbContextFactory.CreateDbContextAsync();
                var timeScenes = await context.Scenes
                    .Where(s => s.TriggerType == "time" && !string.IsNullOrEmpty(s.ExecuteTime))
                    .ToListAsync();

                var executedSceneIds = new HashSet<int>();

                foreach (var scene in timeScenes)
                {
                    if (scene.ExecuteTime != currentTime) continue;

                    if (!string.IsNullOrEmpty(scene.RepeatDays) && !scene.RepeatDays.Contains(currentDay)) continue;

                    if (executedSceneIds.Add(scene.Id))
                    {
                        _logger.LogInformation($"定时场景触发: {scene.SceneName} at {currentTime}");
                        await ExecuteSceneAsync(scene.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查定时场景失败");
            }
        }

        // 检查并执行条件触发的场景（优化版）
        public async Task CheckAndExecuteConditionScenesAsync()
        {
            try
            {
                // 限制检查频率，避免过于频繁
                var now = DateTime.Now;
                if ((now - _lastConditionCheck).TotalSeconds < 2)
                {
                    return;
                }
                _lastConditionCheck = now;

                using var context = await _dbContextFactory.CreateDbContextAsync();
                var conditionScenes = await context.Scenes
                    .Where(s => s.TriggerType == "condition")
                    .ToListAsync();

                if (!conditionScenes.Any()) return;

                var allDevices = await _deviceService.GetAllDevicesAsync();
                var deviceValues = new Dictionary<string, double>();

                foreach (var sensor in allDevices.Where(d => d.TypeIdentifier == "temp-sensor" && d.TemperatureValue.HasValue))
                {
                    deviceValues[sensor.FullDeviceId] = sensor.TemperatureValue.Value;
                }

                foreach (var sensor in allDevices.Where(d => d.TypeIdentifier == "humidity-sensor" && d.HumidityValue.HasValue))
                {
                    deviceValues[sensor.FullDeviceId] = sensor.HumidityValue.Value;
                }

                var triggeredScenes = new List<int>();

                foreach (var scene in conditionScenes)
                {
                    var conditions = JsonSerializer.Deserialize<List<ConditionConfig>>(scene.Conditions);
                    if (conditions == null || conditions.Count == 0) continue;

                    bool shouldExecute = scene.ConditionLogic == "and"
                        ? conditions.All(c => CheckCondition(c, deviceValues))
                        : conditions.Any(c => CheckCondition(c, deviceValues));

                    lock (_cacheLock)
                    {
                        if (shouldExecute && !_conditionSceneCache.ContainsKey(scene.Id))
                        {
                            _conditionSceneCache[scene.Id] = true;
                            triggeredScenes.Add(scene.Id);
                        }
                        else if (!shouldExecute)
                        {
                            _conditionSceneCache.Remove(scene.Id);
                        }
                    }
                }

                foreach (var sceneId in triggeredScenes)
                {
                    var scene = conditionScenes.First(s => s.Id == sceneId);
                    _logger.LogInformation($"条件触发场景: {scene.SceneName}");
                    await ExecuteSceneAsync(sceneId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查条件场景失败");
            }
        }

        private bool CheckCondition(ConditionConfig condition, Dictionary<string, double> deviceValues)
        {
            if (!deviceValues.ContainsKey(condition.DeviceId)) return false;

            var currentValue = deviceValues[condition.DeviceId];
            var threshold = double.TryParse(condition.Value, out var val) ? val : 0;

            return condition.Operator switch
            {
                ">" => currentValue > threshold,
                "<" => currentValue < threshold,
                "=" => Math.Abs(currentValue - threshold) < 0.01,
                ">=" => currentValue >= threshold,
                "<=" => currentValue <= threshold,
                _ => false
            };
        }

        // 添加场景执行日志
        private async Task AddSceneLogAsync(string sceneName, bool isSuccess, string details)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                var log = new SystemLogModel
                {
                    LogType = "automation",
                    LogLevel = isSuccess ? "info" : "error",
                    Title = isSuccess ? $"场景执行成功: {sceneName}" : $"场景执行失败: {sceneName}",
                    Content = details,
                    DeviceName = sceneName,
                    ActionType = "scene_trigger",
                    ActionDetail = details,
                    Timestamp = DateTime.Now,
                    IsRead = false
                };

                await context.SystemLogs.AddAsync(log);
                await context.SaveChangesAsync();

                _logger.LogInformation($"场景日志已记录: {sceneName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"记录场景日志失败: {sceneName}");
            }
        }

        // 在设备数据变化时触发条件检查（供外部调用）
        public async Task TriggerConditionCheckOnDataChange()
        {
            // 清除缓存，强制重新检查
            lock (_cacheLock)
            {
                _conditionSceneCache.Clear();
            }
            _lastConditionCheck = DateTime.MinValue;
            await CheckAndExecuteConditionScenesAsync();
        }

        // 内部类定义
        private class SceneAction
        {
            public string DeviceId { get; set; } = "";
            public string DeviceName { get; set; } = "";
            public string DeviceType { get; set; } = "";
            public string Action { get; set; } = "";
            public string? Value { get; set; }
        }

        private class LinkedSceneConfig
        {
            public int SceneId { get; set; }
            public string SceneName { get; set; } = "";
            public string Action { get; set; } = "execute";
        }

        private class ConditionConfig
        {
            public string DeviceId { get; set; } = "";
            public string DeviceName { get; set; } = "";
            public string DeviceType { get; set; } = "";
            public string Operator { get; set; } = "";
            public string Value { get; set; } = "";
        }
    }
}