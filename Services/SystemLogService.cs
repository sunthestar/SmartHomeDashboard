using Microsoft.EntityFrameworkCore;
using SmartHomeDashboard.Data;
using SmartHomeDashboard.Models;

namespace SmartHomeDashboard.Services
{
    public class SystemLogService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<SystemLogService> _logger;

        public SystemLogService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<SystemLogService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        // 添加日志
        public async Task AddLogAsync(SystemLogModel log)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                log.Timestamp = DateTime.Now;

                await context.SystemLogs.AddAsync(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加日志失败");
            }
        }

        // 添加设备操作日志
        public async Task AddDeviceLogAsync(string actionType, string deviceName, int? deviceId = null, string detail = "")
        {
            var log = new SystemLogModel
            {
                LogType = "device",
                LogLevel = "info",
                Title = $"设备{GetActionText(actionType)}",
                Content = $"{deviceName} 已{GetActionText(actionType)}",
                DeviceId = deviceId,
                DeviceName = deviceName,
                ActionType = actionType,
                ActionDetail = detail,
                Timestamp = DateTime.Now
            };

            await AddLogAsync(log);
        }

        // 添加系统日志
        public async Task AddSystemLogAsync(string title, string content, string level = "info")
        {
            var log = new SystemLogModel
            {
                LogType = "system",
                LogLevel = level,
                Title = title,
                Content = content,
                Timestamp = DateTime.Now
            };

            await AddLogAsync(log);
        }

        // 添加告警日志
        public async Task AddAlertLogAsync(string title, string content, int? deviceId = null, string deviceName = "")
        {
            var log = new SystemLogModel
            {
                LogType = "alert",
                LogLevel = "warning",
                Title = title,
                Content = content,
                DeviceId = deviceId,
                DeviceName = deviceName,
                Timestamp = DateTime.Now
            };

            await AddLogAsync(log);
        }

        // 添加自动化日志
        public async Task AddAutomationLogAsync(string sceneName, string action, string detail = "")
        {
            var log = new SystemLogModel
            {
                LogType = "automation",
                LogLevel = "info",
                Title = $"自动化场景 {action}",
                Content = $"场景 \"{sceneName}\" 已{action}",
                DeviceName = sceneName,
                ActionType = action,
                ActionDetail = detail,
                Timestamp = DateTime.Now
            };

            await AddLogAsync(log);
        }

        // 获取所有日志
        public async Task<List<SystemLogModel>> GetAllLogsAsync(int limit = 100)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.SystemLogs
                    .OrderByDescending(l => l.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取日志失败");
                return new List<SystemLogModel>();
            }
        }

        // 获取指定类型的日志
        public async Task<List<SystemLogModel>> GetLogsByTypeAsync(string logType, int limit = 100)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.SystemLogs
                    .Where(l => l.LogType == logType)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取 {logType} 日志失败");
                return new List<SystemLogModel>();
            }
        }

        // 获取设备日志
        public async Task<List<SystemLogModel>> GetDeviceLogsAsync(int deviceId, int limit = 50)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.SystemLogs
                    .Where(l => l.DeviceId == deviceId)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取设备 {deviceId} 日志失败");
                return new List<SystemLogModel>();
            }
        }

        // 标记日志为已读
        public async Task MarkAsReadAsync(int logId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var log = await context.SystemLogs.FindAsync(logId);
                if (log != null)
                {
                    log.IsRead = true;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"标记日志已读失败 ID: {logId}");
            }
        }

        // 标记所有日志为已读
        public async Task MarkAllAsReadAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var logs = await context.SystemLogs.Where(l => !l.IsRead).ToListAsync();
                foreach (var log in logs)
                {
                    log.IsRead = true;
                }
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记所有日志已读失败");
            }
        }

        // 清除旧日志
        public async Task CleanOldLogsAsync(int daysToKeep = 30)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var oldLogs = await context.SystemLogs
                    .Where(l => l.Timestamp < cutoffDate)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    context.SystemLogs.RemoveRange(oldLogs);
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"已清除 {oldLogs.Count} 条旧日志");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除旧日志失败");
            }
        }

        // 获取未读日志数量
        public async Task<int> GetUnreadCountAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.SystemLogs.CountAsync(l => !l.IsRead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取未读日志数量失败");
                return 0;
            }
        }

        // 获取操作文本
        private string GetActionText(string actionType)
        {
            return actionType switch
            {
                "add" => "添加",
                "delete" => "删除",
                "update" => "更新",
                "control" => "控制",
                "on" => "开启",
                "off" => "关闭",
                "online" => "上线",
                "offline" => "离线",
                _ => actionType
            };
        }
    }
}