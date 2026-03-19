using Microsoft.EntityFrameworkCore;
using SmartHomeDashboard.Data;
using SmartHomeDashboard.Models;
using System.Text.Json;

namespace SmartHomeDashboard.Services
{
    public class DeviceDataService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<DeviceDataService> _logger;

        public DeviceDataService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<DeviceDataService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;

            // 确保数据库已创建
            using var context = _dbContextFactory.CreateDbContext();
            context.Database.EnsureCreated();

            _logger.LogInformation($"DeviceDataService 初始化完成，数据库路径: {context.DbPath}");
        }

        // ==================== 基本查询方法 ====================

        // 获取所有设备（同步版本，兼容原有代码）
        public List<DeviceModel> GetAllDevices()
        {
            return Task.Run(async () => await GetAllDevicesAsync()).Result;
        }

        // 获取所有设备（异步）
        public async Task<List<DeviceModel>> GetAllDevicesAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.Devices
                    .Include(d => d.Room)
                    .Include(d => d.DeviceType)
                    .OrderBy(d => d.RoomId)
                    .ThenBy(d => d.TypeIdentifier)
                    .ThenBy(d => d.DeviceNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有设备失败");
                return new List<DeviceModel>();
            }
        }

        // 根据房间获取设备
        public async Task<List<DeviceModel>> GetDevicesByRoomAsync(string roomId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.Devices
                    .Where(d => d.RoomIdentifier == roomId)
                    .OrderBy(d => d.TypeIdentifier)
                    .ThenBy(d => d.DeviceNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取房间 {roomId} 设备失败");
                return new List<DeviceModel>();
            }
        }

        // 根据类型获取设备
        public async Task<List<DeviceModel>> GetDevicesByTypeAsync(string typeId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.Devices
                    .Where(d => d.TypeIdentifier == typeId)
                    .OrderBy(d => d.RoomIdentifier)
                    .ThenBy(d => d.DeviceNumber)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取类型 {typeId} 设备失败");
                return new List<DeviceModel>();
            }
        }

        // 根据完整设备ID获取设备
        public async Task<DeviceModel?> GetDeviceByFullIdAsync(string fullDeviceId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.Devices
                    .FirstOrDefaultAsync(d => d.FullDeviceId == fullDeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取设备 {fullDeviceId} 失败");
                return null;
            }
        }

        // 根据ID获取设备
        public async Task<DeviceModel?> GetDeviceByIdAsync(int id)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.Devices
                    .Include(d => d.Room)
                    .Include(d => d.DeviceType)
                    .FirstOrDefaultAsync(d => d.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取设备 ID {id} 失败");
                return null;
            }
        }

        // ==================== 分组查询方法 ====================

        // 获取按房间分组的设备
        public async Task<Dictionary<string, List<DeviceModel>>> GetDevicesGroupedByRoomAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var devices = await context.Devices.ToListAsync();

            return devices
                .GroupBy(d => d.RoomIdentifier)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        // 获取按类型分组的设备（指定房间）
        public async Task<Dictionary<string, List<DeviceModel>>> GetDevicesByRoomGroupedByTypeAsync(string roomId)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var devices = await context.Devices
                .Where(d => d.RoomIdentifier == roomId)
                .ToListAsync();

            return devices
                .GroupBy(d => d.TypeIdentifier)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        // 获取所有房间的统计信息
        public async Task<Dictionary<string, (int total, int online)>> GetRoomStatsAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var devices = await context.Devices.ToListAsync();

            return devices
                .GroupBy(d => d.RoomIdentifier)
                .ToDictionary(
                    g => g.Key,
                    g => (
                        total: g.Count(),
                        online: g.Count(d => d.IsOn && d.StatusText != "离线")
                    )
                );
        }

        // 获取设备类型统计
        public async Task<List<DeviceTypeStat>> GetDeviceTypeStatsAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var deviceTypes = await context.DeviceTypes.ToListAsync();
            var devices = await context.Devices.ToListAsync();

            var stats = new List<DeviceTypeStat>();

            foreach (var type in deviceTypes)
            {
                var typeDevices = devices.Where(d => d.TypeIdentifier == type.TypeId).ToList();
                stats.Add(new DeviceTypeStat
                {
                    TypeId = type.TypeId,
                    TypeName = type.TypeName,
                    Icon = type.Icon,
                    Count = typeDevices.Count,
                    OnlineCount = typeDevices.Count(d => d.IsOn && d.StatusText != "离线")
                });
            }

            return stats;
        }

        // ==================== 设备管理方法 ====================

        // 添加设备（异步）
        public async Task<DeviceModel> AddDeviceAsync(DeviceAddModel newDevice)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                // 获取房间
                var room = await context.Rooms.FirstOrDefaultAsync(r => r.RoomId == newDevice.RoomId);
                if (room == null)
                {
                    throw new Exception($"房间不存在: {newDevice.RoomId}");
                }

                // 获取设备类型
                var deviceType = await context.DeviceTypes.FirstOrDefaultAsync(t => t.TypeId == newDevice.TypeId);
                if (deviceType == null)
                {
                    throw new Exception($"设备类型不存在: {newDevice.TypeId}");
                }

                // 生成设备编号
                string deviceNumber = newDevice.DeviceNumber;
                if (string.IsNullOrEmpty(deviceNumber))
                {
                    // 获取该房间该类型的最大编号
                    var existingDevices = await context.Devices
                        .Where(d => d.RoomIdentifier == newDevice.RoomId && d.TypeIdentifier == newDevice.TypeId)
                        .ToListAsync();

                    int maxNumber = existingDevices
                        .Select(d => int.TryParse(d.DeviceNumber, out int num) ? num : 0)
                        .DefaultIfEmpty(0)
                        .Max();

                    deviceNumber = (maxNumber + 1).ToString("D3");
                }

                string fullDeviceId = $"{newDevice.RoomId}-{newDevice.TypeId}-{deviceNumber}";

                // 检查是否已存在
                var exists = await context.Devices.AnyAsync(d => d.FullDeviceId == fullDeviceId);
                if (exists)
                {
                    throw new Exception($"设备已存在: {fullDeviceId}");
                }

                double powerValue = ParsePowerValue(newDevice.Power);

                // 新设备默认为离线状态
                string statusText = "离线";
                string detail = "等待连接";

                // 根据设备类型设置详情
                switch (newDevice.TypeId)
                {
                    case "ac": detail = "空调 · 等待连接"; break;
                    case "lock": detail = "门锁 · 等待连接"; break;
                    case "temp-sensor": detail = "温度传感器 · 等待连接"; break;
                    case "humidity-sensor": detail = "湿度传感器 · 等待连接"; break;
                    case "fan": detail = "风扇 · 等待连接"; break;
                    case "motor": detail = "电机 · 等待连接"; break;
                    case "light": detail = "灯光 · 等待连接"; break;
                    case "camera": detail = "摄像头 · 等待连接"; break;
                }

                var device = new DeviceModel
                {
                    Name = newDevice.Name,
                    DeviceNumber = deviceNumber,
                    FullDeviceId = fullDeviceId,
                    RoomId = room.Id,
                    DeviceTypeId = deviceType.Id,
                    RoomIdentifier = newDevice.RoomId,
                    TypeIdentifier = newDevice.TypeId,
                    Icon = newDevice.Icon ?? deviceType.Icon,
                    IsOn = false,
                    StatusText = statusText,
                    Detail = detail,
                    Power = newDevice.Power,
                    PowerValue = powerValue,
                    Progress = newDevice.Progress,
                    ProgressColor = "#a0a0a0",
                    Temperature = newDevice.Temperature,
                    Humidity = newDevice.Humidity,
                    MotorSpeed = newDevice.MotorSpeed,
                    Mode = newDevice.Mode,
                    Direction = newDevice.Direction,
                    CreatedAt = DateTime.Now
                };

                await context.Devices.AddAsync(device);
                await context.SaveChangesAsync();

                // 更新房间设备计数
                room.DeviceCount = await context.Devices.CountAsync(d => d.RoomIdentifier == room.RoomId);
                room.OnlineCount = await context.Devices.CountAsync(d => d.RoomIdentifier == room.RoomId && d.IsOn && d.StatusText != "离线");
                await context.SaveChangesAsync();

                _logger.LogInformation($"设备添加成功: {device.Name} ({fullDeviceId})");
                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加设备失败");
                throw;
            }
        }

        // 添加设备（同步版本）
        public DeviceModel AddDevice(DeviceAddModel newDevice)
        {
            return Task.Run(async () => await AddDeviceAsync(newDevice)).Result;
        }

        // 删除设备（异步）
        public async Task<bool> DeleteDeviceAsync(int id)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var device = await context.Devices.FindAsync(id);
                if (device != null)
                {
                    context.Devices.Remove(device);
                    await context.SaveChangesAsync();

                    // 更新房间统计
                    var room = await context.Rooms.FindAsync(device.RoomId);
                    if (room != null)
                    {
                        room.DeviceCount = await context.Devices.CountAsync(d => d.RoomIdentifier == room.RoomId);
                        room.OnlineCount = await context.Devices.CountAsync(d => d.RoomIdentifier == room.RoomId && d.IsOn && d.StatusText != "离线");
                        await context.SaveChangesAsync();
                    }

                    _logger.LogInformation($"设备删除成功: ID {id}, {device.FullDeviceId}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除设备失败 ID: {id}");
                return false;
            }
        }

        // 删除设备（同步版本）
        public bool DeleteDevice(int id)
        {
            return Task.Run(async () => await DeleteDeviceAsync(id)).Result;
        }

        // ==================== 设备状态更新方法 ====================

        // 更新设备状态
        public async Task<bool> UpdateDeviceStatusAsync(int id, bool isOn, string statusText)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var device = await context.Devices.FindAsync(id);
                if (device != null)
                {
                    device.IsOn = isOn;
                    device.StatusText = statusText;
                    device.UpdatedAt = DateTime.Now;

                    if (!isOn && statusText == "离线")
                    {
                        device.ProgressColor = "#a0a0a0";
                    }
                    else
                    {
                        device.ProgressColor = device.IsOn ? null : "#a0b8d0";
                    }

                    await context.SaveChangesAsync();

                    // 更新房间在线计数
                    var room = await context.Rooms.FindAsync(device.RoomId);
                    if (room != null)
                    {
                        room.OnlineCount = await context.Devices.CountAsync(d => d.RoomId == room.Id && d.IsOn && d.StatusText != "离线");
                        await context.SaveChangesAsync();
                    }

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备状态失败 ID: {id}");
                return false;
            }
        }

        public bool UpdateDeviceStatus(int id, bool isOn, string statusText)
        {
            return Task.Run(async () => await UpdateDeviceStatusAsync(id, isOn, statusText)).Result;
        }

        // 更新设备温度
        public async Task<bool> UpdateDeviceTemperatureAsync(int id, double temperature)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var device = await context.Devices.FindAsync(id);
                if (device != null && device.TypeIdentifier == "temp-sensor")
                {
                    device.Temperature = temperature;
                    device.UpdatedAt = DateTime.Now;
                    if (device.IsOn)
                    {
                        device.StatusText = $"温度 {temperature:F1}°C";
                    }
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备温度失败 ID: {id}");
                return false;
            }
        }

        // 更新设备湿度/电量
        public async Task<bool> UpdateDeviceHumidityAsync(int id, int humidity)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var device = await context.Devices.FindAsync(id);
                if (device != null)
                {
                    device.Humidity = humidity;
                    device.UpdatedAt = DateTime.Now;
                    if (device.TypeIdentifier == "humidity-sensor" && device.IsOn)
                    {
                        device.StatusText = $"湿度 {humidity}%";
                    }
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备湿度失败 ID: {id}");
                return false;
            }
        }

        // 更新设备电机/风扇转速
        public async Task<bool> UpdateDeviceMotorSpeedAsync(int id, int speed)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var device = await context.Devices.FindAsync(id);
                if (device != null && (device.TypeIdentifier == "motor" || device.TypeIdentifier == "fan"))
                {
                    device.MotorSpeed = speed;
                    device.UpdatedAt = DateTime.Now;
                    if (device.TypeIdentifier == "fan" && device.IsOn)
                    {
                        device.StatusText = $"风速 {speed}档";
                    }
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备转速失败 ID: {id}");
                return false;
            }
        }

        // 更新设备模式（空调）
        public async Task<bool> UpdateDeviceModeAsync(int id, string mode)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var device = await context.Devices.FindAsync(id);
                if (device != null && device.TypeIdentifier == "ac")
                {
                    device.Mode = mode;
                    device.UpdatedAt = DateTime.Now;

                    if (device.IsOn)
                    {
                        string modeText = mode switch
                        {
                            "cool" => "制冷",
                            "heat" => "制热",
                            "fan" => "送风",
                            "dry" => "除湿",
                            "auto" => "自动",
                            _ => mode
                        };
                        device.StatusText = $"{modeText} {device.Temperature ?? 23}°C";
                    }

                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备模式失败 ID: {id}");
                return false;
            }
        }

        // 更新设备方向（电机）
        public async Task<bool> UpdateDeviceDirectionAsync(int id, string direction)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var device = await context.Devices.FindAsync(id);
                if (device != null && device.TypeIdentifier == "motor")
                {
                    device.Direction = direction;
                    device.UpdatedAt = DateTime.Now;
                    if (device.IsOn)
                    {
                        device.StatusText = direction switch
                        {
                            "forward" => "正转",
                            "reverse" => "反转",
                            _ => "停止"
                        };
                    }
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备方向失败 ID: {id}");
                return false;
            }
        }

        // 更新空调温度
        public async Task<bool> UpdateDeviceAcTemperatureAsync(int id, double temperature)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var device = await context.Devices.FindAsync(id);
                if (device != null && device.TypeIdentifier == "ac")
                {
                    device.Temperature = temperature;
                    device.UpdatedAt = DateTime.Now;

                    if (device.IsOn)
                    {
                        string modeText = device.Mode switch
                        {
                            "cool" => "制冷",
                            "heat" => "制热",
                            "fan" => "送风",
                            "dry" => "除湿",
                            "auto" => "自动",
                            _ => device.Mode ?? "制冷"
                        };
                        device.StatusText = $"{modeText} {temperature}°C";
                    }

                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新空调温度失败 ID: {id}");
                return false;
            }
        }

        // 更新设备功率
        public async Task<bool> UpdateDevicePowerAsync(int id, double power)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var device = await context.Devices.FindAsync(id);
                if (device != null)
                {
                    device.PowerValue = power / 1000;
                    device.Power = power >= 1000 ? $"{(power / 1000):F2}kW" : $"{power:F0}W";
                    device.UpdatedAt = DateTime.Now;
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备功率失败 ID: {id}");
                return false;
            }
        }

        // ==================== 空调额外属性更新 ====================

        public async Task<bool> UpdateDeviceSwingVerticalAsync(int id, bool enabled)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var device = await context.Devices.FindAsync(id);
                if (device != null && device.TypeIdentifier == "ac")
                {
                    device.SwingVertical = enabled;
                    device.UpdatedAt = DateTime.Now;
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新空调上下扫风失败 ID: {id}");
                return false;
            }
        }

        public async Task<bool> UpdateDeviceSwingHorizontalAsync(int id, bool enabled)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var device = await context.Devices.FindAsync(id);
                if (device != null && device.TypeIdentifier == "ac")
                {
                    device.SwingHorizontal = enabled;
                    device.UpdatedAt = DateTime.Now;
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新空调左右扫风失败 ID: {id}");
                return false;
            }
        }

        public async Task<bool> UpdateDeviceLightAsync(int id, bool enabled)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var device = await context.Devices.FindAsync(id);
                if (device != null && device.TypeIdentifier == "ac")
                {
                    device.Light = enabled;
                    device.UpdatedAt = DateTime.Now;
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新空调灯光失败 ID: {id}");
                return false;
            }
        }

        public async Task<bool> UpdateDeviceQuietAsync(int id, bool enabled)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var device = await context.Devices.FindAsync(id);
                if (device != null && device.TypeIdentifier == "ac")
                {
                    device.Quiet = enabled;
                    device.UpdatedAt = DateTime.Now;
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新空调静音失败 ID: {id}");
                return false;
            }
        }

        // ==================== 批量操作方法 ====================

        // 批量更新设备（用于TCP批量更新）
        public async Task<int> BulkUpdateDevicesAsync(List<DeviceModel> updatedDevices)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                int updateCount = 0;
                var affectedRooms = new HashSet<int>();

                foreach (var updatedDevice in updatedDevices)
                {
                    var existingDevice = await context.Devices.FindAsync(updatedDevice.Id);
                    if (existingDevice != null)
                    {
                        existingDevice.IsOn = updatedDevice.IsOn;
                        existingDevice.StatusText = updatedDevice.StatusText;
                        existingDevice.Temperature = updatedDevice.Temperature;
                        existingDevice.Humidity = updatedDevice.Humidity;
                        existingDevice.MotorSpeed = updatedDevice.MotorSpeed;
                        existingDevice.Mode = updatedDevice.Mode;
                        existingDevice.Direction = updatedDevice.Direction;
                        existingDevice.PowerValue = updatedDevice.PowerValue;
                        existingDevice.Power = updatedDevice.Power;
                        existingDevice.UpdatedAt = DateTime.Now;

                        affectedRooms.Add(existingDevice.RoomId);
                        updateCount++;
                    }
                }

                await context.SaveChangesAsync();

                // 更新受影响的房间统计
                foreach (var roomId in affectedRooms)
                {
                    var room = await context.Rooms.FindAsync(roomId);
                    if (room != null)
                    {
                        room.OnlineCount = await context.Devices.CountAsync(d => d.RoomId == roomId && d.IsOn && d.StatusText != "离线");
                    }
                }
                await context.SaveChangesAsync();

                return updateCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新设备失败");
                return 0;
            }
        }

        // 重置所有设备为离线
        public async Task ResetAllDevicesToOfflineAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var devices = await context.Devices.ToListAsync();
                foreach (var device in devices)
                {
                    device.IsOn = false;
                    device.StatusText = "离线";
                    device.ProgressColor = "#a0a0a0";
                    device.UpdatedAt = DateTime.Now;
                }

                var rooms = await context.Rooms.ToListAsync();
                foreach (var room in rooms)
                {
                    room.OnlineCount = 0;
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("所有设备已重置为离线状态");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置设备状态失败");
            }
        }

        // ==================== 统计方法 ====================

        // 获取设备统计信息
        public async Task<(int total, int online)> GetDeviceStatsAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var devices = await context.Devices.ToListAsync();
                int total = devices.Count;
                int online = devices.Count(d => d.IsOn && d.StatusText != "离线");
                return (total, online);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备统计失败");
                return (0, 0);
            }
        }

        // 获取房间设备统计
        public async Task<Dictionary<string, (int total, int online)>> GetAllRoomsStatsAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var rooms = await context.Rooms.ToListAsync();
                var devices = await context.Devices.ToListAsync();

                return rooms.ToDictionary(
                    r => r.RoomId,
                    r => (
                        total: devices.Count(d => d.RoomIdentifier == r.RoomId),
                        online: devices.Count(d => d.RoomIdentifier == r.RoomId && d.IsOn && d.StatusText != "离线")
                    )
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取房间统计失败");
                return new Dictionary<string, (int total, int online)>();
            }
        }

        // ==================== 辅助方法 ====================

        // 解析功率值
        private double ParsePowerValue(string powerText)
        {
            if (string.IsNullOrEmpty(powerText)) return 0;

            powerText = powerText.ToLower().Trim();

            if (powerText.Contains('w'))
            {
                double value = double.TryParse(powerText.Replace("w", "").Trim(), out var result) ? result : 0;
                return value / 1000;
            }

            if (powerText.Contains("kw"))
            {
                return double.TryParse(powerText.Replace("kw", "").Trim(), out var result) ? result : 0;
            }

            if (double.TryParse(powerText, out var numValue))
            {
                return numValue / 1000;
            }

            return 0;
        }

        // 生成完整设备ID
        public string GenerateFullDeviceId(string roomId, string typeId, string deviceNumber)
        {
            return $"{roomId}-{typeId}-{deviceNumber}";
        }

        // 解析完整设备ID
        public (string roomId, string typeId, string deviceNumber) ParseFullDeviceId(string fullDeviceId)
        {
            var parts = fullDeviceId.Split('-');
            if (parts.Length == 3)
            {
                return (parts[0], parts[1], parts[2]);
            }
            return ("", "", "");
        }
    }
}