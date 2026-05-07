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

            using var context = _dbContextFactory.CreateDbContext();
            context.Database.EnsureCreated();

            _logger.LogInformation($"DeviceDataService 初始化完成，数据库路径: {context.DbPath}");
        }

        // 辅助：将类型标识转换为显示名称
        private string GetDeviceTypeDisplay(string typeId)
        {
            return typeId switch
            {
                "light" => "灯光",
                "ac" => "空调",
                "lock" => "门锁",
                "camera" => "摄像头",
                "fan" => "风扇",
                "temp-sensor" => "温度传感器",
                "humidity-sensor" => "湿度传感器",
                "motor" => "电机",
                _ => "设备",
            };
        }

        private string GetModeText(string? mode)
        {
            return mode switch
            {
                "cool" => "制冷",
                "heat" => "制热",
                "fan" => "送风",
                "dry" => "除湿",
                "auto" => "自动",
                _ => mode ?? "制冷"
            };
        }

        private string GetDirectionText(string? direction)
        {
            return direction switch
            {
                "forward" => "正转",
                "reverse" => "反转",
                "stop" => "停止",
                _ => "停止"
            };
        }

        // ==================== ADO.NET 辅助方法 ====================

        private DeviceModel MapToDeviceModel(System.Data.Common.DbDataReader reader)
        {
            return new DeviceModel
            {
                Id = reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                DeviceNumber = reader.IsDBNull(2) ? "000" : reader.GetString(2),
                FullDeviceId = reader.IsDBNull(3) ? "" : reader.GetString(3),
                RoomId = reader.GetInt32(4),
                DeviceTypeId = reader.GetInt32(5),
                RoomIdentifier = reader.IsDBNull(6) ? "unknown" : reader.GetString(6),
                TypeIdentifier = reader.IsDBNull(7) ? "unknown" : reader.GetString(7),
                Icon = reader.IsDBNull(8) ? "fa-microchip" : reader.GetString(8),
                IsOn = reader.GetInt32(9) == 1,
                StatusText = reader.IsDBNull(10) ? "离线" : reader.GetString(10),
                Detail = reader.IsDBNull(11) ? "等待连接" : reader.GetString(11),
                Progress = reader.GetInt32(12),
                ProgressColor = reader.IsDBNull(13) ? null : reader.GetString(13),
                Temperature = reader.IsDBNull(14) ? null : reader.GetDouble(14),
                Humidity = reader.IsDBNull(15) ? null : reader.GetInt32(15),
                MotorSpeed = reader.IsDBNull(16) ? null : reader.GetInt32(16),
                Mode = reader.IsDBNull(17) ? null : reader.GetString(17),
                SwingVertical = reader.IsDBNull(18) ? null : reader.GetBoolean(18),
                SwingHorizontal = reader.IsDBNull(19) ? null : reader.GetBoolean(19),
                Light = reader.IsDBNull(20) ? null : reader.GetBoolean(20),
                Quiet = reader.IsDBNull(21) ? null : reader.GetBoolean(21),
                Direction = reader.IsDBNull(22) ? null : reader.GetString(22),
                CreatedAt = reader.GetDateTime(23),
                UpdatedAt = reader.IsDBNull(24) ? null : reader.GetDateTime(24),
                TemperatureValue = reader.IsDBNull(25) ? null : reader.GetDouble(25),
                HumidityValue = reader.IsDBNull(26) ? null : reader.GetDouble(26),
                BatteryLevel = reader.IsDBNull(27) ? null : reader.GetInt32(27),
                Brightness = reader.IsDBNull(28) ? null : reader.GetInt32(28),
                ColorTemperature = reader.IsDBNull(29) ? null : reader.GetInt32(29),
                AcTemperature = reader.IsDBNull(30) ? null : reader.GetInt32(30),
                AcMode = reader.IsDBNull(31) ? null : reader.GetString(31),
                AcFanSpeed = reader.IsDBNull(32) ? null : reader.GetString(32),
                AcSwingVertical = reader.IsDBNull(33) ? null : reader.GetBoolean(33),
                AcSwingHorizontal = reader.IsDBNull(34) ? null : reader.GetBoolean(34),
                AcLight = reader.IsDBNull(35) ? null : reader.GetBoolean(35),
                AcQuiet = reader.IsDBNull(36) ? null : reader.GetBoolean(36),
                FanSpeed = reader.IsDBNull(37) ? null : reader.GetInt32(37),
                FanSwing = reader.IsDBNull(38) ? null : reader.GetBoolean(38),
                MotorDirection = reader.IsDBNull(39) ? null : reader.GetString(39),
                LastUnlockTime = reader.IsDBNull(40) ? null : reader.GetDateTime(40),
                UnlockMethod = reader.IsDBNull(41) ? null : reader.GetString(41),
                IsRecording = reader.IsDBNull(42) ? null : reader.GetBoolean(42),
                MotionDetected = reader.IsDBNull(43) ? null : reader.GetBoolean(43),
                NightMode = reader.IsDBNull(44) ? null : reader.GetString(44),
                Power = reader.IsDBNull(45) ? "0W" : reader.GetString(45),
                PowerValue = reader.GetDouble(46)
            };
        }

        private async Task<DeviceModel?> GetDeviceByIdRawAsync(int id)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        Id, Name, DeviceNumber, FullDeviceId, RoomId, DeviceTypeId,
                        RoomIdentifier, TypeIdentifier, Icon, IsOn, StatusText, Detail,
                        Progress, ProgressColor, Temperature, Humidity, MotorSpeed,
                        Mode, SwingVertical, SwingHorizontal, Light, Quiet, Direction,
                        CreatedAt, UpdatedAt, TemperatureValue, HumidityValue, BatteryLevel,
                        Brightness, ColorTemperature, AcTemperature, AcMode, AcFanSpeed,
                        AcSwingVertical, AcSwingHorizontal, AcLight, AcQuiet, FanSpeed, FanSwing,
                        MotorDirection, LastUnlockTime, UnlockMethod, IsRecording, MotionDetected, NightMode,
                        Power, PowerValue
                    FROM Devices 
                    WHERE Id = @id
                    LIMIT 1";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToDeviceModel(reader);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取设备失败 ID: {id}");
                return null;
            }
        }

        private async Task<DeviceModel?> GetDeviceByFullIdRawAsync(string fullDeviceId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        Id, Name, DeviceNumber, FullDeviceId, RoomId, DeviceTypeId,
                        RoomIdentifier, TypeIdentifier, Icon, IsOn, StatusText, Detail,
                        Progress, ProgressColor, Temperature, Humidity, MotorSpeed,
                        Mode, SwingVertical, SwingHorizontal, Light, Quiet, Direction,
                        CreatedAt, UpdatedAt, TemperatureValue, HumidityValue, BatteryLevel,
                        Brightness, ColorTemperature, AcTemperature, AcMode, AcFanSpeed,
                        AcSwingVertical, AcSwingHorizontal, AcLight, AcQuiet, FanSpeed, FanSwing,
                        MotorDirection, LastUnlockTime, UnlockMethod, IsRecording, MotionDetected, NightMode,
                        Power, PowerValue
                    FROM Devices 
                    WHERE FullDeviceId = @fullDeviceId
                    LIMIT 1";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@fullDeviceId";
                idParam.Value = fullDeviceId;
                cmd.Parameters.Add(idParam);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToDeviceModel(reader);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取设备失败 FullDeviceId: {fullDeviceId}");
                return null;
            }
        }

        private async Task<List<DeviceModel>> GetAllDevicesRawAsync()
        {
            var devices = new List<DeviceModel>();
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"
                    SELECT 
                        Id, Name, DeviceNumber, FullDeviceId, RoomId, DeviceTypeId,
                        RoomIdentifier, TypeIdentifier, Icon, IsOn, StatusText, Detail,
                        Progress, ProgressColor, Temperature, Humidity, MotorSpeed,
                        Mode, SwingVertical, SwingHorizontal, Light, Quiet, Direction,
                        CreatedAt, UpdatedAt, TemperatureValue, HumidityValue, BatteryLevel,
                        Brightness, ColorTemperature, AcTemperature, AcMode, AcFanSpeed,
                        AcSwingVertical, AcSwingHorizontal, AcLight, AcQuiet, FanSpeed, FanSwing,
                        MotorDirection, LastUnlockTime, UnlockMethod, IsRecording, MotionDetected, NightMode,
                        Power, PowerValue
                    FROM Devices";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    devices.Add(MapToDeviceModel(reader));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有设备失败");
            }
            return devices;
        }

        private async Task<RoomModel?> GetRoomByIdRawAsync(int id)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = "SELECT Id, RoomId, RoomName, Description, DeviceCount, OnlineCount, CreatedAt, UpdatedAt FROM Rooms WHERE Id = @id LIMIT 1";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new RoomModel
                    {
                        Id = reader.GetInt32(0),
                        RoomId = reader.GetString(1),
                        RoomName = reader.GetString(2),
                        Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                        DeviceCount = reader.GetInt32(4),
                        OnlineCount = reader.GetInt32(5),
                        CreatedAt = reader.GetDateTime(6),
                        UpdatedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
                    };
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"获取房间失败 ID: {id}");
                return null;
            }
        }

        // ==================== 基本查询方法 ====================

        public List<DeviceModel> GetAllDevices()
        {
            return Task.Run(async () => await GetAllDevicesAsync()).Result;
        }

        public async Task<List<DeviceModel>> GetAllDevicesAsync()
        {
            return await GetAllDevicesRawAsync();
        }

        public async Task<List<DeviceModel>> GetDevicesByRoomAsync(string roomId)
        {
            var devices = await GetAllDevicesAsync();
            return devices.Where(d => d.RoomIdentifier == roomId).ToList();
        }

        public async Task<List<DeviceModel>> GetDevicesByTypeAsync(string typeId)
        {
            var devices = await GetAllDevicesAsync();
            return devices.Where(d => d.TypeIdentifier == typeId).ToList();
        }

        public async Task<DeviceModel?> GetDeviceByFullIdAsync(string fullDeviceId)
        {
            return await GetDeviceByFullIdRawAsync(fullDeviceId);
        }

        public async Task<DeviceModel?> GetDeviceByIdAsync(int id)
        {
            return await GetDeviceByIdRawAsync(id);
        }

        // ==================== 分组查询方法 ====================

        public async Task<Dictionary<string, List<DeviceModel>>> GetDevicesGroupedByRoomAsync()
        {
            var devices = await GetAllDevicesAsync();
            return devices
                .GroupBy(d => d.RoomIdentifier)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public async Task<Dictionary<string, List<DeviceModel>>> GetDevicesByRoomGroupedByTypeAsync(string roomId)
        {
            var devices = await GetAllDevicesAsync();
            var roomDevices = devices.Where(d => d.RoomIdentifier == roomId).ToList();
            return roomDevices
                .GroupBy(d => d.TypeIdentifier)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public async Task<Dictionary<string, (int total, int online)>> GetRoomStatsAsync()
        {
            var devices = await GetAllDevicesAsync();
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

        public async Task<List<DeviceTypeStat>> GetDeviceTypeStatsAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var deviceTypes = await context.DeviceTypes.ToListAsync();
            var devices = await GetAllDevicesAsync();

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

        public async Task<DeviceModel> AddDeviceAsync(DeviceAddModel newDevice)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                var room = await context.Rooms.FirstOrDefaultAsync(r => r.RoomId == newDevice.RoomId);
                if (room == null)
                {
                    throw new Exception($"房间不存在: {newDevice.RoomId}");
                }

                var deviceType = await context.DeviceTypes.FirstOrDefaultAsync(t => t.TypeId == newDevice.TypeId);
                if (deviceType == null)
                {
                    throw new Exception($"设备类型不存在: {newDevice.TypeId}");
                }

                string deviceNumber = newDevice.DeviceNumber;
                if (string.IsNullOrEmpty(deviceNumber))
                {
                    var existingDevices = await context.Devices
                        .Where(d => d.RoomIdentifier == newDevice.RoomId && d.TypeIdentifier == newDevice.TypeId)
                        .ToListAsync();

                    int maxNumber = existingDevices
                        .Select(d => int.TryParse(d.DeviceNumber, out int num) ? num : 0)
                        .DefaultIfEmpty(0)
                        .Max();

                    deviceNumber = (maxNumber + 1).ToString("D3");
                }

                string fullDeviceId = $"{GetTypeAbbr(newDevice.TypeId)}-{GetRoomAbbr(newDevice.RoomId)}-{deviceNumber}";

                var exists = await context.Devices.AnyAsync(d => d.FullDeviceId == fullDeviceId);
                if (exists)
                {
                    throw new Exception($"设备已存在: {fullDeviceId}");
                }

                double powerValue = ParsePowerValue(newDevice.Power);

                string statusText = "离线";
                string detail = "等待连接";

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

                    TemperatureValue = newDevice.TypeId == "temp-sensor" ? (newDevice.TemperatureValue ?? newDevice.Temperature) : null,
                    HumidityValue = newDevice.TypeId == "humidity-sensor" ? (newDevice.HumidityValue ?? (newDevice.Humidity.HasValue ? newDevice.Humidity.Value : (double?)null)) : null,
                    BatteryLevel = newDevice.BatteryLevel ?? newDevice.Humidity,
                    Brightness = newDevice.Brightness,
                    ColorTemperature = newDevice.ColorTemperature,
                    AcTemperature = newDevice.TypeId == "ac" ? (newDevice.AcTemperature ?? (int?)(newDevice.Temperature)) : null,
                    AcMode = newDevice.AcMode ?? newDevice.Mode,
                    AcFanSpeed = newDevice.AcFanSpeed,
                    FanSpeed = newDevice.TypeId == "fan" ? (newDevice.FanSpeed ?? newDevice.MotorSpeed) : null,
                    MotorSpeed = newDevice.TypeId == "motor" ? newDevice.MotorSpeed : null,
                    MotorDirection = newDevice.MotorDirection ?? newDevice.Direction,

                    Temperature = newDevice.TypeId == "temp-sensor" ? (newDevice.TemperatureValue ?? newDevice.Temperature) :
                                 newDevice.TypeId == "humidity-sensor" ? (newDevice.HumidityValue ?? (newDevice.Humidity.HasValue ? newDevice.Humidity.Value : (double?)null)) :
                                 newDevice.TypeId == "ac" ? newDevice.AcTemperature ?? newDevice.Temperature : newDevice.Temperature,
                    Humidity = (newDevice.BatteryLevel ?? newDevice.Humidity) ?? (newDevice.TypeId == "humidity-sensor" ? (int?)(newDevice.HumidityValue ?? newDevice.Temperature) : null),
                    Mode = newDevice.AcMode ?? newDevice.Mode,
                    Direction = newDevice.MotorDirection ?? newDevice.Direction,

                    CreatedAt = DateTime.Now
                };

                await context.Devices.AddAsync(device);
                await context.SaveChangesAsync();

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

        public DeviceModel AddDevice(DeviceAddModel newDevice)
        {
            return Task.Run(async () => await AddDeviceAsync(newDevice)).Result;
        }

        public async Task<bool> DeleteDeviceAsync(int id)
        {
            try
            {
                var device = await GetDeviceByIdRawAsync(id);
                if (device == null) return false;

                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Devices WHERE Id = @id";
                    var idParam = cmd.CreateParameter();
                    idParam.ParameterName = "@id";
                    idParam.Value = id;
                    cmd.Parameters.Add(idParam);
                    await cmd.ExecuteNonQueryAsync();
                }

                int deviceCount, onlineCount;

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM Devices WHERE RoomIdentifier = @roomId";
                    var roomIdParam = cmd.CreateParameter();
                    roomIdParam.ParameterName = "@roomId";
                    roomIdParam.Value = device.RoomIdentifier;
                    cmd.Parameters.Add(roomIdParam);
                    deviceCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM Devices WHERE RoomIdentifier = @roomId AND IsOn = 1 AND StatusText != '离线'";
                    var roomIdParam = cmd.CreateParameter();
                    roomIdParam.ParameterName = "@roomId";
                    roomIdParam.Value = device.RoomIdentifier;
                    cmd.Parameters.Add(roomIdParam);
                    onlineCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE Rooms SET DeviceCount = @deviceCount, OnlineCount = @onlineCount WHERE Id = @roomId";
                    var deviceCountParam = cmd.CreateParameter();
                    deviceCountParam.ParameterName = "@deviceCount";
                    deviceCountParam.Value = deviceCount;
                    cmd.Parameters.Add(deviceCountParam);

                    var onlineCountParam = cmd.CreateParameter();
                    onlineCountParam.ParameterName = "@onlineCount";
                    onlineCountParam.Value = onlineCount;
                    cmd.Parameters.Add(onlineCountParam);

                    var roomIdParam = cmd.CreateParameter();
                    roomIdParam.ParameterName = "@roomId";
                    roomIdParam.Value = device.RoomId;
                    cmd.Parameters.Add(roomIdParam);

                    await cmd.ExecuteNonQueryAsync();
                }

                _logger.LogInformation($"设备删除成功: ID {id}, {device.FullDeviceId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除设备失败 ID: {id}");
                return false;
            }
        }

        public bool DeleteDevice(int id)
        {
            return Task.Run(async () => await DeleteDeviceAsync(id)).Result;
        }

        // ==================== 设备状态更新方法 ====================

        public async Task<bool> UpdateDeviceStatusAsync(int id, bool isOn, string statusText)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET IsOn = @isOn, 
                               StatusText = @statusText, 
                               Detail = CASE 
                                   WHEN @isOn = 0 AND @statusText = '离线' THEN 
                                       CASE TypeIdentifier
                                           WHEN 'camera' THEN '摄像头 · 离线'
                                           WHEN 'light' THEN '灯光 · 离线'
                                           WHEN 'temp-sensor' THEN '温度传感器 · 离线'
                                           WHEN 'humidity-sensor' THEN '湿度传感器 · 离线'
                                           WHEN 'ac' THEN '空调 · 离线'
                                           WHEN 'lock' THEN '门锁 · 离线'
                                           WHEN 'fan' THEN '风扇 · 离线'
                                           WHEN 'motor' THEN '电机 · 离线'
                                           ELSE '设备 · 离线'
                                       END
                                   WHEN @isOn = 1 AND @statusText != '离线' THEN
                                       CASE TypeIdentifier
                                           WHEN 'camera' THEN '摄像头 · 在线'
                                           WHEN 'light' THEN '灯光 · 在线'
                                           WHEN 'temp-sensor' THEN '温度传感器 · 在线'
                                           WHEN 'humidity-sensor' THEN '湿度传感器 · 在线'
                                           WHEN 'ac' THEN '空调 · 在线'
                                           WHEN 'lock' THEN '门锁 · 在线'
                                           WHEN 'fan' THEN '风扇 · 在线'
                                           WHEN 'motor' THEN '电机 · 在线'
                                           ELSE '设备 · 在线'
                                       END
                                   ELSE Detail
                               END,
                               ProgressColor = CASE WHEN @isOn = 1 AND @statusText != '离线' THEN '#4caf50' ELSE '#a0a0a0' END,
                               UpdatedAt = @now
                           WHERE Id = @id";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var isOnParam = cmd.CreateParameter();
                isOnParam.ParameterName = "@isOn";
                isOnParam.Value = isOn ? 1 : 0;
                cmd.Parameters.Add(isOnParam);

                var statusParam = cmd.CreateParameter();
                statusParam.ParameterName = "@statusText";
                statusParam.Value = statusText;
                cmd.Parameters.Add(statusParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();

                var room = await GetRoomByIdRawAsync((await GetDeviceByIdRawAsync(id))?.RoomId ?? 0);
                if (room != null)
                {
                    int onlineCount;
                    using (var countCmd = connection.CreateCommand())
                    {
                        countCmd.CommandText = "SELECT COUNT(*) FROM Devices WHERE RoomId = @roomId AND IsOn = 1 AND StatusText != '离线'";
                        var roomIdParam = countCmd.CreateParameter();
                        roomIdParam.ParameterName = "@roomId";
                        roomIdParam.Value = room.Id;
                        countCmd.Parameters.Add(roomIdParam);
                        onlineCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                    }

                    using (var updateCmd = connection.CreateCommand())
                    {
                        updateCmd.CommandText = "UPDATE Rooms SET OnlineCount = @onlineCount WHERE Id = @roomId";
                        var onlineCountParam = updateCmd.CreateParameter();
                        onlineCountParam.ParameterName = "@onlineCount";
                        onlineCountParam.Value = onlineCount;
                        updateCmd.Parameters.Add(onlineCountParam);

                        var roomIdParam = updateCmd.CreateParameter();
                        roomIdParam.ParameterName = "@roomId";
                        roomIdParam.Value = room.Id;
                        updateCmd.Parameters.Add(roomIdParam);

                        await updateCmd.ExecuteNonQueryAsync();
                    }
                }

                return true;
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

        // ==================== 传感器专用更新方法 ====================

        public async Task<bool> UpdateTemperatureSensorAsync(int id, double temperature, int? batteryLevel = null)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET TemperatureValue = @temperature,
                               Temperature = @temperature,
                               StatusText = CASE WHEN IsOn = 1 THEN @statusText ELSE StatusText END,
                               BatteryLevel = COALESCE(@batteryLevel, BatteryLevel),
                               Humidity = COALESCE(@batteryLevel, Humidity),
                               UpdatedAt = @now
                           WHERE Id = @id";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var tempParam = cmd.CreateParameter();
                tempParam.ParameterName = "@temperature";
                tempParam.Value = temperature;
                cmd.Parameters.Add(tempParam);

                var statusParam = cmd.CreateParameter();
                statusParam.ParameterName = "@statusText";
                statusParam.Value = $"温度 {temperature:F1}°C";
                cmd.Parameters.Add(statusParam);

                var batteryParam = cmd.CreateParameter();
                batteryParam.ParameterName = "@batteryLevel";
                batteryParam.Value = batteryLevel.HasValue ? (object)batteryLevel.Value : DBNull.Value;
                cmd.Parameters.Add(batteryParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation($"温度传感器 ID:{id} 更新: 温度={temperature}°C, 电量={batteryLevel}%");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新温度传感器失败 ID: {id}");
                return false;
            }
        }

        public async Task<bool> UpdateHumiditySensorAsync(int id, double humidity, int? batteryLevel = null)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET HumidityValue = @humidity,
                               Temperature = @humidity,
                               StatusText = CASE WHEN IsOn = 1 THEN @statusText ELSE StatusText END,
                               BatteryLevel = COALESCE(@batteryLevel, BatteryLevel),
                               Humidity = COALESCE(@batteryLevel, Humidity),
                               UpdatedAt = @now
                           WHERE Id = @id";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var humParam = cmd.CreateParameter();
                humParam.ParameterName = "@humidity";
                humParam.Value = humidity;
                cmd.Parameters.Add(humParam);

                var statusParam = cmd.CreateParameter();
                statusParam.ParameterName = "@statusText";
                statusParam.Value = $"湿度 {humidity:F0}%";
                cmd.Parameters.Add(statusParam);

                var batteryParam = cmd.CreateParameter();
                batteryParam.ParameterName = "@batteryLevel";
                batteryParam.Value = batteryLevel.HasValue ? (object)batteryLevel.Value : DBNull.Value;
                cmd.Parameters.Add(batteryParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation($"湿度传感器 ID:{id} 更新: 湿度={humidity}%, 电量={batteryLevel}%");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新湿度传感器失败 ID: {id}");
                return false;
            }
        }

        public async Task<bool> UpdateDeviceTemperatureAsync(int id, double temperature)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET Temperature = @temperature,
                               UpdatedAt = @now
                           WHERE Id = @id";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var tempParam = cmd.CreateParameter();
                tempParam.ParameterName = "@temperature";
                tempParam.Value = temperature;
                cmd.Parameters.Add(tempParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备温度失败 ID: {id}");
                return false;
            }
        }

        public async Task<bool> UpdateDeviceHumidityAsync(int id, int humidity)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET Humidity = @humidity,
                               UpdatedAt = @now
                           WHERE Id = @id";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var humParam = cmd.CreateParameter();
                humParam.ParameterName = "@humidity";
                humParam.Value = humidity;
                cmd.Parameters.Add(humParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备湿度/电量失败 ID: {id}");
                return false;
            }
        }

        public async Task<bool> UpdateDeviceMotorSpeedAsync(int id, int speed)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET MotorSpeed = @speed,
                               FanSpeed = CASE WHEN TypeIdentifier = 'fan' THEN @speed ELSE FanSpeed END,
                               StatusText = CASE WHEN TypeIdentifier = 'fan' AND IsOn = 1 THEN @fanStatus 
                                                WHEN TypeIdentifier = 'motor' AND IsOn = 1 THEN @motorStatus
                                                ELSE StatusText END,
                               UpdatedAt = @now
                           WHERE Id = @id";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var speedParam = cmd.CreateParameter();
                speedParam.ParameterName = "@speed";
                speedParam.Value = speed;
                cmd.Parameters.Add(speedParam);

                var fanStatusParam = cmd.CreateParameter();
                fanStatusParam.ParameterName = "@fanStatus";
                fanStatusParam.Value = $"风速 {speed}档";
                cmd.Parameters.Add(fanStatusParam);

                var motorStatusParam = cmd.CreateParameter();
                motorStatusParam.ParameterName = "@motorStatus";
                motorStatusParam.Value = $"{GetDirectionText(await GetMotorDirectionAsync(id))} {speed}rpm";
                cmd.Parameters.Add(motorStatusParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备转速失败 ID: {id}");
                return false;
            }
        }

        private async Task<string?> GetMotorDirectionAsync(int id)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = "SELECT MotorDirection FROM Devices WHERE Id = @id";
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString();
            }
            catch
            {
                return "stop";
            }
        }

        public async Task<bool> UpdateDeviceModeAsync(int id, string mode)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET AcMode = @mode,
                               Mode = @mode,
                               UpdatedAt = @now
                           WHERE Id = @id";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var modeParam = cmd.CreateParameter();
                modeParam.ParameterName = "@mode";
                modeParam.Value = mode;
                cmd.Parameters.Add(modeParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备模式失败 ID: {id}");
                return false;
            }
        }

        public async Task<bool> UpdateDeviceDirectionAsync(int id, string direction)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET MotorDirection = @direction,
                               Direction = @direction,
                               UpdatedAt = @now
                           WHERE Id = @id";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var dirParam = cmd.CreateParameter();
                dirParam.ParameterName = "@direction";
                dirParam.Value = direction;
                cmd.Parameters.Add(dirParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备方向失败 ID: {id}");
                return false;
            }
        }

        public async Task<bool> UpdateDeviceAcTemperatureAsync(int id, double temperature)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET AcTemperature = @temperature,
                               Temperature = @temperature,
                               UpdatedAt = @now
                           WHERE Id = @id";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var tempParam = cmd.CreateParameter();
                tempParam.ParameterName = "@temperature";
                tempParam.Value = temperature;
                cmd.Parameters.Add(tempParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新空调温度失败 ID: {id}");
                return false;
            }
        }

        public async Task<bool> UpdateDevicePowerAsync(int id, double power)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var powerKW = power / 1000;
                var powerDisplay = power >= 1000 ? $"{(power / 1000):F2}kW" : $"{power:F0}W";

                var sql = @"UPDATE Devices 
                           SET PowerValue = @powerValue,
                               Power = @powerDisplay,
                               UpdatedAt = @now
                           WHERE Id = @id";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var powerValueParam = cmd.CreateParameter();
                powerValueParam.ParameterName = "@powerValue";
                powerValueParam.Value = powerKW;
                cmd.Parameters.Add(powerValueParam);

                var powerDisplayParam = cmd.CreateParameter();
                powerDisplayParam.ParameterName = "@powerDisplay";
                powerDisplayParam.Value = powerDisplay;
                cmd.Parameters.Add(powerDisplayParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                return true;
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
            return await UpdateBoolFieldAsync(id, "AcSwingVertical", "SwingVertical", enabled);
        }

        public async Task<bool> UpdateDeviceSwingHorizontalAsync(int id, bool enabled)
        {
            return await UpdateBoolFieldAsync(id, "AcSwingHorizontal", "SwingHorizontal", enabled);
        }

        public async Task<bool> UpdateDeviceLightAsync(int id, bool enabled)
        {
            return await UpdateBoolFieldAsync(id, "AcLight", "Light", enabled);
        }

        public async Task<bool> UpdateDeviceQuietAsync(int id, bool enabled)
        {
            return await UpdateBoolFieldAsync(id, "AcQuiet", "Quiet", enabled);
        }

        private async Task<bool> UpdateBoolFieldAsync(int id, string newField, string oldField, bool value)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = $"UPDATE Devices SET {newField} = @value, {oldField} = @value, UpdatedAt = @now WHERE Id = @id";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var valParam = cmd.CreateParameter();
                valParam.ParameterName = "@value";
                valParam.Value = value ? 1 : 0;
                cmd.Parameters.Add(valParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@id";
                idParam.Value = id;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新布尔字段失败 ID: {id}");
                return false;
            }
        }

        // ==================== 批量操作方法 ====================

        public async Task<int> BulkUpdateDevicesAsync(List<DeviceModel> updatedDevices)
        {
            int updateCount = 0;
            foreach (var device in updatedDevices)
            {
                try
                {
                    using var context = await _dbContextFactory.CreateDbContextAsync();
                    var connection = context.Database.GetDbConnection();
                    await connection.OpenAsync();

                    var sql = @"UPDATE Devices 
                               SET IsOn = @isOn,
                                   StatusText = @statusText,
                                   TemperatureValue = @temperatureValue,
                                   HumidityValue = @humidityValue,
                                   BatteryLevel = @batteryLevel,
                                   Brightness = @brightness,
                                   ColorTemperature = @colorTemperature,
                                   AcTemperature = @acTemperature,
                                   AcMode = @acMode,
                                   AcFanSpeed = @acFanSpeed,
                                   AcSwingVertical = @acSwingVertical,
                                   AcSwingHorizontal = @acSwingHorizontal,
                                   AcLight = @acLight,
                                   AcQuiet = @acQuiet,
                                   FanSpeed = @fanSpeed,
                                   FanSwing = @fanSwing,
                                   MotorSpeed = @motorSpeed,
                                   MotorDirection = @motorDirection,
                                   LastUnlockTime = @lastUnlockTime,
                                   UnlockMethod = @unlockMethod,
                                   IsRecording = @isRecording,
                                   MotionDetected = @motionDetected,
                                   NightMode = @nightMode,
                                   Temperature = @temperature,
                                   Humidity = @humidity,
                                   Mode = @mode,
                                   Direction = @direction,
                                   PowerValue = @powerValue,
                                   Power = @power,
                                   UpdatedAt = @now
                               WHERE Id = @id";

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;

                    AddParameter(cmd, "@isOn", device.IsOn ? 1 : 0);
                    AddParameter(cmd, "@statusText", device.StatusText ?? "");
                    AddParameter(cmd, "@temperatureValue", device.TemperatureValue);
                    AddParameter(cmd, "@humidityValue", device.HumidityValue);
                    AddParameter(cmd, "@batteryLevel", device.BatteryLevel);
                    AddParameter(cmd, "@brightness", device.Brightness);
                    AddParameter(cmd, "@colorTemperature", device.ColorTemperature);
                    AddParameter(cmd, "@acTemperature", device.AcTemperature);
                    AddParameter(cmd, "@acMode", device.AcMode);
                    AddParameter(cmd, "@acFanSpeed", device.AcFanSpeed);
                    AddParameter(cmd, "@acSwingVertical", device.AcSwingVertical);
                    AddParameter(cmd, "@acSwingHorizontal", device.AcSwingHorizontal);
                    AddParameter(cmd, "@acLight", device.AcLight);
                    AddParameter(cmd, "@acQuiet", device.AcQuiet);
                    AddParameter(cmd, "@fanSpeed", device.FanSpeed);
                    AddParameter(cmd, "@fanSwing", device.FanSwing);
                    AddParameter(cmd, "@motorSpeed", device.MotorSpeed);
                    AddParameter(cmd, "@motorDirection", device.MotorDirection);
                    AddParameter(cmd, "@lastUnlockTime", device.LastUnlockTime);
                    AddParameter(cmd, "@unlockMethod", device.UnlockMethod);
                    AddParameter(cmd, "@isRecording", device.IsRecording);
                    AddParameter(cmd, "@motionDetected", device.MotionDetected);
                    AddParameter(cmd, "@nightMode", device.NightMode);
                    AddParameter(cmd, "@temperature", device.Temperature);
                    AddParameter(cmd, "@humidity", device.Humidity);
                    AddParameter(cmd, "@mode", device.Mode);
                    AddParameter(cmd, "@direction", device.Direction);
                    AddParameter(cmd, "@powerValue", device.PowerValue);
                    AddParameter(cmd, "@power", device.Power ?? "");
                    AddParameter(cmd, "@now", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    AddParameter(cmd, "@id", device.Id);

                    await cmd.ExecuteNonQueryAsync();
                    updateCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"批量更新设备失败 ID: {device.Id}");
                }
            }
            return updateCount;
        }

        private void AddParameter(System.Data.Common.DbCommand cmd, string name, object? value)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(param);
        }

        public async Task ResetAllDevicesToOfflineAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                // 重置所有设备为离线状态 - 所有设备类型统一处理
                var sql = @"UPDATE Devices 
                   SET IsOn = 0, 
                       StatusText = '离线',
                       Detail = CASE 
                           WHEN TypeIdentifier = 'camera' THEN '摄像头 · 离线'
                           WHEN TypeIdentifier = 'light' THEN '灯光 · 离线'
                           WHEN TypeIdentifier = 'temp-sensor' THEN '温度传感器 · 离线'
                           WHEN TypeIdentifier = 'humidity-sensor' THEN '湿度传感器 · 离线'
                           WHEN TypeIdentifier = 'ac' THEN '空调 · 离线'
                           WHEN TypeIdentifier = 'lock' THEN '门锁 · 离线'
                           WHEN TypeIdentifier = 'fan' THEN '风扇 · 离线'
                           WHEN TypeIdentifier = 'motor' THEN '电机 · 离线'
                           ELSE '设备 · 离线' 
                       END,
                       ProgressColor = '#a0a0a0',
                       UpdatedAt = @now";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation($"已重置 {rowsAffected} 个设备为离线状态");

                // 重置房间在线统计
                var roomSql = "UPDATE Rooms SET OnlineCount = 0";
                using var roomCmd = connection.CreateCommand();
                roomCmd.CommandText = roomSql;
                await roomCmd.ExecuteNonQueryAsync();

                // 发送更新到前端
                var updatedDevices = await GetAllDevicesRawAsync();
                await SendDevicesUpdateViaSignalR(updatedDevices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置设备状态失败");
            }
        }

        private async Task SendDevicesUpdateViaSignalR(List<DeviceModel> devices)
        {
            try
            {
                // 这个方法需要在 TcpServerService 中实现 SignalR 推送
                // 暂时记录日志
                _logger.LogInformation($"需要推送设备列表更新，共 {devices.Count} 个设备");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送设备列表更新失败");
            }
        }

        public async Task SyncDeviceStatusWithTcpConnections(Dictionary<string, bool> onlineStatus)
        {
            try
            {
                var devices = await GetAllDevicesAsync();
                int updatedCount = 0;

                foreach (var device in devices)
                {
                    if (onlineStatus.TryGetValue(device.FullDeviceId, out bool isOnline))
                    {
                        if (isOnline && (device.StatusText == "离线" || !device.IsOn))
                        {
                            await UpdateDeviceStatusAsync(device.Id, true, "在线");
                            updatedCount++;
                        }
                        else if (!isOnline && device.StatusText != "离线")
                        {
                            await UpdateDeviceStatusAsync(device.Id, false, "离线");
                            updatedCount++;
                        }
                    }
                }

                if (updatedCount > 0)
                {
                    _logger.LogInformation($"设备状态同步完成，更新了 {updatedCount} 个设备");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "同步设备状态失败");
            }
        }

        // ==================== 统计方法 ====================

        public async Task<(int total, int online)> GetDeviceStatsAsync()
        {
            var devices = await GetAllDevicesAsync();
            int total = devices.Count;
            int online = devices.Count(d => d.IsOn && d.StatusText != "离线");
            return (total, online);
        }

        public async Task<Dictionary<string, (int total, int online)>> GetAllRoomsStatsAsync()
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();
            var rooms = await context.Rooms.ToListAsync();
            var devices = await GetAllDevicesAsync();

            return rooms.ToDictionary(
                r => r.RoomId,
                r => (
                    total: devices.Count(d => d.RoomIdentifier == r.RoomId),
                    online: devices.Count(d => d.RoomIdentifier == r.RoomId && d.IsOn && d.StatusText != "离线")
                )
            );
        }

        // ==================== 辅助方法 ====================

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

        private string GetTypeAbbr(string typeId)
        {
            return typeId switch
            {
                "fan" => "fan",
                "humidity-sensor" => "hum",
                "temp-sensor" => "temp",
                "light" => "light",
                "ac" => "ac",
                "lock" => "lock",
                "camera" => "cam",
                "motor" => "motor",
                _ => "dev"
            };
        }

        private string GetRoomAbbr(string roomId)
        {
            return roomId switch
            {
                "living" => "liv",
                "master-bedroom" => "mbd",
                "second-bedroom" => "sbd",
                "kitchen" => "kit",
                "bathroom" => "bat",
                "dining" => "din",
                "entrance" => "ent",
                _ => "unk"
            };
        }

        public string GenerateFullDeviceId(string roomId, string typeId, string deviceNumber)
        {
            return $"{GetTypeAbbr(typeId)}-{GetRoomAbbr(roomId)}-{deviceNumber}";
        }

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