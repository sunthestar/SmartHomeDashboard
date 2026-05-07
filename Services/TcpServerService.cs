using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using SmartHomeDashboard.Hubs;
using SmartHomeDashboard.Models;
using SmartHomeDashboard.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace SmartHomeDashboard.Services
{
    public class TcpServerService : IHostedService
    {
        private readonly ILogger<TcpServerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly DeviceDataService _deviceDataService;
        private readonly IHubContext<DeviceHub> _hubContext;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private TcpListener? _tcpListener;
        private bool _isRunning;
        private readonly Dictionary<string, TcpClientInfo> _connectedClients;
        private readonly Dictionary<string, Func<TcpMessage, Task>> _messageHandlers;
        private Task? _serverTask;
        private CancellationTokenSource? _cancellationTokenSource;
        private Timer? _heartbeatCheckTimer;
        private Timer? _syncTimer;
        private readonly int _heartbeatTimeoutSeconds = 90;

        public event EventHandler<TcpMessage>? OnMessageReceived;
        public event EventHandler<TcpDevice>? OnDeviceConnected;
        public event EventHandler<TcpDevice>? OnDeviceDisconnected;
        public event EventHandler<TelemetryData>? OnTelemetryReceived;

        public TcpServerService(
            ILogger<TcpServerService> logger,
            IConfiguration configuration,
            DeviceDataService deviceDataService,
            IHubContext<DeviceHub> hubContext,
            IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _deviceDataService = deviceDataService;
            _hubContext = hubContext;
            _dbContextFactory = dbContextFactory;
            _connectedClients = new Dictionary<string, TcpClientInfo>();
            _messageHandlers = new Dictionary<string, Func<TcpMessage, Task>>();

            if (int.TryParse(_configuration["TcpSettings:HeartbeatTimeout"], out var timeout))
            {
                _heartbeatTimeoutSeconds = timeout;
            }

            RegisterDefaultHandlers();
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

        private string GetIconForDeviceType(string typeId)
        {
            return typeId switch
            {
                "light" => "fa-lightbulb",
                "ac" => "fa-wind",
                "lock" => "fa-door-open",
                "camera" => "fa-camera",
                "fan" => "fa-fan",
                "temp-sensor" => "fa-thermometer-half",
                "humidity-sensor" => "fa-tint",
                "motor" => "fa-cogs",
                _ => "fa-microchip"
            };
        }

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
                _ => "设备"
            };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _serverTask = Task.Run(() => RunServerAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            _heartbeatCheckTimer = new Timer(CheckHeartbeats, null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
            _syncTimer = new Timer(async _ => await SyncDeviceStatus(), null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            _heartbeatCheckTimer?.Change(Timeout.Infinite, 0);
            _heartbeatCheckTimer?.Dispose();
            _syncTimer?.Change(Timeout.Infinite, 0);
            _syncTimer?.Dispose();

            try
            {
                if (_serverTask != null)
                {
                    await _serverTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("TCP服务器停止超时");
            }

            _tcpListener?.Stop();

            foreach (var client in _connectedClients.Values)
            {
                try
                {
                    client.Client?.Close();
                }
                catch { }
            }
            _connectedClients.Clear();
        }

        private async Task RunServerAsync(CancellationToken cancellationToken)
        {
            try
            {
                var port = int.Parse(_configuration["TcpSettings:Port"] ?? "8888");
                _tcpListener = new TcpListener(IPAddress.Any, port);
                _tcpListener.Start();
                _isRunning = true;

                _logger.LogInformation($"========================================");
                _logger.LogInformation($"TCP服务器启动成功，监听端口: {port}");
                _logger.LogInformation($"心跳超时时间: {_heartbeatTimeoutSeconds}秒");
                _logger.LogInformation($"服务器时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"========================================");

                while (!cancellationToken.IsCancellationRequested && _isRunning)
                {
                    try
                    {
                        _logger.LogInformation("等待客户端连接...");
                        var client = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
                        var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                        _logger.LogInformation($"? 收到新连接！客户端: {endpoint?.Address}:{endpoint?.Port}");
                        _ = Task.Run(() => HandleClientAsync(client), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "接受客户端连接失败");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TCP服务器启动失败");
            }
            finally
            {
                _tcpListener?.Stop();
                _logger.LogInformation("TCP服务器已停止");
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
            var ipAddress = endpoint?.Address.ToString() ?? "未知";
            var port = endpoint?.Port ?? 0;
            int messageCount = 0;
            string? deviceId = null;

            _logger.LogInformation($"");
            _logger.LogInformation($"========== 新客户端连接 ==========");
            _logger.LogInformation($"IP地址: {ipAddress}:{port}");
            _logger.LogInformation($"连接时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _logger.LogInformation($"==================================");

            var clientInfo = new TcpClientInfo
            {
                Client = client,
                Stream = client.GetStream(),
                IpAddress = ipAddress,
                Port = port,
                ConnectedTime = DateTime.Now,
                LastHeartbeat = DateTime.Now,
                LastSeen = DateTime.Now
            };

            string tempKey = $"temp_{ipAddress}_{port}_{DateTime.Now.Ticks}";
            lock (_connectedClients)
            {
                _connectedClients[tempKey] = clientInfo;
                _logger.LogInformation($"临时连接已添加: {tempKey}");
            }

            try
            {
                using var reader = new StreamReader(clientInfo.Stream, Encoding.UTF8);
                var buffer = new char[4096];
                var messageBuffer = new StringBuilder();

                while (_isRunning && client.Connected)
                {
                    var bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    messageBuffer.Append(buffer, 0, bytesRead);
                    var messages = messageBuffer.ToString().Split('\n');

                    for (int i = 0; i < messages.Length - 1; i++)
                    {
                        var messageStr = messages[i].Trim();
                        if (!string.IsNullOrEmpty(messageStr))
                        {
                            messageCount++;
                            await ProcessMessageAsync(messageStr, clientInfo, messageCount);

                            clientInfo.LastHeartbeat = DateTime.Now;
                            clientInfo.LastSeen = DateTime.Now;

                            if (deviceId == null)
                            {
                                try
                                {
                                    var msg = JsonSerializer.Deserialize<JsonDocument>(messageStr);
                                    if (msg.RootElement.TryGetProperty("deviceId", out var devIdElement))
                                    {
                                        var newDeviceId = devIdElement.GetString();
                                        if (!string.IsNullOrEmpty(newDeviceId) && newDeviceId != "")
                                        {
                                            deviceId = newDeviceId;
                                            lock (_connectedClients)
                                            {
                                                if (_connectedClients.ContainsKey(tempKey))
                                                {
                                                    var info = _connectedClients[tempKey];
                                                    _connectedClients.Remove(tempKey);
                                                    info.DeviceId = deviceId;
                                                    _connectedClients[deviceId] = info;
                                                    _logger.LogInformation($"? 设备ID已从消息中获取并更新: {deviceId}");
                                                }
                                            }
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }

                    messageBuffer.Clear();
                    messageBuffer.Append(messages.Last());
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "客户端连接处理异常（正常断开）");
            }
            finally
            {
                string disconnectedDeviceId = deviceId ?? "";

                lock (_connectedClients)
                {
                    if (deviceId != null && _connectedClients.ContainsKey(deviceId))
                    {
                        _connectedClients.Remove(deviceId);
                    }
                    if (_connectedClients.ContainsKey(tempKey))
                    {
                        _connectedClients.Remove(tempKey);
                    }
                }

                if (!string.IsNullOrEmpty(disconnectedDeviceId) && disconnectedDeviceId != "temp" && !disconnectedDeviceId.StartsWith("temp_"))
                {
                    await SetDeviceOfflineInDatabase(disconnectedDeviceId);
                }

                _logger.LogInformation($"");
                _logger.LogInformation($"========== 客户端断开连接 ==========");
                _logger.LogInformation($"IP地址: {ipAddress}:{port}");
                _logger.LogInformation($"设备ID: {disconnectedDeviceId ?? "未知"}");
                _logger.LogInformation($"断开时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"总计接收消息: {messageCount} 条");
                _logger.LogInformation($"====================================");

                try
                {
                    client.Close();
                }
                catch { }
            }
        }

        private async Task ProcessMessageAsync(string messageStr, TcpClientInfo clientInfo, int messageCount)
        {
            _logger.LogInformation($"");
            _logger.LogInformation($"----- 收到第 {messageCount} 条消息 -----");
            _logger.LogInformation($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            _logger.LogInformation($"来源: {clientInfo.IpAddress}:{clientInfo.Port}");
            _logger.LogInformation($"原始数据: {messageStr}");
            _logger.LogInformation($"----------------------------------------");

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var message = JsonSerializer.Deserialize<TcpMessage>(messageStr, options);

                if (message != null)
                {
                    _logger.LogInformation($"解析结果: 类型={message.Type}, 设备ID={message.DeviceId}");

                    message.Timestamp = DateTime.UtcNow;
                    OnMessageReceived?.Invoke(this, message);

                    await ProcessMessageByTypeAsync(message, clientInfo);
                }
                else
                {
                    _logger.LogWarning($"消息解析失败: 返回null");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError($"JSON解析错误: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"处理消息时发生未知错误: {ex.Message}");
            }

            _logger.LogInformation($"----- 消息处理完成 -----");
            _logger.LogInformation($"");
        }

        private async Task ProcessMessageByTypeAsync(TcpMessage message, TcpClientInfo clientInfo)
        {
            foreach (var handler in _messageHandlers)
            {
                if (handler.Key == message.Type)
                {
                    try
                    {
                        await handler.Value(message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"执行消息处理器失败: {handler.Key}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(message.DeviceId))
            {
                lock (_connectedClients)
                {
                    if (_connectedClients.ContainsKey(message.DeviceId))
                    {
                        _connectedClients[message.DeviceId].LastSeen = DateTime.Now;
                        _connectedClients[message.DeviceId].LastHeartbeat = DateTime.Now;
                    }
                }
            }
        }

        private void RegisterDefaultHandlers()
        {
            _messageHandlers["register"] = async (message) =>
            {
                _logger.LogInformation("执行注册处理器");
                await HandleRegisterAsync(message);
            };

            _messageHandlers["heartbeat"] = async (message) =>
            {
                _logger.LogDebug("执行心跳处理器");
                await HandleHeartbeatAsync(message);
            };

            _messageHandlers["status"] = async (message) =>
            {
                _logger.LogDebug("执行状态处理器");
                await HandleStatusAsync(message);
            };

            _messageHandlers["telemetry"] = async (message) =>
            {
                _logger.LogDebug("执行遥测数据处理器");
                await HandleTelemetryAsync(message);
            };

            _messageHandlers["command_response"] = async (message) =>
            {
                _logger.LogDebug("执行命令响应处理器");
                await HandleCommandResponseAsync(message);
            };

            _messageHandlers["event"] = async (message) =>
            {
                _logger.LogDebug("执行事件处理器");
                await HandleEventAsync(message);
            };

            _messageHandlers["disconnect"] = async (message) =>
            {
                _logger.LogInformation("执行断开连接处理器");
                await HandleDisconnectAsync(message);
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

        private async Task<DeviceModel?> GetDeviceByNameAndRoomRawAsync(string name, string roomId)
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
                    WHERE Name = @name AND RoomIdentifier = @roomId
                    LIMIT 1";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var nameParam = cmd.CreateParameter();
                nameParam.ParameterName = "@name";
                nameParam.Value = name;
                cmd.Parameters.Add(nameParam);

                var roomParam = cmd.CreateParameter();
                roomParam.ParameterName = "@roomId";
                roomParam.Value = roomId;
                cmd.Parameters.Add(roomParam);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return MapToDeviceModel(reader);
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"查询设备失败: Name={name}, Room={roomId}");
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

        private async Task SetDeviceOfflineInDatabase(string fullDeviceId)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                // 所有设备类型统一处理为离线，摄像头也设置为"离线"
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
                       UpdatedAt = @now
                   WHERE FullDeviceId = @fullDeviceId";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@fullDeviceId";
                idParam.Value = fullDeviceId;
                cmd.Parameters.Add(idParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    _logger.LogInformation($"设备 {fullDeviceId} 已标记为离线");

                    // 同时更新房间在线统计
                    var roomSql = @"UPDATE Rooms 
                           SET OnlineCount = (
                               SELECT COUNT(*) FROM Devices 
                               WHERE RoomId = Rooms.Id AND IsOn = 1 AND StatusText != '离线'
                           ),
                           UpdatedAt = @now";

                    using var roomCmd = connection.CreateCommand();
                    roomCmd.CommandText = roomSql;
                    var roomNowParam = roomCmd.CreateParameter();
                    roomNowParam.ParameterName = "@now";
                    roomNowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    roomCmd.Parameters.Add(roomNowParam);
                    await roomCmd.ExecuteNonQueryAsync();
                }

                var updatedDevices = await GetAllDevicesRawAsync();
                await SendDevicesUpdateToClients(updatedDevices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"设置设备离线失败: {fullDeviceId}");
            }
        }

        // ==================== 设备状态更新辅助方法 ====================

        private async Task UpdateDeviceStatusText(int deviceId, string statusText)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = "UPDATE Devices SET StatusText = @statusText, UpdatedAt = @now WHERE Id = @deviceId";
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var statusParam = cmd.CreateParameter();
                statusParam.ParameterName = "@statusText";
                statusParam.Value = statusText;
                cmd.Parameters.Add(statusParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备状态文本失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceBatteryLevel(int deviceId, int batteryLevel)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = "UPDATE Devices SET BatteryLevel = @batteryLevel, Humidity = @batteryLevel, UpdatedAt = @now WHERE Id = @deviceId";
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var batteryParam = cmd.CreateParameter();
                batteryParam.ParameterName = "@batteryLevel";
                batteryParam.Value = batteryLevel;
                cmd.Parameters.Add(batteryParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备电量失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceBrightness(int deviceId, int brightness)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = "UPDATE Devices SET Brightness = @brightness, Progress = @brightness, UpdatedAt = @now WHERE Id = @deviceId";
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var brightnessParam = cmd.CreateParameter();
                brightnessParam.ParameterName = "@brightness";
                brightnessParam.Value = brightness;
                cmd.Parameters.Add(brightnessParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation($"灯光亮度已更新: DeviceId={deviceId}, Brightness={brightness}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备亮度失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceColorTemperature(int deviceId, int colorTemp)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = "UPDATE Devices SET ColorTemperature = @colorTemp, UpdatedAt = @now WHERE Id = @deviceId";
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var tempParam = cmd.CreateParameter();
                tempParam.ParameterName = "@colorTemp";
                tempParam.Value = colorTemp;
                cmd.Parameters.Add(tempParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation($"灯光色温已更新: DeviceId={deviceId}, ColorTemp={colorTemp}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备色温失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceCameraOnlineStatus(int deviceId, bool isOnline)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET Detail = CASE 
                                   WHEN @isOnline = 1 THEN '摄像头 · 在线'
                                   ELSE '摄像头 · 离线'
                               END,
                               ProgressColor = CASE WHEN @isOnline = 1 THEN '#4caf50' ELSE '#a0a0a0' END,
                               UpdatedAt = @now
                           WHERE Id = @deviceId AND TypeIdentifier = 'camera'";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var isOnlineParam = cmd.CreateParameter();
                isOnlineParam.ParameterName = "@isOnline";
                isOnlineParam.Value = isOnline ? 1 : 0;
                cmd.Parameters.Add(isOnlineParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation($"摄像头在线状态已更新: DeviceId={deviceId}, IsOnline={isOnline}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新摄像头在线状态失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceLightOnlineStatus(int deviceId, bool isOnline)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET Detail = CASE 
                                   WHEN @isOnline = 1 THEN '灯光 · 在线'
                                   ELSE '灯光 · 离线'
                               END,
                               ProgressColor = CASE WHEN @isOnline = 1 THEN '#4caf50' ELSE '#a0a0a0' END,
                               UpdatedAt = @now
                           WHERE Id = @deviceId AND TypeIdentifier = 'light'";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var isOnlineParam = cmd.CreateParameter();
                isOnlineParam.ParameterName = "@isOnline";
                isOnlineParam.Value = isOnline ? 1 : 0;
                cmd.Parameters.Add(isOnlineParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation($"灯光在线状态已更新: DeviceId={deviceId}, IsOnline={isOnline}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新灯光在线状态失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceCameraPowerStatus(int deviceId, bool isOn)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET StatusText = CASE WHEN @isOn = 1 THEN '开启' ELSE '关闭' END,
                               IsOn = @isOn,
                               UpdatedAt = @now
                           WHERE Id = @deviceId AND TypeIdentifier = 'camera'";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var isOnParam = cmd.CreateParameter();
                isOnParam.ParameterName = "@isOn";
                isOnParam.Value = isOn ? 1 : 0;
                cmd.Parameters.Add(isOnParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation($"摄像头开关状态已更新: DeviceId={deviceId}, IsOn={isOn}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新摄像头开关状态失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceOnlineStatus(int deviceId, bool isOnline, string statusText)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET IsOn = @isOnline, 
                               StatusText = @statusText,
                               ProgressColor = CASE WHEN @isOnline = 1 THEN '#4caf50' ELSE '#a0a0a0' END,
                               UpdatedAt = @now
                           WHERE Id = @deviceId";

                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var isOnlineParam = cmd.CreateParameter();
                isOnlineParam.ParameterName = "@isOnline";
                isOnlineParam.Value = isOnline ? 1 : 0;
                cmd.Parameters.Add(isOnlineParam);

                var statusParam = cmd.CreateParameter();
                statusParam.ParameterName = "@statusText";
                statusParam.Value = statusText;
                cmd.Parameters.Add(statusParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation($"设备在线状态已更新: DeviceId={deviceId}, IsOnline={isOnline}, StatusText={statusText}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备在线状态失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDevicePowerStatus(int deviceId, bool isOn)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET IsOn = @isOn, 
                               StatusText = CASE WHEN @isOn = 1 THEN '开启' ELSE '关闭' END,
                               UpdatedAt = @now 
                           WHERE Id = @deviceId";
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var isOnParam = cmd.CreateParameter();
                isOnParam.ParameterName = "@isOn";
                isOnParam.Value = isOn ? 1 : 0;
                cmd.Parameters.Add(isOnParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备电源状态失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceAcMode(int deviceId, string mode)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = "UPDATE Devices SET AcMode = @mode, Mode = @mode, UpdatedAt = @now WHERE Id = @deviceId";
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
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新空调模式失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceAcTemperature(int deviceId, int temperature)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = "UPDATE Devices SET AcTemperature = @temp, Temperature = @temp, UpdatedAt = @now WHERE Id = @deviceId";
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var tempParam = cmd.CreateParameter();
                tempParam.ParameterName = "@temp";
                tempParam.Value = temperature;
                cmd.Parameters.Add(tempParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新空调温度失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceFanSpeed(int deviceId, int speed)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = "UPDATE Devices SET FanSpeed = @speed, MotorSpeed = @speed, UpdatedAt = @now WHERE Id = @deviceId";
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var speedParam = cmd.CreateParameter();
                speedParam.ParameterName = "@speed";
                speedParam.Value = speed;
                cmd.Parameters.Add(speedParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新风扇转速失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceMotorDirection(int deviceId, string direction)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = "UPDATE Devices SET MotorDirection = @direction, Direction = @direction, UpdatedAt = @now WHERE Id = @deviceId";
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
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新电机方向失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceTemperature(int deviceId, double temperature)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET TemperatureValue = @temperature,
                               Temperature = @temperature,
                               UpdatedAt = @now
                           WHERE Id = @deviceId";

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
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备温度失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceHumidity(int deviceId, double humidity)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = @"UPDATE Devices 
                           SET HumidityValue = @humidity,
                               Temperature = @humidity,
                               UpdatedAt = @now
                           WHERE Id = @deviceId";

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
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备湿度失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceMotionDetected(int deviceId, bool motionDetected)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = "UPDATE Devices SET MotionDetected = @motionDetected, UpdatedAt = @now WHERE Id = @deviceId";
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var motionParam = cmd.CreateParameter();
                motionParam.ParameterName = "@motionDetected";
                motionParam.Value = motionDetected ? 1 : 0;
                cmd.Parameters.Add(motionParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备运动侦测失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceIsRecording(int deviceId, bool isRecording)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = "UPDATE Devices SET IsRecording = @isRecording, UpdatedAt = @now WHERE Id = @deviceId";
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var recordingParam = cmd.CreateParameter();
                recordingParam.ParameterName = "@isRecording";
                recordingParam.Value = isRecording ? 1 : 0;
                cmd.Parameters.Add(recordingParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备录制状态失败 ID: {deviceId}");
            }
        }

        private async Task UpdateDeviceNightMode(int deviceId, string nightMode)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var connection = context.Database.GetDbConnection();
                await connection.OpenAsync();

                var sql = "UPDATE Devices SET NightMode = @nightMode, UpdatedAt = @now WHERE Id = @deviceId";
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;

                var modeParam = cmd.CreateParameter();
                modeParam.ParameterName = "@nightMode";
                modeParam.Value = nightMode;
                cmd.Parameters.Add(modeParam);

                var nowParam = cmd.CreateParameter();
                nowParam.ParameterName = "@now";
                nowParam.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                cmd.Parameters.Add(nowParam);

                var idParam = cmd.CreateParameter();
                idParam.ParameterName = "@deviceId";
                idParam.Value = deviceId;
                cmd.Parameters.Add(idParam);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备夜视模式失败 ID: {deviceId}");
            }
        }

        private async Task ParseAndUpdateDeviceSpecificValues(int deviceId, string deviceType, string currentValue)
        {
            try
            {
                switch (deviceType)
                {
                    case "ac":
                        var acMatch = Regex.Match(currentValue, @"(\w+)\s+(\d+)°C");
                        if (acMatch.Success)
                        {
                            var mode = acMatch.Groups[1].Value;
                            var temp = int.Parse(acMatch.Groups[2].Value);
                            await UpdateDeviceAcMode(deviceId, mode);
                            await UpdateDeviceAcTemperature(deviceId, temp);
                        }
                        break;

                    case "fan":
                        var fanMatch = Regex.Match(currentValue, @"风速\s+(\d+)");
                        if (fanMatch.Success)
                        {
                            var speed = int.Parse(fanMatch.Groups[1].Value);
                            await UpdateDeviceFanSpeed(deviceId, speed);
                        }
                        break;

                    case "light":
                        // 灯光：只更新开关状态（StatusText），不影响在线状态
                        if (currentValue == "开启")
                        {
                            await UpdateDevicePowerStatus(deviceId, true);
                        }
                        else if (currentValue == "关闭")
                        {
                            await UpdateDevicePowerStatus(deviceId, false);
                        }
                        break;

                    case "lock":
                        bool isLocked = currentValue == "已上锁";
                        await UpdateDevicePowerStatus(deviceId, isLocked);
                        break;

                    case "camera":
                        // 摄像头：只更新开关状态，不影响在线状态
                        if (currentValue == "开启")
                        {
                            await UpdateDeviceCameraPowerStatus(deviceId, true);
                        }
                        else if (currentValue == "关闭")
                        {
                            await UpdateDeviceCameraPowerStatus(deviceId, false);
                        }
                        break;

                    case "motor":
                        await UpdateDeviceMotorDirection(deviceId, currentValue);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"解析设备特定值失败: DeviceId={deviceId}, Type={deviceType}, Value={currentValue}");
            }
        }

        // ==================== 消息处理器 ====================

        private async Task HandleRegisterAsync(TcpMessage message)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var registerData = JsonSerializer.Deserialize<RegisterMessage>(message.Data.ToString()!, options);
                if (registerData == null)
                {
                    _logger.LogError("注册消息解析失败：数据为空");
                    return;
                }

                _logger.LogInformation($"收到注册请求: 设备名={registerData.DeviceInfo.Name}, 类型={registerData.DeviceInfo.Type}, 房间={registerData.DeviceInfo.Room}, MAC={registerData.MacAddress}");

                TcpClientInfo? clientInfo = null;
                string clientKey = "";

                lock (_connectedClients)
                {
                    var tempKey = _connectedClients.Keys.FirstOrDefault(k => k.StartsWith("temp_"));
                    if (!string.IsNullOrEmpty(tempKey))
                    {
                        clientInfo = _connectedClients[tempKey];
                        clientKey = tempKey;
                        _logger.LogInformation($"找到临时连接: {tempKey}");
                    }
                }

                if (clientInfo == null)
                {
                    lock (_connectedClients)
                    {
                        var match = _connectedClients.Values
                            .Where(c => c.IpAddress == registerData.IpAddress)
                            .OrderByDescending(c => c.LastSeen)
                            .FirstOrDefault();

                        if (match != null)
                        {
                            clientInfo = match;
                            clientKey = _connectedClients.First(kv => kv.Value == match).Key;
                            _logger.LogInformation($"通过 IP 地址找到连接: {clientKey}");
                        }
                    }
                }

                if (clientInfo == null)
                {
                    _logger.LogError($"未找到可用连接，当前连接列表: {string.Join(", ", _connectedClients.Keys)}");
                    return;
                }

                if (clientInfo.Stream == null && clientInfo.Client != null)
                {
                    clientInfo.Stream = clientInfo.Client.GetStream();
                }

                var existingDevice = await GetDeviceByFullIdRawAsync(message.DeviceId);

                if (existingDevice == null)
                {
                    existingDevice = await GetDeviceByNameAndRoomRawAsync(registerData.DeviceInfo.Name, registerData.DeviceInfo.Room);
                }

                if (existingDevice != null)
                {
                    _logger.LogInformation($"设备已存在: {existingDevice.FullDeviceId}");

                    lock (_connectedClients)
                    {
                        if (!string.IsNullOrEmpty(clientKey) && _connectedClients.ContainsKey(clientKey))
                        {
                            _connectedClients.Remove(clientKey);
                        }

                        if (!_connectedClients.ContainsKey(existingDevice.FullDeviceId))
                        {
                            var existingClientInfo = new TcpClientInfo
                            {
                                Client = clientInfo.Client,
                                Stream = clientInfo.Stream,
                                DeviceId = existingDevice.FullDeviceId,
                                DeviceName = existingDevice.Name,
                                DeviceType = existingDevice.TypeIdentifier,
                                IpAddress = clientInfo.IpAddress,
                                Port = clientInfo.Port,
                                ConnectedTime = clientInfo.ConnectedTime,
                                LastSeen = DateTime.Now,
                                LastHeartbeat = DateTime.Now
                            };
                            _connectedClients[existingDevice.FullDeviceId] = existingClientInfo;
                        }
                        else
                        {
                            var existing = _connectedClients[existingDevice.FullDeviceId];
                            existing.Client = clientInfo.Client;
                            existing.Stream = clientInfo.Stream;
                            existing.LastSeen = DateTime.Now;
                            existing.LastHeartbeat = DateTime.Now;
                        }
                    }

                    // 设备在线，更新状态
                    if (existingDevice.TypeIdentifier == "camera")
                    {
                        await UpdateDeviceCameraOnlineStatus(existingDevice.Id, true);
                        // 摄像头保持原有开关状态，不自动设置为"在线"
                    }
                    else if (existingDevice.TypeIdentifier == "light")
                    {
                        await UpdateDeviceLightOnlineStatus(existingDevice.Id, true);
                        await UpdateDeviceOnlineStatus(existingDevice.Id, true, existingDevice.StatusText);
                    }
                    else
                    {
                        await UpdateDeviceOnlineStatus(existingDevice.Id, true, "在线");
                    }

                    await SendRegisterResponse(message, existingDevice.FullDeviceId, clientInfo);

                    var updatedDevicesList = await GetAllDevicesRawAsync();
                    await SendDevicesUpdateToClients(updatedDevicesList);
                    return;
                }

                // 新设备注册逻辑
                var deviceType = registerData.DeviceInfo.Type;
                var deviceRoom = registerData.DeviceInfo.Room;

                using var context = await _dbContextFactory.CreateDbContextAsync();

                var existingDevicesList = await context.Devices
                    .Where(d => d.RoomIdentifier == deviceRoom && d.TypeIdentifier == deviceType)
                    .ToListAsync();

                int maxNumber = existingDevicesList
                    .Select(d => int.TryParse(d.DeviceNumber, out int num) ? num : 0)
                    .DefaultIfEmpty(0)
                    .Max();

                int sequence = maxNumber + 1;
                string deviceNumber = sequence.ToString("D3");

                string typeAbbr = GetTypeAbbr(deviceType);
                string roomAbbr = GetRoomAbbr(deviceRoom);
                string fullDeviceId = $"{typeAbbr}-{roomAbbr}-{deviceNumber}";

                _logger.LogInformation($"生成新设备ID: {fullDeviceId}");

                var roomModel = await context.Rooms.FirstOrDefaultAsync(r => r.RoomId == deviceRoom);
                var deviceTypeModel = await context.DeviceTypes.FirstOrDefaultAsync(t => t.TypeId == deviceType);

                if (roomModel == null || deviceTypeModel == null)
                {
                    _logger.LogError($"房间或设备类型不存在: Room={deviceRoom}, Type={deviceType}");
                    return;
                }

                string initialStatusText = "离线";
                string initialDetail = $"{GetDeviceTypeDisplay(deviceType)} · 离线";

                if (deviceType == "camera")
                {
                    initialStatusText = "关闭";
                    initialDetail = "摄像头 · 离线";
                }
                else if (deviceType == "light")
                {
                    initialStatusText = "关闭";
                    initialDetail = "灯光 · 离线";
                }

                var newDevice = new DeviceModel
                {
                    Name = registerData.DeviceInfo.Name,
                    DeviceNumber = deviceNumber,
                    FullDeviceId = fullDeviceId,
                    RoomId = roomModel.Id,
                    DeviceTypeId = deviceTypeModel.Id,
                    RoomIdentifier = deviceRoom,
                    TypeIdentifier = deviceType,
                    Icon = GetIconForDeviceType(deviceType),
                    IsOn = false,
                    StatusText = initialStatusText,
                    Detail = initialDetail,
                    Power = "0W",
                    PowerValue = 0,
                    Progress = 0,
                    ProgressColor = "#a0a0a0",
                    CreatedAt = DateTime.Now
                };

                await context.Devices.AddAsync(newDevice);
                await context.SaveChangesAsync();
                _logger.LogInformation($"新设备已添加到数据库: {fullDeviceId}");

                roomModel.DeviceCount = await context.Devices.CountAsync(d => d.RoomIdentifier == deviceRoom);
                roomModel.OnlineCount = await context.Devices.CountAsync(d => d.RoomIdentifier == deviceRoom && d.IsOn && d.StatusText != "离线");
                await context.SaveChangesAsync();

                lock (_connectedClients)
                {
                    if (!string.IsNullOrEmpty(clientKey) && _connectedClients.ContainsKey(clientKey))
                    {
                        _connectedClients.Remove(clientKey);
                    }

                    if (!_connectedClients.ContainsKey(fullDeviceId))
                    {
                        var deviceClientInfo = new TcpClientInfo
                        {
                            Client = clientInfo.Client,
                            Stream = clientInfo.Stream,
                            DeviceId = fullDeviceId,
                            DeviceName = newDevice.Name,
                            DeviceType = deviceType,
                            IpAddress = clientInfo.IpAddress,
                            Port = clientInfo.Port,
                            ConnectedTime = clientInfo.ConnectedTime,
                            LastSeen = DateTime.Now,
                            LastHeartbeat = DateTime.Now
                        };
                        _connectedClients[fullDeviceId] = deviceClientInfo;
                    }
                }

                await SendRegisterResponse(message, fullDeviceId, clientInfo);

                var allDevicesList = await GetAllDevicesRawAsync();
                await SendDevicesUpdateToClients(allDevicesList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理注册消息失败");
            }
        }

        private async Task SendRegisterResponse(TcpMessage message, string fullDeviceId, TcpClientInfo clientInfo)
        {
            var response = new TcpMessage
            {
                MessageId = $"resp-{message.MessageId}",
                Type = "register_response",
                DeviceId = fullDeviceId,
                Data = new RegisterResponse
                {
                    Success = true,
                    AssignedId = fullDeviceId,
                    ServerTime = DateTime.UtcNow,
                    Config = new DeviceConfig
                    {
                        HeartbeatInterval = 30,
                        ReportInterval = 60
                    }
                }
            };

            if (clientInfo != null && clientInfo.Client != null && clientInfo.Client.Connected)
            {
                try
                {
                    var json = JsonSerializer.Serialize(response);
                    var data = Encoding.UTF8.GetBytes(json + "\n");
                    await clientInfo.Stream.WriteAsync(data, 0, data.Length);
                    await clientInfo.Stream.FlushAsync();
                    _logger.LogInformation($"已发送注册响应到设备 {fullDeviceId}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"无法发送注册响应: {ex.Message}");
                }
            }
        }

        private async Task HandleHeartbeatAsync(TcpMessage message)
        {
            try
            {
                _logger.LogInformation($"收到心跳消息: 设备ID={message.DeviceId}");

                bool needUpdate = false;

                try
                {
                    var json = message.Data is JsonElement je ? je.GetRawText() : message.Data?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(json))
                    {
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("deviceStatus", out var statusArray) && statusArray.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var statusElem in statusArray.EnumerateArray())
                            {
                                if (!statusElem.TryGetProperty("deviceId", out var devIdElem)) continue;
                                var otherDeviceId = devIdElem.GetString();
                                if (string.IsNullOrEmpty(otherDeviceId)) continue;

                                string? currentValue = null;
                                if (statusElem.TryGetProperty("currentValue", out var curValElem) && curValElem.ValueKind == JsonValueKind.String)
                                {
                                    currentValue = curValElem.GetString();
                                }

                                bool? isOnline = null;
                                if (statusElem.TryGetProperty("isOnline", out var isOnlineElem))
                                {
                                    if (isOnlineElem.ValueKind == JsonValueKind.True) isOnline = true;
                                    else if (isOnlineElem.ValueKind == JsonValueKind.False) isOnline = false;
                                    _logger.LogInformation($"解析到 isOnline 字段: {isOnline.Value} 设备: {otherDeviceId}");
                                }

                                int? batteryLevel = null;
                                if (statusElem.TryGetProperty("batteryLevel", out var batteryElem))
                                {
                                    batteryLevel = batteryElem.GetInt32();
                                }

                                var otherDevice = await GetDeviceByFullIdRawAsync(otherDeviceId);
                                if (otherDevice != null)
                                {
                                    bool deviceUpdated = false;

                                    // ========== 处理在线状态 ==========
                                    if (isOnline.HasValue)
                                    {
                                        if (otherDevice.TypeIdentifier == "camera")
                                        {
                                            await UpdateDeviceCameraOnlineStatus(otherDevice.Id, isOnline.Value);
                                            deviceUpdated = true;
                                            _logger.LogInformation($"摄像头 {otherDevice.Name} 在线状态更新: {(isOnline.Value ? "在线" : "离线")}");
                                        }
                                        else if (otherDevice.TypeIdentifier == "light")
                                        {
                                            await UpdateDeviceLightOnlineStatus(otherDevice.Id, isOnline.Value);
                                            deviceUpdated = true;
                                            _logger.LogInformation($"灯光 {otherDevice.Name} 在线状态更新: {(isOnline.Value ? "在线" : "离线")}");
                                        }
                                        else
                                        {
                                            string statusText = isOnline.Value ? (currentValue ?? "在线") : "离线";
                                            await UpdateDeviceOnlineStatus(otherDevice.Id, isOnline.Value, statusText);
                                            deviceUpdated = true;
                                            _logger.LogInformation($"设备 {otherDevice.Name} 在线状态更新: {(isOnline.Value ? "在线" : "离线")}, StatusText={statusText}");
                                        }
                                    }

                                    // 更新状态文本（只有在线时才更新温度传感器的状态文本）
                                    if (!string.IsNullOrEmpty(currentValue))
                                    {
                                        if (otherDevice.TypeIdentifier != "camera" && otherDevice.TypeIdentifier != "light")
                                        {
                                            if (otherDevice.StatusText != currentValue)
                                            {
                                                await UpdateDeviceStatusText(otherDevice.Id, currentValue);
                                                deviceUpdated = true;
                                                _logger.LogInformation($"设备 {otherDevice.Name} 状态文本更新: {currentValue}");
                                            }
                                        }

                                        await ParseAndUpdateDeviceSpecificValues(otherDevice.Id, otherDevice.TypeIdentifier, currentValue);
                                    }

                                    // 更新电量
                                    if (batteryLevel.HasValue)
                                    {
                                        await UpdateDeviceBatteryLevel(otherDevice.Id, batteryLevel.Value);
                                        deviceUpdated = true;
                                    }

                                    if (deviceUpdated)
                                    {
                                        needUpdate = true;
                                    }

                                    // 创建 TCP 连接映射
                                    if (isOnline.HasValue && isOnline.Value)
                                    {
                                        lock (_connectedClients)
                                        {
                                            if (!_connectedClients.ContainsKey(otherDeviceId) && _connectedClients.ContainsKey(message.DeviceId))
                                            {
                                                var src = _connectedClients[message.DeviceId];
                                                var mapped = new TcpClientInfo
                                                {
                                                    Client = src.Client,
                                                    Stream = src.Stream,
                                                    DeviceId = otherDeviceId,
                                                    DeviceName = otherDevice.Name,
                                                    DeviceType = otherDevice.TypeIdentifier,
                                                    IpAddress = src.IpAddress,
                                                    Port = src.Port,
                                                    ConnectedTime = src.ConnectedTime,
                                                    LastSeen = DateTime.Now,
                                                    LastHeartbeat = DateTime.Now
                                                };
                                                _connectedClients[otherDeviceId] = mapped;
                                                _logger.LogInformation($"在心跳中为设备创建TCP映射: {otherDeviceId}");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning($"心跳中设备不存在: {otherDeviceId}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理心跳消息失败");
                }

                TcpClientInfo? clientInfo = null;
                lock (_connectedClients)
                {
                    if (_connectedClients.ContainsKey(message.DeviceId))
                    {
                        clientInfo = _connectedClients[message.DeviceId];
                    }
                }

                if (clientInfo != null && clientInfo.Client != null && clientInfo.Client.Connected)
                {
                    var response = new TcpMessage
                    {
                        MessageId = $"resp-{message.MessageId}",
                        Type = "heartbeat_response",
                        DeviceId = message.DeviceId,
                        Data = new HeartbeatResponse
                        {
                            Sequence = 1,
                            ServerTime = DateTime.UtcNow
                        }
                    };

                    await SendToClientAsync(message.DeviceId, response);
                    _logger.LogInformation($"心跳响应已发送到设备 {message.DeviceId}");
                }

                if (needUpdate)
                {
                    var updatedDevices = await GetAllDevicesRawAsync();
                    await SendDevicesUpdateToClients(updatedDevices);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理心跳消息失败");
            }
        }

        private async Task HandleTelemetryAsync(TcpMessage message)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var telemetryData = JsonSerializer.Deserialize<TelemetryData>(message.Data.ToString()!, options);

                if (telemetryData != null)
                {
                    _logger.LogInformation($"设备 {message.DeviceId} 遥测数据接收");

                    OnTelemetryReceived?.Invoke(this, telemetryData);

                    var device = await GetDeviceByFullIdRawAsync(message.DeviceId);
                    bool needUpdate = false;

                    if (device != null)
                    {
                        // 检查设备是否在线
                        bool isCurrentlyOnline = device.StatusText != "离线";

                        // ========== 摄像头设备处理 ==========
                        if (device.TypeIdentifier == "camera")
                        {
                            // 如果设备当前处于离线状态，不应该通过遥测数据强制上线
                            // 只有当设备在线时才处理遥测数据
                            if (!isCurrentlyOnline)
                            {
                                _logger.LogInformation($"摄像头 {device.Name} 当前处于离线状态，忽略遥测数据更新");
                                // 仍然需要检查是否有 isOnline=true 的状态更新
                                if (telemetryData.IsOnline.HasValue && telemetryData.IsOnline.Value == true)
                                {
                                    // 明确收到上线信号才上线
                                    await UpdateDeviceCameraOnlineStatus(device.Id, true);
                                    isCurrentlyOnline = true;
                                    needUpdate = true;
                                    _logger.LogInformation($"摄像头 {device.Name} 通过遥测数据上线（明确收到上线信号）");
                                }
                                else
                                {
                                    // 如果没有明确的上线信号，不处理其他遥测数据
                                    // 但需要检查是否强制要求更新
                                    if (needUpdate)
                                    {
                                        var updatedDevices = await GetAllDevicesRawAsync();
                                        await SendDevicesUpdateToClients(updatedDevices);
                                    }
                                    return;
                                }
                            }

                            if (isCurrentlyOnline)
                            {
                                // 只有在设备在线时才处理开关状态
                                if (telemetryData.IsOn.HasValue)
                                {
                                    var statusText = telemetryData.IsOn.Value ? "开启" : "关闭";
                                    await UpdateDeviceStatusText(device.Id, statusText);
                                    await UpdateDeviceCameraPowerStatus(device.Id, telemetryData.IsOn.Value);
                                    needUpdate = true;
                                    _logger.LogInformation($"摄像头 {device.Name} 开关状态更新为: {statusText}");
                                }

                                if (telemetryData.MotionDetected.HasValue)
                                {
                                    await UpdateDeviceMotionDetected(device.Id, telemetryData.MotionDetected.Value);
                                    needUpdate = true;
                                    _logger.LogInformation($"摄像头 {device.Name} 运动侦测更新为: {telemetryData.MotionDetected.Value}");
                                }

                                if (telemetryData.IsRecording.HasValue)
                                {
                                    await UpdateDeviceIsRecording(device.Id, telemetryData.IsRecording.Value);
                                    needUpdate = true;
                                    _logger.LogInformation($"摄像头 {device.Name} 录制状态更新为: {telemetryData.IsRecording.Value}");
                                }

                                if (!string.IsNullOrEmpty(telemetryData.NightMode))
                                {
                                    await UpdateDeviceNightMode(device.Id, telemetryData.NightMode);
                                    needUpdate = true;
                                    _logger.LogInformation($"摄像头 {device.Name} 夜视模式更新为: {telemetryData.NightMode}");
                                }
                            }
                        }
                        // ========== 灯光设备处理 ==========
                        else if (device.TypeIdentifier == "light")
                        {
                            // 首先确保设备在线状态正确
                            if (!isCurrentlyOnline && telemetryData.IsOn.HasValue)
                            {
                                await UpdateDeviceLightOnlineStatus(device.Id, true);
                                await UpdateDeviceOnlineStatus(device.Id, true, telemetryData.IsOn.Value ? "开启" : "关闭");
                                isCurrentlyOnline = true;
                                needUpdate = true;
                                _logger.LogInformation($"灯光 {device.Name} 通过遥测数据上线");
                            }

                            // 更新亮度
                            if (telemetryData.Brightness.HasValue)
                            {
                                await UpdateDeviceBrightness(device.Id, telemetryData.Brightness.Value);
                                needUpdate = true;
                                _logger.LogInformation($"灯光 {device.Name} 亮度更新为 {telemetryData.Brightness.Value}%");
                            }

                            // 更新色温
                            if (telemetryData.ColorTemperature.HasValue)
                            {
                                await UpdateDeviceColorTemperature(device.Id, telemetryData.ColorTemperature.Value);
                                needUpdate = true;
                                _logger.LogInformation($"灯光 {device.Name} 色温更新为 {telemetryData.ColorTemperature.Value}K");
                            }

                            // 更新开关状态
                            if (telemetryData.IsOn.HasValue && isCurrentlyOnline)
                            {
                                var statusText = telemetryData.IsOn.Value ? "开启" : "关闭";
                                await UpdateDeviceStatusText(device.Id, statusText);
                                await UpdateDevicePowerStatus(device.Id, telemetryData.IsOn.Value);
                                needUpdate = true;
                                _logger.LogInformation($"灯光 {device.Name} 开关状态更新为: {statusText}");
                            }
                        }
                        // ========== 温度传感器处理 ==========
                        else if (telemetryData.TemperatureValue.HasValue && device.TypeIdentifier == "temp-sensor")
                        {
                            await UpdateDeviceTemperature(device.Id, telemetryData.TemperatureValue.Value);

                            if (isCurrentlyOnline)
                            {
                                var statusText = $"温度 {telemetryData.TemperatureValue.Value:F1}°C";
                                await UpdateDeviceStatusText(device.Id, statusText);
                            }
                            needUpdate = true;
                            _logger.LogInformation($"温度传感器 {device.Name} 温度更新为 {telemetryData.TemperatureValue.Value}°C，当前在线状态: {isCurrentlyOnline}");
                        }
                        // ========== 湿度传感器处理 ==========
                        else if (telemetryData.HumidityValue.HasValue && device.TypeIdentifier == "humidity-sensor")
                        {
                            await UpdateDeviceHumidity(device.Id, telemetryData.HumidityValue.Value);

                            if (isCurrentlyOnline)
                            {
                                var statusText = $"湿度 {telemetryData.HumidityValue.Value:F0}%";
                                await UpdateDeviceStatusText(device.Id, statusText);
                            }
                            needUpdate = true;
                            _logger.LogInformation($"湿度传感器 {device.Name} 湿度更新为 {telemetryData.HumidityValue.Value}%，当前在线状态: {isCurrentlyOnline}");
                        }

                        // 统一处理电量更新（所有设备类型）
                        if (telemetryData.BatteryLevel.HasValue)
                        {
                            await UpdateDeviceBatteryLevel(device.Id, telemetryData.BatteryLevel.Value);
                            needUpdate = true;
                            _logger.LogInformation($"设备 {device.Name} 电量更新为 {telemetryData.BatteryLevel.Value}%");
                        }

                        if (needUpdate)
                        {
                            var updatedDevices = await GetAllDevicesRawAsync();
                            await SendDevicesUpdateToClients(updatedDevices);

                            await _hubContext.Clients.Group(message.DeviceId).SendAsync(
                                "TelemetryUpdated",
                                message.DeviceId,
                                new
                                {
                                    deviceId = message.DeviceId,
                                    isOn = telemetryData.IsOn,
                                    brightness = telemetryData.Brightness,
                                    colorTemperature = telemetryData.ColorTemperature,
                                    motionDetected = telemetryData.MotionDetected,
                                    isRecording = telemetryData.IsRecording,
                                    nightMode = telemetryData.NightMode,
                                    temperatureValue = telemetryData.TemperatureValue,
                                    humidityValue = telemetryData.HumidityValue,
                                    batteryLevel = telemetryData.BatteryLevel,
                                    statusText = device.StatusText,
                                    timestamp = DateTime.Now
                                });

                            _logger.LogInformation($"已推送设备更新到前端: {message.DeviceId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理遥测数据失败");
            }
        }

        private async Task HandleStatusAsync(TcpMessage message)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var statusData = JsonSerializer.Deserialize<StatusData>(message.Data.ToString()!, options);

                if (statusData != null)
                {
                    _logger.LogInformation($"设备 {message.DeviceId} 状态: 在线={statusData.IsOnline}");

                    var device = await GetDeviceByFullIdRawAsync(message.DeviceId);
                    if (device != null)
                    {
                        if (statusData.IsOnline)
                        {
                            // 设备在线
                            if (device.TypeIdentifier == "camera")
                            {
                                // 摄像头：保持 StatusText 为当前状态（开启/关闭），但标记为在线
                                await UpdateDeviceCameraOnlineStatus(device.Id, true);
                                // 不改变 IsOn 和 StatusText，保持原有开关状态
                                // 如果 currentValue 有值且不是"离线"，可以更新状态
                                if (statusData.CurrentValue != null &&
                                    statusData.CurrentValue.ToString() != "离线")
                                {
                                    var currentValue = statusData.CurrentValue.ToString();
                                    if (currentValue == "开启" || currentValue == "关闭")
                                    {
                                        bool isOn = currentValue == "开启";
                                        await UpdateDeviceStatusText(device.Id, currentValue);
                                        await UpdateDeviceCameraPowerStatus(device.Id, isOn);
                                        _logger.LogInformation($"摄像头 {device.Name} 状态更新为: {currentValue}");
                                    }
                                }
                                _logger.LogInformation($"摄像头 {device.Name} 在线");
                            }
                            else if (device.TypeIdentifier == "light")
                            {
                                await UpdateDeviceLightOnlineStatus(device.Id, true);
                                await UpdateDeviceOnlineStatus(device.Id, true, device.StatusText);
                                _logger.LogInformation($"灯光 {device.Name} 在线");
                            }
                            else
                            {
                                await UpdateDeviceOnlineStatus(device.Id, true, "在线");
                                _logger.LogInformation($"设备 {device.Name} 在线");
                            }
                        }
                        else
                        {
                            // 设备离线 - 统一处理，所有设备都显示离线
                            _logger.LogInformation($"设备 {device.Name} 离线");
                            await SetDeviceOfflineInDatabase(message.DeviceId);
                        }

                        var updatedDevices = await GetAllDevicesRawAsync();
                        await SendDevicesUpdateToClients(updatedDevices);
                    }
                    else
                    {
                        _logger.LogWarning($"设备 {message.DeviceId} 不存在于数据库中");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理状态消息失败");
            }
        }

        private async Task HandleCommandResponseAsync(TcpMessage message)
        {
            try
            {
                _logger.LogInformation($"设备 {message.DeviceId} 命令响应");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理命令响应失败");
            }
        }

        private async Task HandleEventAsync(TcpMessage message)
        {
            try
            {
                _logger.LogInformation($"设备 {message.DeviceId} 事件");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理事件消息失败");
            }
        }

        private async Task HandleDisconnectAsync(TcpMessage message)
        {
            try
            {
                _logger.LogInformation($"设备 {message.DeviceId} 断开连接");
                await SetDeviceOfflineInDatabase(message.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理断开连接消息失败");
            }
        }

        public async Task SendCommandAsync(string deviceId, string command, Dictionary<string, object>? parameters = null)
        {
            _logger.LogInformation($"准备发送命令到设备 {deviceId}: {command}");

            if (!_connectedClients.ContainsKey(deviceId))
            {
                throw new Exception($"设备 {deviceId} 不在线");
            }

            var commandData = new CommandData
            {
                CommandId = $"cmd-{DateTime.Now.Ticks}",
                Command = command,
                Parameters = parameters ?? new(),
                Source = "server"
            };

            var message = new TcpMessage
            {
                MessageId = commandData.CommandId,
                Type = "command",
                DeviceId = deviceId,
                Data = commandData
            };

            await SendToClientAsync(deviceId, message);
            _logger.LogInformation($"发送命令到设备 {deviceId}: {command}");
        }

        private async Task SendToClientAsync(string deviceId, TcpMessage message)
        {
            TcpClientInfo? clientInfo = null;

            lock (_connectedClients)
            {
                if (_connectedClients.ContainsKey(deviceId))
                {
                    clientInfo = _connectedClients[deviceId];
                }
            }

            if (clientInfo == null)
            {
                throw new Exception($"设备 {deviceId} 不在线");
            }

            if (clientInfo.Client == null || !clientInfo.Client.Connected)
            {
                throw new Exception($"设备 {deviceId} 的TCP连接已断开");
            }

            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json + "\n");

            try
            {
                await clientInfo.Stream.WriteAsync(data, 0, data.Length);
                await clientInfo.Stream.FlushAsync();
                _logger.LogDebug($"发送消息到设备 {deviceId}: {message.Type}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"发送消息到设备 {deviceId} 失败");
                throw;
            }
        }

        public List<TcpDevice> GetAllDevices()
        {
            var devices = new List<TcpDevice>();

            lock (_connectedClients)
            {
                foreach (var c in _connectedClients.Values)
                {
                    if (!string.IsNullOrEmpty(c.DeviceId) && !c.DeviceId.StartsWith("temp_"))
                    {
                        devices.Add(new TcpDevice
                        {
                            DeviceId = c.DeviceId ?? "",
                            DeviceName = c.DeviceName,
                            DeviceType = c.DeviceType,
                            IpAddress = c.IpAddress,
                            Port = c.Port,
                            LastSeen = c.LastSeen,
                            IsOnline = true,
                            Properties = new Dictionary<string, object>
                            {
                                ["connectedTime"] = c.ConnectedTime,
                                ["lastHeartbeat"] = c.LastHeartbeat
                            }
                        });
                    }
                }
            }

            return devices;
        }

        public TcpDevice? GetDevice(string deviceId)
        {
            lock (_connectedClients)
            {
                if (_connectedClients.ContainsKey(deviceId))
                {
                    var c = _connectedClients[deviceId];
                    return new TcpDevice
                    {
                        DeviceId = c.DeviceId ?? "",
                        DeviceName = c.DeviceName,
                        DeviceType = c.DeviceType,
                        IpAddress = c.IpAddress,
                        Port = c.Port,
                        LastSeen = c.LastSeen,
                        IsOnline = true,
                        Properties = new Dictionary<string, object>
                        {
                            ["connectedTime"] = c.ConnectedTime,
                            ["lastHeartbeat"] = c.LastHeartbeat
                        }
                    };
                }
            }
            return null;
        }

        private async Task SyncDeviceStatus()
        {
            try
            {
                var onlineStatus = new Dictionary<string, bool>();
                lock (_connectedClients)
                {
                    foreach (var kvp in _connectedClients)
                    {
                        if (!kvp.Key.StartsWith("temp_") && !string.IsNullOrEmpty(kvp.Value.DeviceId))
                        {
                            onlineStatus[kvp.Key] = true;
                        }
                    }
                }

                var allDevices = await GetAllDevicesRawAsync();
                await SendDevicesUpdateToClients(allDevices);
                _logger.LogDebug($"设备状态同步完成，在线设备: {onlineStatus.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "同步设备状态失败");
            }
        }

        private async void CheckHeartbeats(object? state)
        {
            try
            {
                var now = DateTime.Now;
                var timeoutDevices = new List<string>();

                lock (_connectedClients)
                {
                    foreach (var kvp in _connectedClients.ToList())
                    {
                        var deviceId = kvp.Key;
                        var clientInfo = kvp.Value;

                        if (deviceId.StartsWith("temp_")) continue;
                        if (string.IsNullOrEmpty(clientInfo.DeviceId)) continue;

                        if ((now - clientInfo.LastHeartbeat).TotalSeconds > _heartbeatTimeoutSeconds)
                        {
                            timeoutDevices.Add(deviceId);
                            _logger.LogWarning($"设备 {deviceId} 心跳超时");
                        }
                    }
                }

                foreach (var deviceId in timeoutDevices)
                {
                    await HandleDeviceTimeout(deviceId);
                }

                var tempDevices = new List<string>();
                lock (_connectedClients)
                {
                    foreach (var kvp in _connectedClients.ToList())
                    {
                        if (kvp.Key.StartsWith("temp_"))
                        {
                            var clientInfo = kvp.Value;
                            if ((now - clientInfo.LastHeartbeat).TotalSeconds > _heartbeatTimeoutSeconds)
                            {
                                tempDevices.Add(kvp.Key);
                            }
                        }
                    }
                }

                foreach (var tempKey in tempDevices)
                {
                    lock (_connectedClients)
                    {
                        if (_connectedClients.TryGetValue(tempKey, out var clientInfo))
                        {
                            try
                            {
                                clientInfo.Client?.Close();
                            }
                            catch { }
                            _connectedClients.Remove(tempKey);
                            _logger.LogInformation($"清理临时连接: {tempKey}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "心跳检查过程中发生错误");
            }
        }

        private async Task HandleDeviceTimeout(string deviceId)
        {
            try
            {
                TcpClientInfo? clientInfo = null;

                lock (_connectedClients)
                {
                    if (_connectedClients.TryGetValue(deviceId, out clientInfo))
                    {
                        _connectedClients.Remove(deviceId);
                    }
                }

                if (clientInfo != null)
                {
                    var tcpDevice = new TcpDevice
                    {
                        DeviceId = deviceId,
                        DeviceName = clientInfo.DeviceName,
                        DeviceType = clientInfo.DeviceType,
                        IpAddress = clientInfo.IpAddress,
                        LastSeen = DateTime.Now,
                        IsOnline = false
                    };

                    OnDeviceDisconnected?.Invoke(this, tcpDevice);

                    try
                    {
                        clientInfo.Client?.Close();
                    }
                    catch { }

                    _logger.LogInformation($"设备 {deviceId} 因心跳超时已离线");

                    // 所有设备类型统一标记为离线
                    await SetDeviceOfflineInDatabase(deviceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理设备 {deviceId} 超时时发生错误");
            }
        }

        private async Task SendDevicesUpdateToClients(List<DeviceModel> devices)
        {
            try
            {
                var simplifiedDevices = devices.Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.DeviceNumber,
                    d.FullDeviceId,
                    d.RoomIdentifier,
                    d.TypeIdentifier,
                    d.Icon,
                    d.IsOn,
                    d.StatusText,
                    d.Detail,
                    d.Power,
                    d.PowerValue,
                    d.Progress,
                    d.ProgressColor,
                    d.Temperature,
                    d.Humidity,
                    d.MotorSpeed,
                    d.Mode,
                    d.Direction,
                    d.TemperatureValue,
                    d.HumidityValue,
                    d.BatteryLevel,
                    d.Brightness,
                    d.ColorTemperature,
                    d.AcTemperature,
                    d.AcMode,
                    d.FanSpeed,
                    d.MotorDirection,
                    d.IsRecording,
                    d.MotionDetected,
                    d.NightMode,
                    d.CreatedAt
                }).ToList();

                await _hubContext.Clients.All.SendAsync("DevicesUpdated", simplifiedDevices);
                _logger.LogDebug($"已发送设备列表更新，共 {devices.Count} 个设备");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送设备列表更新失败");
            }
        }

        public class TcpClientInfo
        {
            public TcpClient? Client { get; set; }
            public NetworkStream? Stream { get; set; }
            public string? DeviceId { get; set; }
            public string DeviceName { get; set; } = "";
            public string DeviceType { get; set; } = "";
            public string IpAddress { get; set; } = "";
            public int Port { get; set; }
            public DateTime ConnectedTime { get; set; }
            public DateTime LastSeen { get; set; } = DateTime.Now;
            public DateTime LastHeartbeat { get; set; } = DateTime.Now;
        }
    }
}