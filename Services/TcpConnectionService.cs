using Microsoft.EntityFrameworkCore;
using SmartHomeDashboard.Data;
using SmartHomeDashboard.Models;

namespace SmartHomeDashboard.Services
{
    public class TcpConnectionService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<TcpConnectionService> _logger;

        public TcpConnectionService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<TcpConnectionService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        // 添加或更新TCP连接
        public async Task<TcpConnectionModel> AddOrUpdateConnectionAsync(TcpConnectionModel connection)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                var existing = await context.TcpConnections
                    .FirstOrDefaultAsync(c => c.FullDeviceId == connection.FullDeviceId);

                if (existing == null)
                {
                    // 新增连接
                    connection.CreatedAt = DateTime.Now;
                    connection.LastSeen = DateTime.Now;
                    connection.LastHeartbeat = DateTime.Now;

                    await context.TcpConnections.AddAsync(connection);
                    _logger.LogInformation($"新增TCP连接: {connection.FullDeviceId}");
                }
                else
                {
                    // 更新连接
                    existing.IpAddress = connection.IpAddress;
                    existing.Port = connection.Port;
                    existing.LastSeen = DateTime.Now;
                    existing.LastHeartbeat = DateTime.Now;
                    existing.IsOnline = true;
                    existing.UpdatedAt = DateTime.Now;
                }

                await context.SaveChangesAsync();
                return existing ?? connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"添加/更新TCP连接失败: {connection.FullDeviceId}");
                throw;
            }
        }

        // 更新心跳时间
        public async Task UpdateHeartbeatAsync(string fullDeviceId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                var connection = await context.TcpConnections
                    .FirstOrDefaultAsync(c => c.FullDeviceId == fullDeviceId);

                if (connection != null)
                {
                    connection.LastHeartbeat = DateTime.Now;
                    connection.LastSeen = DateTime.Now;
                    connection.IsOnline = true;
                    connection.UpdatedAt = DateTime.Now;

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新心跳失败: {fullDeviceId}");
            }
        }

        // 设备离线
        public async Task SetDeviceOfflineAsync(string fullDeviceId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                var connection = await context.TcpConnections
                    .FirstOrDefaultAsync(c => c.FullDeviceId == fullDeviceId);

                if (connection != null)
                {
                    connection.IsOnline = false;
                    connection.TimeoutCount++;
                    connection.UpdatedAt = DateTime.Now;

                    await context.SaveChangesAsync();
                    _logger.LogInformation($"设备离线: {fullDeviceId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"设置设备离线失败: {fullDeviceId}");
            }
        }

        // 获取所有在线设备
        public async Task<List<TcpConnectionModel>> GetOnlineDevicesAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.TcpConnections
                    .Where(c => c.IsOnline)
                    .OrderByDescending(c => c.LastHeartbeat)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取在线设备失败");
                return new List<TcpConnectionModel>();
            }
        }

        // 获取设备连接历史
        public async Task<TcpConnectionModel?> GetConnectionByDeviceIdAsync(int deviceId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.TcpConnections
                    .FirstOrDefaultAsync(c => c.DeviceId == deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取设备连接失败 DeviceId: {deviceId}");
                return null;
            }
        }

        // 获取设备连接历史
        public async Task<TcpConnectionModel?> GetConnectionByFullIdAsync(string fullDeviceId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.TcpConnections
                    .FirstOrDefaultAsync(c => c.FullDeviceId == fullDeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取设备连接失败 FullDeviceId: {fullDeviceId}");
                return null;
            }
        }

        // 清理超时连接
        public async Task CleanupTimeoutConnectionsAsync(int timeoutSeconds = 90)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                var timeout = DateTime.Now.AddSeconds(-timeoutSeconds);
                var timeoutConnections = await context.TcpConnections
                    .Where(c => c.IsOnline && c.LastHeartbeat < timeout)
                    .ToListAsync();

                foreach (var conn in timeoutConnections)
                {
                    conn.IsOnline = false;
                    conn.TimeoutCount++;
                    conn.UpdatedAt = DateTime.Now;
                    _logger.LogWarning($"连接超时: {conn.FullDeviceId}, 最后心跳: {conn.LastHeartbeat}");
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理超时连接失败");
            }
        }

        // 获取连接统计
        public async Task<(int total, int online, int timeout)> GetConnectionStatsAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                var total = await context.TcpConnections.CountAsync();
                var online = await context.TcpConnections.CountAsync(c => c.IsOnline);
                var timeout = await context.TcpConnections.CountAsync(c => !c.IsOnline);

                return (total, online, timeout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取连接统计失败");
                return (0, 0, 0);
            }
        }
    }
}