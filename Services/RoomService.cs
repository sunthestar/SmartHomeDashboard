using Microsoft.EntityFrameworkCore;
using SmartHomeDashboard.Data;
using SmartHomeDashboard.Models;

namespace SmartHomeDashboard.Services
{
    public class RoomService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<RoomService> _logger;

        public RoomService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<RoomService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<List<RoomModel>> GetAllRoomsAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.Rooms.OrderBy(r => r.Id).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取房间列表失败");
                return new List<RoomModel>();
            }
        }

        public List<RoomModel> GetAllRooms()
        {
            return Task.Run(async () => await GetAllRoomsAsync()).Result;
        }

        public async Task<RoomModel?> GetRoomByIdAsync(int id)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.Rooms.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取房间失败 ID: {id}");
                return null;
            }
        }

        public async Task<RoomModel?> GetRoomByRoomIdAsync(string roomId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取房间失败 RoomId: {roomId}");
                return null;
            }
        }

        public async Task UpdateRoomStatsAsync(string roomId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var room = await context.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
                if (room != null)
                {
                    var devices = await context.Devices
                        .Where(d => d.RoomIdentifier == roomId)
                        .ToListAsync();

                    room.DeviceCount = devices.Count;
                    room.OnlineCount = devices.Count(d => d.IsOn && d.StatusText != "离线");
                    room.UpdatedAt = DateTime.Now;

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新房间统计失败 RoomId: {roomId}");
            }
        }

        public async Task UpdateAllRoomStatsAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var rooms = await context.Rooms.ToListAsync();
                var devices = await context.Devices.ToListAsync();

                foreach (var room in rooms)
                {
                    var roomDevices = devices.Where(d => d.RoomIdentifier == room.RoomId).ToList();
                    room.DeviceCount = roomDevices.Count;
                    room.OnlineCount = roomDevices.Count(d => d.IsOn && d.StatusText != "离线");
                    room.UpdatedAt = DateTime.Now;
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("所有房间统计已更新");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新所有房间统计失败");
            }
        }

        public async Task<RoomViewModel?> GetRoomDetailsAsync(string roomId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                var room = await context.Rooms
                    .FirstOrDefaultAsync(r => r.RoomId == roomId);

                if (room == null) return null;

                var devices = await context.Devices
                    .Where(d => d.RoomIdentifier == roomId)
                    .ToListAsync();

                var deviceTypes = await context.DeviceTypes.ToListAsync();

                var devicesByType = new Dictionary<string, List<DeviceModel>>();

                foreach (var type in deviceTypes)
                {
                    var typeDevices = devices.Where(d => d.TypeIdentifier == type.TypeId).ToList();
                    if (typeDevices.Any())
                    {
                        devicesByType[type.TypeId] = typeDevices;
                    }
                }

                return new RoomViewModel
                {
                    RoomId = room.RoomId,
                    RoomName = room.RoomName,
                    DeviceCount = devices.Count,
                    OnlineCount = devices.Count(d => d.IsOn && d.StatusText != "离线"),
                    DevicesByType = devicesByType
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取房间详情失败 RoomId: {roomId}");
                return null;
            }
        }

        public async Task<List<RoomViewModel>> GetAllRoomStatsAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                var rooms = await context.Rooms.ToListAsync();
                var devices = await context.Devices.ToListAsync();
                var deviceTypes = await context.DeviceTypes.ToDictionaryAsync(t => t.TypeId);

                var result = new List<RoomViewModel>();

                foreach (var room in rooms)
                {
                    var roomDevices = devices.Where(d => d.RoomIdentifier == room.RoomId).ToList();

                    var devicesByType = new Dictionary<string, List<DeviceModel>>();

                    foreach (var type in deviceTypes.Values)
                    {
                        var typeDevices = roomDevices.Where(d => d.TypeIdentifier == type.TypeId).ToList();
                        if (typeDevices.Any())
                        {
                            devicesByType[type.TypeId] = typeDevices;
                        }
                    }

                    result.Add(new RoomViewModel
                    {
                        RoomId = room.RoomId,
                        RoomName = room.RoomName,
                        DeviceCount = roomDevices.Count,
                        OnlineCount = roomDevices.Count(d => d.IsOn && d.StatusText != "离线"),
                        DevicesByType = devicesByType
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有房间统计失败");
                return new List<RoomViewModel>();
            }
        }
    }
}