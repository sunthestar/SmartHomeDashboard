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

        // 获取所有房间
        public async Task<List<RoomModel>> GetAllRoomsAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            return await context.Rooms.OrderBy(r => r.Id).ToListAsync();
        }

        // 获取房间详情（包含设备）
        public async Task<RoomViewModel?> GetRoomDetailsAsync(string roomId)
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

        // 获取所有房间的统计信息
        public async Task<List<RoomViewModel>> GetAllRoomStatsAsync()
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

        // 更新房间统计信息
        public async Task UpdateRoomStatsAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var rooms = await context.Rooms.ToListAsync();
            var devices = await context.Devices.ToListAsync();

            foreach (var room in rooms)
            {
                var roomDevices = devices.Where(d => d.RoomIdentifier == room.RoomId).ToList();
                room.DeviceCount = roomDevices.Count;
                room.OnlineCount = roomDevices.Count(d => d.IsOn && d.StatusText != "离线");
            }

            await context.SaveChangesAsync();
        }
    }
}