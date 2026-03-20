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

        public SceneService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            ILogger<SceneService> logger,
            DeviceDataService deviceService)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
            _deviceService = deviceService;
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

        // 获取启用的场景
        public async Task<List<SceneModel>> GetActiveScenesAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.Scenes.Where(s => s.IsActive).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取启用场景失败");
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
                existing.ExecuteTime = scene.ExecuteTime;
                existing.RepeatDays = scene.RepeatDays;
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

        // 更新场景启用状态
        public async Task<bool> UpdateSceneActiveStateAsync(int id, bool isActive)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                var scene = await context.Scenes.FindAsync(id);
                if (scene == null) return false;

                scene.IsActive = isActive;
                scene.UpdatedAt = DateTime.Now;

                await context.SaveChangesAsync();
                _logger.LogInformation($"场景状态更新: {scene.SceneName} -> {(isActive ? "启用" : "禁用")}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新场景状态失败 ID: {id}");
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

        // 执行场景
        public async Task<bool> ExecuteSceneAsync(int id)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                var scene = await context.Scenes.FindAsync(id);
                if (scene == null || !scene.IsActive) return false;

                // 解析动作JSON
                var actions = JsonSerializer.Deserialize<List<SceneAction>>(scene.Actions);
                if (actions == null) return false;

                // 执行每个动作
                foreach (var action in actions)
                {
                    await ExecuteActionAsync(action);
                }

                // 更新执行计数
                scene.ExecuteCount++;
                scene.LastExecuteTime = DateTime.Now;
                scene.UpdatedAt = DateTime.Now;

                await context.SaveChangesAsync();

                _logger.LogInformation($"场景执行成功: {scene.SceneName}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"执行场景失败 ID: {id}");
                return false;
            }
        }

        // 执行单个动作
        private async Task ExecuteActionAsync(SceneAction action)
        {
            // 根据动作类型执行对应的设备控制
            switch (action.Type)
            {
                case "light":
                    if (action.Action == "off")
                    {
                        // 关闭所有灯光
                        var lights = await _deviceService.GetDevicesByTypeAsync("light");
                        foreach (var light in lights)
                        {
                            // 发送关闭命令
                        }
                    }
                    else if (action.Action == "dim" && action.Value.HasValue)
                    {
                        // 调暗灯光
                    }
                    break;

                case "ac":
                    if (action.Action == "set_temperature" && action.Value.HasValue)
                    {
                        // 设置空调温度
                    }
                    break;

                    // 其他设备类型...
            }
        }

        // 定时检查并执行场景（可由后台服务调用）
        public async Task CheckAndExecuteScheduledScenesAsync()
        {
            try
            {
                var now = DateTime.Now;
                var currentTime = now.ToString("HH:mm");
                var currentDay = now.DayOfWeek.ToString().ToLower().Substring(0, 3);

                var activeScenes = await GetActiveScenesAsync();

                foreach (var scene in activeScenes.Where(s => s.TriggerType == "time"))
                {
                    // 检查执行时间
                    if (scene.ExecuteTime != currentTime) continue;

                    // 检查重复周期
                    if (!string.IsNullOrEmpty(scene.RepeatDays) &&
                        !scene.RepeatDays.Contains(currentDay)) continue;

                    // 执行场景
                    await ExecuteSceneAsync(scene.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查定时场景失败");
            }
        }

        // 场景动作模型
        private class SceneAction
        {
            public string Type { get; set; } = "";
            public string Action { get; set; } = "";
            public int? Value { get; set; }
            public string? Playlist { get; set; }
        }
    }
}