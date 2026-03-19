using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using SmartHomeDashboard.Hubs;
using SmartHomeDashboard.Models;
using SmartHomeDashboard.Data;
using Microsoft.EntityFrameworkCore;

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

        // ==================== 设备ID生成 ====================

        private async Task<string> GenerateDeviceIdAsync(string deviceType, string room)
        {
            using var context = await _dbContextFactory.CreateDbContextAsync();

            var typeAbbr = deviceType.ToLower() switch
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

            var roomAbbr = room.ToLower() switch
            {
                "living" => "liv",
                "master-bedroom" => "mbd",
                "second-bedroom" => "sbd",
                "kitchen" => "kit",
                "bathroom" => "bat",
                "dining" => "din",
                "entrance" => "ent",
                "discovered" => "disc",
                _ => "unk"
            };

            // 从数据库获取该房间该类型的最大编号
            var existingDevices = await context.Devices
                .Where(d => d.RoomIdentifier == room && d.TypeIdentifier == deviceType)
                .ToListAsync();

            int maxNumber = existingDevices
                .Select(d => int.TryParse(d.DeviceNumber, out int num) ? num : 0)
                .DefaultIfEmpty(0)
                .Max();

            int sequence = maxNumber + 1;
            var sequenceStr = sequence.ToString("D3");

            return $"{typeAbbr}-{roomAbbr}-{sequenceStr}";
        }

        private (string type, string room, int sequence) ParseDeviceId(string deviceId)
        {
            try
            {
                var parts = deviceId.Split('-');
                if (parts.Length == 3)
                {
                    var type = parts[0];
                    var room = parts[1];
                    var sequence = int.Parse(parts[2]);
                    return (type, room, sequence);
                }
            }
            catch { }

            return ("unknown", "unknown", 0);
        }

        // ==================== 消息验证 ====================

        private bool ValidateRegisterMessage(RegisterMessage message, out string error)
        {
            error = "";

            if (message.DeviceInfo == null)
            {
                error = "deviceInfo 不能为空";
                return false;
            }

            _logger.LogInformation($"验证设备信息: Name='{message.DeviceInfo.Name}', Type='{message.DeviceInfo.Type}', Room='{message.DeviceInfo.Room}'");

            if (string.IsNullOrEmpty(message.DeviceInfo.Name))
            {
                error = "设备名称不能为空";
                _logger.LogError($"设备名称为空，完整 deviceInfo: {JsonSerializer.Serialize(message.DeviceInfo)}");
                return false;
            }

            if (string.IsNullOrEmpty(message.DeviceInfo.Type))
            {
                error = "设备类型不能为空";
                return false;
            }

            if (string.IsNullOrEmpty(message.DeviceInfo.Room))
            {
                error = "房间信息不能为空";
                return false;
            }

            if (string.IsNullOrEmpty(message.MacAddress))
            {
                error = "MAC地址不能为空";
                return false;
            }

            var macRegex = new System.Text.RegularExpressions.Regex("^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$");
            if (!macRegex.IsMatch(message.MacAddress))
            {
                error = "MAC地址格式无效";
                return false;
            }

            return true;
        }

        // ==================== 服务生命周期 ====================

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _serverTask = Task.Run(() => RunServerAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            _heartbeatCheckTimer = new Timer(CheckHeartbeats, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _isRunning = false;
            _cancellationTokenSource?.Cancel();

            _heartbeatCheckTimer?.Change(Timeout.Infinite, 0);
            _heartbeatCheckTimer?.Dispose();

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
                        var client = await _tcpListener.AcceptTcpClientAsync(cancellationToken);
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

        // ==================== 心跳检查 ====================

        private async void CheckHeartbeats(object? state)
        {
            try
            {
                var now = DateTime.Now;
                var timeoutDevices = new List<string>();

                lock (_connectedClients)
                {
                    foreach (var kvp in _connectedClients)
                    {
                        var deviceId = kvp.Key;
                        var clientInfo = kvp.Value;

                        if ((now - clientInfo.LastHeartbeat).TotalSeconds > _heartbeatTimeoutSeconds)
                        {
                            timeoutDevices.Add(deviceId);
                            _logger.LogWarning($"设备 {deviceId} 心跳超时，最后心跳时间: {clientInfo.LastHeartbeat}");
                        }
                    }
                }

                foreach (var deviceId in timeoutDevices)
                {
                    await HandleDeviceTimeout(deviceId);
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

                    await UpdateDeviceOfflineStatus(deviceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理设备 {deviceId} 超时时发生错误");
            }
        }

        private async Task UpdateDeviceOfflineStatus(string deviceId)
        {
            try
            {
                var allDevices = await _deviceDataService.GetAllDevicesAsync();
                var deviceIdParts = deviceId?.Split('-');

                if (deviceIdParts != null && deviceIdParts.Length == 3)
                {
                    var typeAbbr = deviceIdParts[0];
                    var roomAbbr = deviceIdParts[1];

                    string targetType = typeAbbr switch
                    {
                        "hum" => "humidity-sensor",
                        "temp" => "temp-sensor",
                        "light" => "light",
                        "ac" => "ac",
                        "lock" => "lock",
                        "cam" => "camera",
                        "fan" => "fan",
                        "motor" => "motor",
                        _ => "unknown"
                    };

                    string targetRoom = roomAbbr switch
                    {
                        "ent" => "entrance",
                        "liv" => "living",
                        "kit" => "kitchen",
                        "mbd" => "master-bedroom",
                        "sbd" => "second-bedroom",
                        "bat" => "bathroom",
                        "din" => "dining",
                        _ => "unknown"
                    };

                    var targetDevice = allDevices.FirstOrDefault(d =>
                        d.TypeIdentifier == targetType && d.RoomIdentifier == targetRoom);

                    if (targetDevice != null && targetDevice.IsOn && targetDevice.StatusText != "离线")
                    {
                        targetDevice.IsOn = false;
                        targetDevice.StatusText = "离线";
                        await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, false, "离线");
                        _logger.LogInformation($"已更新设备 {targetDevice.Name} 状态为离线（心跳超时）");
                    }
                }

                var updatedDevices = await _deviceDataService.GetAllDevicesAsync();
                await _hubContext.Clients.All.SendAsync("DevicesUpdated", updatedDevices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新设备 {deviceId} 离线状态失败");
            }
        }

        // ==================== 客户端连接处理 ====================

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
                IpAddress = ipAddress,
                Port = port,
                ConnectedTime = DateTime.Now,
                LastHeartbeat = DateTime.Now,
                Stream = client.GetStream()
            };

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

                            if (deviceId == null)
                            {
                                try
                                {
                                    var msg = JsonSerializer.Deserialize<JsonDocument>(messageStr);
                                    if (msg.RootElement.TryGetProperty("deviceId", out var devIdElement))
                                    {
                                        deviceId = devIdElement.GetString();
                                        if (!string.IsNullOrEmpty(deviceId))
                                        {
                                            lock (_connectedClients)
                                            {
                                                if (_connectedClients.ContainsKey(deviceId))
                                                {
                                                    _connectedClients[deviceId].Client = client;
                                                    _connectedClients[deviceId].Stream = clientInfo.Stream;
                                                    _connectedClients[deviceId].LastHeartbeat = DateTime.Now;
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
                _logger.LogInformation($"");
                _logger.LogInformation($"========== 客户端断开连接 ==========");
                _logger.LogInformation($"IP地址: {ipAddress}:{port}");
                _logger.LogInformation($"设备ID: {deviceId ?? "未知"}");
                _logger.LogInformation($"断开时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _logger.LogInformation($"总计接收消息: {messageCount} 条");
                _logger.LogInformation($"====================================");

                if (!string.IsNullOrEmpty(deviceId))
                {
                    lock (_connectedClients)
                    {
                        if (_connectedClients.TryGetValue(deviceId, out var existingClient))
                        {
                            existingClient.Client = null;
                            existingClient.Stream = null;
                        }
                    }
                }

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
            _logger.LogInformation($"原始数据:");
            _logger.LogInformation(messageStr);
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
                _logger.LogError($"错误位置: {ex.Path}");
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
                _logger.LogInformation("执行心跳处理器");
                await HandleHeartbeatAsync(message);
            };

            _messageHandlers["status"] = async (message) =>
            {
                _logger.LogInformation("执行状态处理器");
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

                if (!ValidateRegisterMessage(registerData, out string validationError))
                {
                    _logger.LogError($"注册消息验证失败: {validationError}");
                    return;
                }

                _logger.LogInformation($"设备注册: {registerData.DeviceInfo.Name}, 类型: {registerData.DeviceInfo.Type}, 房间: {registerData.DeviceInfo.Room}, MAC: {registerData.MacAddress}");

                var deviceType = registerData.DeviceInfo.Type;
                var deviceRoom = registerData.DeviceInfo.Room;

                var validTypes = new[] { "fan", "temp-sensor", "humidity-sensor", "light", "ac", "lock", "camera", "motor" };
                if (!validTypes.Contains(deviceType))
                {
                    _logger.LogWarning($"未知的设备类型: {deviceType}，将使用默认类型");
                    deviceType = "unknown";
                }

                var validRooms = new[] { "living", "master-bedroom", "second-bedroom", "kitchen", "bathroom", "dining", "entrance" };
                if (!validRooms.Contains(deviceRoom))
                {
                    _logger.LogWarning($"未知的房间: {deviceRoom}，将使用默认房间");
                    deviceRoom = "discovered";
                }

                var deviceId = await GenerateDeviceIdAsync(deviceType, deviceRoom);

                var clientInfo = _connectedClients.Values.FirstOrDefault(c => c.IpAddress == registerData.IpAddress);

                if (clientInfo == null)
                {
                    _logger.LogInformation($"为IP {registerData.IpAddress} 创建新的客户端信息");

                    clientInfo = new TcpClientInfo
                    {
                        IpAddress = registerData.IpAddress,
                        ConnectedTime = DateTime.Now,
                        LastSeen = DateTime.Now,
                        LastHeartbeat = DateTime.Now
                    };
                }

                clientInfo.DeviceId = deviceId;
                clientInfo.DeviceName = registerData.DeviceInfo.Name;
                clientInfo.DeviceType = deviceType;

                lock (_connectedClients)
                {
                    _connectedClients[deviceId] = clientInfo;
                }

                _logger.LogInformation($"客户端信息已更新: {deviceId}");

                var (typeAbbr, roomAbbr, sequence) = ParseDeviceId(deviceId);

                var tcpDevice = new TcpDevice
                {
                    DeviceId = deviceId,
                    DeviceName = registerData.DeviceInfo.Name,
                    DeviceType = deviceType,
                    IpAddress = registerData.IpAddress,
                    LastSeen = DateTime.Now,
                    IsOnline = true,
                    Properties = new Dictionary<string, object>
                    {
                        ["typeAbbr"] = typeAbbr,
                        ["roomAbbr"] = roomAbbr,
                        ["sequence"] = sequence,
                        ["macAddress"] = registerData.MacAddress,
                        ["manufacturer"] = registerData.DeviceInfo.Manufacturer,
                        ["model"] = registerData.DeviceInfo.Model,
                        ["firmwareVersion"] = registerData.DeviceInfo.FirmwareVersion,
                        ["capabilities"] = registerData.Capabilities
                    }
                };

                OnDeviceConnected?.Invoke(this, tcpDevice);

                var existingDevices = await _deviceDataService.GetAllDevicesAsync();
                var exists = existingDevices.Any(d => d.Name == tcpDevice.DeviceName);

                if (!exists)
                {
                    var dbDeviceType = deviceType switch
                    {
                        "fan" => "fan",
                        "humidity-sensor" => "humidity-sensor",
                        "temp-sensor" => "temp-sensor",
                        "light" => "light",
                        "ac" => "ac",
                        "lock" => "lock",
                        "camera" => "camera",
                        "motor" => "motor",
                        _ => "unknown"
                    };

                    var icon = dbDeviceType switch
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

                    var addModel = new DeviceAddModel
                    {
                        Name = registerData.DeviceInfo.Name,
                        RoomId = deviceRoom,
                        TypeId = dbDeviceType,
                        Icon = icon,
                        Power = "0W",
                        IsOn = true,
                        Progress = 0
                    };

                    await _deviceDataService.AddDeviceAsync(addModel);
                    _logger.LogInformation($"通过TCP发现并添加新设备: {tcpDevice.DeviceName} (ID: {deviceId})");

                    var updatedDevices = await _deviceDataService.GetAllDevicesAsync();
                    await _hubContext.Clients.All.SendAsync("DevicesUpdated", updatedDevices);
                }
                else
                {
                    _logger.LogInformation($"设备 {tcpDevice.DeviceName} 已存在，无需重复添加");
                }

                var response = new TcpMessage
                {
                    MessageId = $"resp-{message.MessageId}",
                    Type = "register_response",
                    DeviceId = deviceId,
                    Data = new RegisterResponse
                    {
                        Success = true,
                        AssignedId = deviceId,
                        ServerTime = DateTime.UtcNow,
                        Config = new DeviceConfig
                        {
                            HeartbeatInterval = 30,
                            ReportInterval = 60
                        }
                    }
                };

                if (clientInfo.Client != null && clientInfo.Client.Connected)
                {
                    await SendToClientAsync(deviceId, response);
                    _logger.LogInformation($"已发送注册响应到设备 {deviceId}");
                }
                else
                {
                    _logger.LogInformation($"设备 {deviceId} 注册成功，等待后续连接");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理注册消息失败");
            }
        }

        private async Task HandleHeartbeatAsync(TcpMessage message)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var heartbeatData = JsonSerializer.Deserialize<HeartbeatData>(message.Data.ToString()!, options);

                if (heartbeatData != null)
                {
                    _logger.LogInformation($"收到心跳消息: 序列号={heartbeatData.Sequence}, 在线设备={heartbeatData.OnlineCount}/{heartbeatData.TotalCount}");

                    // 更新发送心跳的主设备的心跳时间
                    if (!string.IsNullOrEmpty(message.DeviceId))
                    {
                        lock (_connectedClients)
                        {
                            if (_connectedClients.ContainsKey(message.DeviceId))
                            {
                                _connectedClients[message.DeviceId].LastHeartbeat = DateTime.Now;
                                _connectedClients[message.DeviceId].LastSeen = DateTime.Now;
                            }
                        }
                    }

                    // 处理心跳中包含的所有设备状态
                    if (heartbeatData.DeviceStatus != null && heartbeatData.DeviceStatus.Any())
                    {
                        bool anyDeviceUpdated = false;
                        var allDevices = await _deviceDataService.GetAllDevicesAsync();

                        foreach (var deviceStatus in heartbeatData.DeviceStatus)
                        {
                            var deviceIdParts = deviceStatus.DeviceId?.Split('-');
                            if (deviceIdParts == null || deviceIdParts.Length != 3)
                            {
                                _logger.LogWarning($"无效的设备ID格式: {deviceStatus.DeviceId}");
                                continue;
                            }

                            var typeAbbr = deviceIdParts[0];
                            var roomAbbr = deviceIdParts[1];

                            string targetType = typeAbbr switch
                            {
                                "hum" => "humidity-sensor",
                                "temp" => "temp-sensor",
                                "light" => "light",
                                "ac" => "ac",
                                "lock" => "lock",
                                "cam" => "camera",
                                "fan" => "fan",
                                "motor" => "motor",
                                "motion" => "motion-sensor",
                                _ => "unknown"
                            };

                            string targetRoom = roomAbbr switch
                            {
                                "ent" => "entrance",
                                "liv" => "living",
                                "kit" => "kitchen",
                                "mbd" => "master-bedroom",
                                "sbd" => "second-bedroom",
                                "bat" => "bathroom",
                                "din" => "dining",
                                _ => "unknown"
                            };

                            var targetDevice = allDevices.FirstOrDefault(d =>
                                d.TypeIdentifier == targetType && d.RoomIdentifier == targetRoom);

                            if (targetDevice != null)
                            {
                                bool statusChanged = false;

                                if (targetDevice.TypeIdentifier == "temp-sensor")
                                {
                                    bool wasOnline = targetDevice.IsOn && targetDevice.StatusText != "离线";
                                    if (wasOnline != deviceStatus.IsOnline)
                                    {
                                        statusChanged = true;
                                        targetDevice.IsOn = deviceStatus.IsOnline;

                                        if (deviceStatus.IsOnline)
                                        {
                                            if (!string.IsNullOrEmpty(deviceStatus.CurrentValue) && deviceStatus.CurrentValue.Contains("°C"))
                                            {
                                                try
                                                {
                                                    double temp = double.Parse(deviceStatus.CurrentValue.Replace("°C", "").Trim());
                                                    targetDevice.Temperature = temp;
                                                    targetDevice.StatusText = $"温度 {temp:F1}°C";
                                                    _logger.LogInformation($"温度传感器 {targetDevice.Name} 在线，温度: {temp}°C");
                                                }
                                                catch
                                                {
                                                    targetDevice.StatusText = "在线";
                                                }
                                            }
                                            else
                                            {
                                                targetDevice.StatusText = "在线";
                                            }
                                        }
                                        else
                                        {
                                            targetDevice.StatusText = "离线";
                                            targetDevice.Temperature = null;
                                            _logger.LogInformation($"温度传感器 {targetDevice.Name} 离线");
                                        }

                                        await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, deviceStatus.IsOnline, targetDevice.StatusText);
                                        if (targetDevice.Temperature.HasValue)
                                        {
                                            await _deviceDataService.UpdateDeviceTemperatureAsync(targetDevice.Id, targetDevice.Temperature.Value);
                                        }
                                    }
                                    else if (deviceStatus.IsOnline && !string.IsNullOrEmpty(deviceStatus.CurrentValue) && deviceStatus.CurrentValue.Contains("°C"))
                                    {
                                        try
                                        {
                                            double temp = double.Parse(deviceStatus.CurrentValue.Replace("°C", "").Trim());
                                            if (targetDevice.Temperature != temp)
                                            {
                                                targetDevice.Temperature = temp;
                                                targetDevice.StatusText = $"温度 {temp:F1}°C";
                                                await _deviceDataService.UpdateDeviceTemperatureAsync(targetDevice.Id, temp);
                                                await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, true, targetDevice.StatusText);
                                                statusChanged = true;
                                                _logger.LogInformation($"温度传感器 {targetDevice.Name} 温度更新: {temp}°C");
                                            }
                                        }
                                        catch { }
                                    }
                                }
                                else
                                {
                                    bool wasOnline = targetDevice.IsOn && targetDevice.StatusText != "离线";
                                    if (wasOnline != deviceStatus.IsOnline)
                                    {
                                        statusChanged = true;
                                        targetDevice.IsOn = deviceStatus.IsOnline;

                                        if (deviceStatus.IsOnline)
                                        {
                                            if (!string.IsNullOrEmpty(deviceStatus.CurrentValue))
                                            {
                                                targetDevice.StatusText = deviceStatus.CurrentValue;
                                            }
                                            else
                                            {
                                                targetDevice.StatusText = targetDevice.TypeIdentifier switch
                                                {
                                                    "humidity-sensor" => "在线",
                                                    "light" => "开启",
                                                    "lock" => "已上锁",
                                                    "camera" => "在线",
                                                    "fan" => "运行中",
                                                    "motor" => "停止",
                                                    "ac" => "制冷 24°C",
                                                    "motion-sensor" => "在线",
                                                    _ => "在线"
                                                };
                                            }
                                            _logger.LogInformation($"设备 {targetDevice.Name} 通过心跳上线");
                                        }
                                        else
                                        {
                                            targetDevice.StatusText = "离线";
                                            _logger.LogInformation($"设备 {targetDevice.Name} 通过心跳离线");
                                        }

                                        await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, deviceStatus.IsOnline, targetDevice.StatusText);
                                    }
                                }

                                if (deviceStatus.BatteryLevel.HasValue && targetDevice.Humidity != deviceStatus.BatteryLevel.Value)
                                {
                                    await _deviceDataService.UpdateDeviceHumidityAsync(targetDevice.Id, deviceStatus.BatteryLevel.Value);
                                    _logger.LogInformation($"更新设备 {targetDevice.Name} 电量: {deviceStatus.BatteryLevel}%");
                                    statusChanged = true;
                                }

                                if (!string.IsNullOrEmpty(deviceStatus.CurrentValue) && deviceStatus.IsOnline)
                                {
                                    if (targetDevice.TypeIdentifier == "humidity-sensor" && deviceStatus.CurrentValue.Contains("%"))
                                    {
                                        try
                                        {
                                            int humidity = int.Parse(deviceStatus.CurrentValue.Replace("%", "").Trim());
                                            if (targetDevice.Humidity != humidity)
                                            {
                                                await _deviceDataService.UpdateDeviceHumidityAsync(targetDevice.Id, humidity);
                                                _logger.LogInformation($"更新湿度传感器 {targetDevice.Name}: {humidity}%");
                                                statusChanged = true;
                                            }
                                        }
                                        catch { }
                                    }
                                    else if (targetDevice.TypeIdentifier == "light" && (deviceStatus.CurrentValue == "开启" || deviceStatus.CurrentValue == "关闭"))
                                    {
                                        bool newIsOn = deviceStatus.CurrentValue == "开启";
                                        if (targetDevice.IsOn != newIsOn || targetDevice.StatusText != deviceStatus.CurrentValue)
                                        {
                                            targetDevice.IsOn = newIsOn;
                                            targetDevice.StatusText = deviceStatus.CurrentValue;
                                            await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, targetDevice.IsOn, targetDevice.StatusText);
                                            statusChanged = true;
                                        }
                                    }
                                    else if (targetDevice.TypeIdentifier == "lock" && (deviceStatus.CurrentValue == "已上锁" || deviceStatus.CurrentValue == "未上锁"))
                                    {
                                        bool newIsOn = deviceStatus.CurrentValue == "已上锁";
                                        if (targetDevice.IsOn != newIsOn || targetDevice.StatusText != deviceStatus.CurrentValue)
                                        {
                                            targetDevice.IsOn = newIsOn;
                                            targetDevice.StatusText = deviceStatus.CurrentValue;
                                            await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, targetDevice.IsOn, targetDevice.StatusText);
                                            statusChanged = true;
                                        }
                                    }
                                    else if (targetDevice.TypeIdentifier == "motor")
                                    {
                                        if (deviceStatus.CurrentValue == "停止" && targetDevice.IsOn)
                                        {
                                            targetDevice.IsOn = false;
                                            targetDevice.StatusText = "停止";
                                            targetDevice.Direction = "stop";
                                            await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, false, "停止");
                                            await _deviceDataService.UpdateDeviceDirectionAsync(targetDevice.Id, "stop");
                                            statusChanged = true;
                                        }
                                        else if (deviceStatus.CurrentValue == "正转" && (!targetDevice.IsOn || targetDevice.StatusText != "正转"))
                                        {
                                            targetDevice.IsOn = true;
                                            targetDevice.StatusText = "正转";
                                            targetDevice.Direction = "forward";
                                            await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, true, "正转");
                                            await _deviceDataService.UpdateDeviceDirectionAsync(targetDevice.Id, "forward");
                                            statusChanged = true;
                                        }
                                        else if (deviceStatus.CurrentValue == "反转" && (!targetDevice.IsOn || targetDevice.StatusText != "反转"))
                                        {
                                            targetDevice.IsOn = true;
                                            targetDevice.StatusText = "反转";
                                            targetDevice.Direction = "reverse";
                                            await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, true, "反转");
                                            await _deviceDataService.UpdateDeviceDirectionAsync(targetDevice.Id, "reverse");
                                            statusChanged = true;
                                        }
                                    }
                                    else if (targetDevice.TypeIdentifier == "fan" && deviceStatus.CurrentValue.StartsWith("风速"))
                                    {
                                        try
                                        {
                                            int speed = int.Parse(deviceStatus.CurrentValue.Replace("风速", "").Replace("档", "").Trim());
                                            if (targetDevice.MotorSpeed != speed)
                                            {
                                                await _deviceDataService.UpdateDeviceMotorSpeedAsync(targetDevice.Id, speed);
                                                _logger.LogInformation($"更新风扇 {targetDevice.Name} 转速: {speed}档");
                                                statusChanged = true;
                                            }
                                        }
                                        catch { }
                                    }
                                    else if (targetDevice.TypeIdentifier == "ac" && !deviceStatus.CurrentValue.Contains("°C") && deviceStatus.CurrentValue != "关闭")
                                    {
                                        string mode = deviceStatus.CurrentValue switch
                                        {
                                            "制冷" => "cool",
                                            "制热" => "heat",
                                            "送风" => "fan",
                                            "除湿" => "dry",
                                            "自动" => "auto",
                                            _ => targetDevice.Mode ?? "cool"
                                        };

                                        if (targetDevice.Mode != mode)
                                        {
                                            await _deviceDataService.UpdateDeviceModeAsync(targetDevice.Id, mode);
                                            _logger.LogInformation($"更新空调 {targetDevice.Name} 模式: {deviceStatus.CurrentValue}");
                                            statusChanged = true;
                                        }
                                    }
                                }

                                if (statusChanged)
                                {
                                    anyDeviceUpdated = true;
                                }
                            }
                        }

                        if (anyDeviceUpdated)
                        {
                            var updatedDevices = await _deviceDataService.GetAllDevicesAsync();
                            await _hubContext.Clients.All.SendAsync("DevicesUpdated", updatedDevices);
                        }
                    }

                    // 发送心跳响应
                    var response = new TcpMessage
                    {
                        MessageId = $"resp-{message.MessageId}",
                        Type = "heartbeat_response",
                        DeviceId = message.DeviceId,
                        Data = new HeartbeatResponse
                        {
                            Sequence = heartbeatData.Sequence,
                            ServerTime = DateTime.UtcNow
                        }
                    };

                    if (_connectedClients.ContainsKey(message.DeviceId) &&
                        _connectedClients[message.DeviceId].Client != null &&
                        _connectedClients[message.DeviceId].Client.Connected)
                    {
                        await SendToClientAsync(message.DeviceId, response);
                        _logger.LogDebug($"心跳响应已发送到设备 {message.DeviceId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理心跳消息失败");
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
                    _logger.LogInformation($"设备 {message.DeviceId} 状态: 在线={statusData.IsOnline}, 电量={statusData.BatteryLevel}%, IP={statusData.IpAddress}");

                    var allDevices = await _deviceDataService.GetAllDevicesAsync();
                    var deviceIdParts = message.DeviceId?.Split('-');

                    if (deviceIdParts != null && deviceIdParts.Length == 3)
                    {
                        var typeAbbr = deviceIdParts[0];
                        var roomAbbr = deviceIdParts[1];

                        string targetType = typeAbbr switch
                        {
                            "hum" => "humidity-sensor",
                            "temp" => "temp-sensor",
                            "light" => "light",
                            "ac" => "ac",
                            "lock" => "lock",
                            "cam" => "camera",
                            "fan" => "fan",
                            "motor" => "motor",
                            _ => "unknown"
                        };

                        string targetRoom = roomAbbr switch
                        {
                            "ent" => "entrance",
                            "liv" => "living",
                            "kit" => "kitchen",
                            "mbd" => "master-bedroom",
                            "sbd" => "second-bedroom",
                            "bat" => "bathroom",
                            "din" => "dining",
                            _ => "unknown"
                        };

                        var targetDevice = allDevices.FirstOrDefault(d =>
                            d.TypeIdentifier == targetType && d.RoomIdentifier == targetRoom);

                        if (targetDevice != null)
                        {
                            bool wasOnline = targetDevice.IsOn && targetDevice.StatusText != "离线";

                            if (statusData.IsOnline)
                            {
                                if (!wasOnline)
                                {
                                    targetDevice.IsOn = true;
                                    targetDevice.StatusText = "在线";

                                    if (targetDevice.TypeIdentifier == "temp-sensor" && targetDevice.Temperature.HasValue)
                                    {
                                        targetDevice.StatusText = $"温度 {targetDevice.Temperature.Value:F1}°C";
                                    }

                                    await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, true, targetDevice.StatusText);
                                    _logger.LogInformation($"设备 {targetDevice.Name} 已上线");
                                }
                            }
                            else
                            {
                                if (wasOnline || targetDevice.StatusText != "离线")
                                {
                                    targetDevice.IsOn = false;
                                    targetDevice.StatusText = "离线";
                                    await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, false, "离线");
                                    _logger.LogInformation($"设备 {targetDevice.Name} 已离线 (状态消息)");
                                }
                            }

                            if (statusData.BatteryLevel.HasValue)
                            {
                                await _deviceDataService.UpdateDeviceHumidityAsync(targetDevice.Id, statusData.BatteryLevel.Value);
                                _logger.LogInformation($"设备 {targetDevice.Name} 电量: {statusData.BatteryLevel}%");
                            }

                            if (targetDevice.TypeIdentifier == "temp-sensor" && statusData.CurrentValue != null)
                            {
                                try
                                {
                                    string currentValue = statusData.CurrentValue.ToString();
                                    if (currentValue.Contains("°C"))
                                    {
                                        double temp = double.Parse(currentValue.Replace("°C", "").Trim());
                                        await _deviceDataService.UpdateDeviceTemperatureAsync(targetDevice.Id, temp);
                                        _logger.LogInformation($"更新温度传感器: {targetDevice.Name} = {temp}°C");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "解析温度值失败");
                                }
                            }

                            var updatedDevices = await _deviceDataService.GetAllDevicesAsync();
                            await _hubContext.Clients.All.SendAsync("DevicesUpdated", updatedDevices);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理状态消息失败");
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

                _logger.LogInformation($"设备 {message.DeviceId} 遥测数据:");
                if (telemetryData?.TemperatureValue.HasValue == true)
                    _logger.LogInformation($"  - 温度: {telemetryData.TemperatureValue}°C");
                if (telemetryData?.HumidityValue.HasValue == true)
                    _logger.LogInformation($"  - 湿度: {telemetryData.HumidityValue}%");
                if (telemetryData?.BatteryLevel.HasValue == true)
                    _logger.LogInformation($"  - 电量: {telemetryData.BatteryLevel}%");
                if (telemetryData?.Power.HasValue == true)
                    _logger.LogInformation($"  - 功率: {telemetryData.Power}W");
                if (telemetryData?.IsOn.HasValue == true)
                    _logger.LogInformation($"  - 开关: {telemetryData.IsOn}");
                if (telemetryData?.Speed.HasValue == true)
                    _logger.LogInformation($"  - 风速: {telemetryData.Speed}档");
                if (!string.IsNullOrEmpty(telemetryData?.Mode))
                    _logger.LogInformation($"  - 模式: {telemetryData.Mode}");
                if (telemetryData?.Temperature.HasValue == true)
                    _logger.LogInformation($"  - 设定温度: {telemetryData.Temperature}°C");
                if (!string.IsNullOrEmpty(telemetryData?.Direction))
                    _logger.LogInformation($"  - 方向: {telemetryData.Direction}");

                OnTelemetryReceived?.Invoke(this, telemetryData!);

                var deviceIdParts = message.DeviceId?.Split('-');
                if (deviceIdParts != null && deviceIdParts.Length == 3)
                {
                    bool dataUpdated = false;

                    var typeAbbr = deviceIdParts[0];
                    var roomAbbr = deviceIdParts[1];

                    string targetType = typeAbbr switch
                    {
                        "hum" => "humidity-sensor",
                        "temp" => "temp-sensor",
                        "light" => "light",
                        "ac" => "ac",
                        "lock" => "lock",
                        "cam" => "camera",
                        "fan" => "fan",
                        "motor" => "motor",
                        _ => "unknown"
                    };

                    string targetRoom = roomAbbr switch
                    {
                        "ent" => "entrance",
                        "liv" => "living",
                        "kit" => "kitchen",
                        "mbd" => "master-bedroom",
                        "sbd" => "second-bedroom",
                        "bat" => "bathroom",
                        "din" => "dining",
                        _ => "unknown"
                    };

                    var allDevices = await _deviceDataService.GetAllDevicesAsync();

                    var targetDevice = allDevices.FirstOrDefault(d =>
                        d.TypeIdentifier == targetType && d.RoomIdentifier == targetRoom);

                    if (targetDevice != null)
                    {
                        _logger.LogInformation($"最终匹配的设备: {targetDevice.Name} (ID: {targetDevice.Id})");

                        switch (targetDevice.TypeIdentifier)
                        {
                            case "temp-sensor":
                                if (telemetryData?.TemperatureValue.HasValue == true)
                                {
                                    await _deviceDataService.UpdateDeviceTemperatureAsync(targetDevice.Id, telemetryData.TemperatureValue.Value);
                                    dataUpdated = true;
                                    _logger.LogInformation($"更新温度传感器: {targetDevice.Name} = {telemetryData.TemperatureValue}°C");
                                }
                                if (telemetryData?.BatteryLevel.HasValue == true)
                                {
                                    await _deviceDataService.UpdateDeviceHumidityAsync(targetDevice.Id, telemetryData.BatteryLevel.Value);
                                    dataUpdated = true;
                                }
                                break;

                            case "humidity-sensor":
                                if (telemetryData?.HumidityValue.HasValue == true)
                                {
                                    await _deviceDataService.UpdateDeviceHumidityAsync(targetDevice.Id, (int)Math.Round(telemetryData.HumidityValue.Value));
                                    dataUpdated = true;
                                    _logger.LogInformation($"更新湿度传感器: {targetDevice.Name} = {telemetryData.HumidityValue}%");
                                }
                                if (telemetryData?.BatteryLevel.HasValue == true)
                                {
                                    await _deviceDataService.UpdateDeviceHumidityAsync(targetDevice.Id, telemetryData.BatteryLevel.Value);
                                    dataUpdated = true;
                                }
                                break;

                            case "fan":
                                if (telemetryData?.Speed.HasValue == true)
                                {
                                    await _deviceDataService.UpdateDeviceMotorSpeedAsync(targetDevice.Id, telemetryData.Speed.Value);
                                    _logger.LogInformation($"更新风扇转速: {targetDevice.Name} = {telemetryData.Speed}档");
                                    dataUpdated = true;
                                }
                                if (telemetryData?.IsOn.HasValue == true)
                                {
                                    targetDevice.IsOn = telemetryData.IsOn.Value;
                                    targetDevice.StatusText = telemetryData.IsOn.Value ? $"风速 {telemetryData.Speed ?? 3}档" : "关闭";
                                    await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, telemetryData.IsOn.Value, targetDevice.StatusText);
                                    dataUpdated = true;
                                }
                                if (telemetryData?.Power.HasValue == true)
                                {
                                    await _deviceDataService.UpdateDevicePowerAsync(targetDevice.Id, telemetryData.Power.Value);
                                    dataUpdated = true;
                                }
                                break;

                            case "motor":
                                if (!string.IsNullOrEmpty(telemetryData?.Direction))
                                {
                                    await _deviceDataService.UpdateDeviceDirectionAsync(targetDevice.Id, telemetryData.Direction);
                                    _logger.LogInformation($"更新电机方向: {targetDevice.Name} = {telemetryData.Direction}");
                                    dataUpdated = true;
                                }
                                if (telemetryData?.Speed.HasValue == true)
                                {
                                    await _deviceDataService.UpdateDeviceMotorSpeedAsync(targetDevice.Id, telemetryData.Speed.Value);
                                    dataUpdated = true;
                                }
                                if (telemetryData?.Power.HasValue == true)
                                {
                                    await _deviceDataService.UpdateDevicePowerAsync(targetDevice.Id, telemetryData.Power.Value);
                                    dataUpdated = true;
                                }
                                break;

                            case "light":
                                if (telemetryData?.IsOn.HasValue == true)
                                {
                                    targetDevice.IsOn = telemetryData.IsOn.Value;
                                    targetDevice.StatusText = telemetryData.IsOn.Value ? "开启" : "关闭";
                                    await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, telemetryData.IsOn.Value, targetDevice.StatusText);
                                    _logger.LogInformation($"更新灯光状态: {targetDevice.Name} = {(telemetryData.IsOn.Value ? "开启" : "关闭")}");
                                    dataUpdated = true;
                                }
                                if (telemetryData?.Power.HasValue == true)
                                {
                                    await _deviceDataService.UpdateDevicePowerAsync(targetDevice.Id, telemetryData.Power.Value);
                                    dataUpdated = true;
                                }
                                break;

                            case "lock":
                                if (telemetryData?.IsOn.HasValue == true)
                                {
                                    targetDevice.IsOn = telemetryData.IsOn.Value;
                                    targetDevice.StatusText = telemetryData.IsOn.Value ? "已上锁" : "未上锁";
                                    await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, telemetryData.IsOn.Value, targetDevice.StatusText);
                                    _logger.LogInformation($"更新门锁状态: {targetDevice.Name} = {(telemetryData.IsOn.Value ? "已上锁" : "未上锁")}");
                                    dataUpdated = true;
                                }
                                if (telemetryData?.BatteryLevel.HasValue == true)
                                {
                                    await _deviceDataService.UpdateDeviceHumidityAsync(targetDevice.Id, telemetryData.BatteryLevel.Value);
                                    dataUpdated = true;
                                }
                                break;

                            case "camera":
                                if (telemetryData?.IsOn.HasValue == true)
                                {
                                    targetDevice.IsOn = telemetryData.IsOn.Value;
                                    targetDevice.StatusText = telemetryData.IsOn.Value ? "在线" : "离线";
                                    await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, telemetryData.IsOn.Value, targetDevice.StatusText);
                                    dataUpdated = true;
                                }
                                if (telemetryData?.Power.HasValue == true)
                                {
                                    await _deviceDataService.UpdateDevicePowerAsync(targetDevice.Id, telemetryData.Power.Value);
                                    dataUpdated = true;
                                }
                                break;

                            case "ac":
                                if (telemetryData?.Mode != null)
                                {
                                    await _deviceDataService.UpdateDeviceModeAsync(targetDevice.Id, telemetryData.Mode);
                                    dataUpdated = true;
                                }
                                if (telemetryData?.Temperature.HasValue == true)
                                {
                                    await _deviceDataService.UpdateDeviceAcTemperatureAsync(targetDevice.Id, telemetryData.Temperature.Value);
                                    dataUpdated = true;
                                }
                                if (telemetryData?.IsOn.HasValue == true)
                                {
                                    targetDevice.IsOn = telemetryData.IsOn.Value;
                                    if (telemetryData.IsOn.Value)
                                    {
                                        string modeText = telemetryData.Mode switch
                                        {
                                            "cool" => "制冷",
                                            "heat" => "制热",
                                            "fan" => "送风",
                                            "dry" => "除湿",
                                            "auto" => "自动",
                                            _ => telemetryData.Mode ?? "制冷"
                                        };
                                        targetDevice.StatusText = $"{modeText} {telemetryData.Temperature ?? 23}°C";
                                    }
                                    else
                                    {
                                        targetDevice.StatusText = "关闭";
                                    }
                                    await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, telemetryData.IsOn.Value, targetDevice.StatusText);
                                    dataUpdated = true;
                                }
                                if (telemetryData?.Power.HasValue == true)
                                {
                                    await _deviceDataService.UpdateDevicePowerAsync(targetDevice.Id, telemetryData.Power.Value);
                                    dataUpdated = true;
                                }
                                break;
                        }
                    }

                    if (dataUpdated)
                    {
                        var devices = await _deviceDataService.GetAllDevicesAsync();
                        await _hubContext.Clients.All.SendAsync("DevicesUpdated", devices);

                        if (telemetryData != null)
                        {
                            await _hubContext.Clients.Group(message.DeviceId).SendAsync(
                                "TelemetryUpdated",
                                message.DeviceId,
                                telemetryData
                            );

                            _logger.LogInformation($"已通过SignalR通知前端更新设备 {message.DeviceId} 的数据");
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"设备ID格式无效: {message.DeviceId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理遥测数据失败");
            }
        }

        private async Task HandleCommandResponseAsync(TcpMessage message)
        {
            try
            {
                var responseData = JsonSerializer.Deserialize<CommandResponseData>(message.Data.ToString()!);
                _logger.LogInformation($"设备 {message.DeviceId} 命令响应: 成功={responseData?.Success}, 命令ID={responseData?.CommandId}");
                if (responseData?.Error != null)
                {
                    _logger.LogWarning($"命令错误: {responseData.Error}");
                }
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
                var eventData = JsonSerializer.Deserialize<EventData>(message.Data.ToString()!);
                _logger.LogInformation($"设备 {message.DeviceId} 事件: {eventData?.EventType}, 级别={eventData?.Severity}");
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
                var disconnectData = JsonSerializer.Deserialize<DisconnectData>(message.Data.ToString()!);
                _logger.LogInformation($"设备 {message.DeviceId} 断开连接: {disconnectData?.Reason}, 代码={disconnectData?.Code}");

                await HandleClientDisconnectAsync(message.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理断开连接消息失败");
            }
        }

        private async Task HandleClientDisconnectAsync(string deviceId)
        {
            if (_connectedClients.ContainsKey(deviceId))
            {
                var clientInfo = _connectedClients[deviceId];

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

                lock (_connectedClients)
                {
                    _connectedClients.Remove(deviceId);
                }

                _logger.LogInformation($"设备 {deviceId} 已主动断开连接");

                var allDevices = await _deviceDataService.GetAllDevicesAsync();
                var deviceIdParts = deviceId?.Split('-');

                if (deviceIdParts != null && deviceIdParts.Length == 3)
                {
                    var typeAbbr = deviceIdParts[0];
                    var roomAbbr = deviceIdParts[1];

                    string targetType = typeAbbr switch
                    {
                        "hum" => "humidity-sensor",
                        "temp" => "temp-sensor",
                        "light" => "light",
                        "ac" => "ac",
                        "lock" => "lock",
                        "cam" => "camera",
                        "fan" => "fan",
                        "motor" => "motor",
                        _ => "unknown"
                    };

                    string targetRoom = roomAbbr switch
                    {
                        "ent" => "entrance",
                        "liv" => "living",
                        "kit" => "kitchen",
                        "mbd" => "master-bedroom",
                        "sbd" => "second-bedroom",
                        "bat" => "bathroom",
                        "din" => "dining",
                        _ => "unknown"
                    };

                    var targetDevice = allDevices.FirstOrDefault(d =>
                        d.TypeIdentifier == targetType && d.RoomIdentifier == targetRoom);

                    if (targetDevice != null)
                    {
                        targetDevice.IsOn = false;
                        targetDevice.StatusText = "离线";
                        await _deviceDataService.UpdateDeviceStatusAsync(targetDevice.Id, false, "离线");
                        _logger.LogInformation($"已更新设备 {targetDevice.Name} 状态为离线");
                    }
                }

                var updatedDevices = await _deviceDataService.GetAllDevicesAsync();
                await _hubContext.Clients.All.SendAsync("DevicesUpdated", updatedDevices);
            }
        }

        // ==================== 命令发送 ====================

        public async Task SendCommandAsync(string deviceId, string command, Dictionary<string, object>? parameters = null)
        {
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
                _logger.LogWarning($"设备 {deviceId} 没有活动的TCP连接，无法发送消息");
                return;
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

        // ==================== 设备查询 ====================

        public List<TcpDevice> GetAllDevices()
        {
            var devices = new List<TcpDevice>();

            lock (_connectedClients)
            {
                foreach (var c in _connectedClients.Values)
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

        // ==================== 内部类 ====================

        private class TcpClientInfo
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