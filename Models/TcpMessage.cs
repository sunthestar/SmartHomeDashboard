using System.Text.Json.Serialization;

namespace SmartHomeDashboard.Models
{
    public class TcpMessage
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string DeviceId { get; set; } = "";
        public object Data { get; set; } = new();
    }

    public class RegisterMessage
    {
        public string MacAddress { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public DeviceInfo DeviceInfo { get; set; } = new();
        public List<string> Capabilities { get; set; } = new();
        public AuthInfo Auth { get; set; } = new();
    }

    public class DeviceInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("room")]
        public string Room { get; set; } = "";

        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; } = "";

        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("firmwareVersion")]
        public string FirmwareVersion { get; set; } = "";

        [JsonPropertyName("hardwareVersion")]
        public string HardwareVersion { get; set; } = "";
    }

    public class AuthInfo
    {
        public string Key { get; set; } = "";
    }

    public class RegisterResponse
    {
        public bool Success { get; set; }
        public string AssignedId { get; set; } = "";
        public DateTime ServerTime { get; set; }
        public DeviceConfig Config { get; set; } = new();
    }

    public class DeviceConfig
    {
        public int HeartbeatInterval { get; set; } = 30;
        public int ReportInterval { get; set; } = 60;
    }

    public class HeartbeatData
    {
        public int Sequence { get; set; }
        public List<DeviceStatusInfo> DeviceStatus { get; set; } = new();
        public int OnlineCount { get; set; }
        public int TotalCount { get; set; }
    }

    public class DeviceStatusInfo
    {
        public string DeviceId { get; set; } = "";
        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }
        public int? BatteryLevel { get; set; }
        public string? CurrentValue { get; set; }
    }

    public class HeartbeatResponse
    {
        public int Sequence { get; set; }
        public DateTime ServerTime { get; set; }
    }

    public class StatusData
    {
        public bool IsOnline { get; set; }
        public string ConnectionStatus { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public int? SignalStrength { get; set; }
        public int? BatteryLevel { get; set; }
        public long? Uptime { get; set; }
        public string? Room { get; set; }
        public object? CurrentValue { get; set; }
    }

    /// <summary>
    /// 基础设备状态 - 所有设备共享
    /// </summary>
    public class BaseDeviceState
    {
        public bool IsOnline { get; set; }
        public double? Power { get; set; }  // 功率(W)
    }

    /// <summary>
    /// 开关设备状态 - 灯泡、门锁、摄像头等
    /// </summary>
    public class SwitchDeviceState : BaseDeviceState
    {
        public bool IsOn { get; set; }  // 开启/关闭
    }

    /// <summary>
    /// 风扇设备状态
    /// </summary>
    public class FanDeviceState : BaseDeviceState
    {
        public bool IsOn { get; set; }
        public int Speed { get; set; }  // 1-5档
    }

    /// <summary>
    /// 空调设备状态
    /// </summary>
    public class AirConditionerState : BaseDeviceState
    {
        public bool IsOn { get; set; }
        public string Mode { get; set; } = "cool";  // cool, heat, dry
        public double Temperature { get; set; } = 24.0;  // 温度
        public bool SwingVertical { get; set; }  // 上下扫风
        public bool SwingHorizontal { get; set; }  // 左右扫风
        public bool Light { get; set; }  // 灯光
        public bool Quiet { get; set; }  // 静音
    }

    /// <summary>
    /// 温度传感器状态
    /// </summary>
    public class TemperatureSensorState : BaseDeviceState
    {
        public double Temperature { get; set; }  // 温度值
    }

    /// <summary>
    /// 湿度传感器状态
    /// </summary>
    public class HumiditySensorState : BaseDeviceState
    {
        public double Humidity { get; set; }  // 湿度值
    }

    /// <summary>
    /// 电机设备状态
    /// </summary>
    public class MotorState : BaseDeviceState
    {
        public string Direction { get; set; } = "stop";  // forward, reverse, stop
        public bool IsRunning => Direction != "stop";
    }

    /// <summary>
    /// 统一遥测数据 - 根据设备类型包含不同字段
    /// </summary>
    public class TelemetryData
    {
        // 公共字段
        public string DeviceId { get; set; } = "";
        public string DeviceType { get; set; } = "";
        public DateTime Timestamp { get; set; }

        // 基础设备属性
        public bool? IsOnline { get; set; }
        public double? Power { get; set; }
        public int? BatteryLevel { get; set; }

        // 开关设备属性
        public bool? IsOn { get; set; }

        // 风扇属性
        public int? Speed { get; set; }

        // 空调属性
        public string? Mode { get; set; }
        public double? Temperature { get; set; }
        public bool? SwingVertical { get; set; }
        public bool? SwingHorizontal { get; set; }
        public bool? Light { get; set; }
        public bool? Quiet { get; set; }

        // 传感器属性
        public double? TemperatureValue { get; set; }  // 温度传感器
        public double? HumidityValue { get; set; }     // 湿度传感器

        // 电机属性
        public string? Direction { get; set; }

        // 辅助方法：根据设备类型设置对应的属性
        public void SetValueForDeviceType(string deviceType, object value)
        {
            switch (deviceType.ToLower())
            {
                case "light":
                case "lock":
                case "camera":
                    if (value is bool boolValue)
                        IsOn = boolValue;
                    break;

                case "fan":
                    if (value is int intValue)
                        Speed = intValue;
                    else if (value is bool boolVal)
                        IsOn = boolVal;
                    break;

                case "ac":
                    if (value is string strValue)
                        Mode = strValue;
                    else if (value is double dblValue)
                        Temperature = dblValue;
                    else if (value is bool blValue)
                    {
                        // 需要根据上下文判断是哪个布尔属性
                    }
                    break;

                case "temp-sensor":
                    if (value is double tempValue)
                        TemperatureValue = tempValue;
                    break;

                case "humidity-sensor":
                    if (value is double humValue)
                        HumidityValue = humValue;
                    break;

                case "motor":
                    if (value is string dirValue)
                        Direction = dirValue;
                    break;
            }
        }
    }

    public class CommandData
    {
        public string CommandId { get; set; } = "";
        public string Command { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string Source { get; set; } = "";
    }

    public class CommandResponseData
    {
        public string CommandId { get; set; } = "";
        public bool Success { get; set; }
        public object? Result { get; set; }
        public string? Error { get; set; }
        public int? ExecutionTime { get; set; }
    }

    public class EventData
    {
        public string EventId { get; set; } = "";
        public string EventType { get; set; } = "";
        public string Severity { get; set; } = "";
        public object Data { get; set; } = new();
    }

    public class DisconnectData
    {
        public string Reason { get; set; } = "";
        public int Code { get; set; }
    }

    public class TcpDevice
    {
        public string DeviceId { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string DeviceType { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public int Port { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsOnline { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();

        // 根据设备类型动态获取状态
        public object? GetState(TelemetryData telemetry)
        {
            return DeviceType switch
            {
                "light" or "lock" or "camera" => new { IsOn = telemetry.IsOn, Power = telemetry.Power },
                "fan" => new { IsOn = telemetry.IsOn, Speed = telemetry.Speed, Power = telemetry.Power },
                "ac" => new
                {
                    IsOn = telemetry.IsOn,
                    Mode = telemetry.Mode,
                    Temperature = telemetry.Temperature,
                    SwingVertical = telemetry.SwingVertical,
                    SwingHorizontal = telemetry.SwingHorizontal,
                    Light = telemetry.Light,
                    Quiet = telemetry.Quiet,
                    Power = telemetry.Power
                },
                "temp-sensor" => new { Temperature = telemetry.TemperatureValue, BatteryLevel = telemetry.BatteryLevel },
                "humidity-sensor" => new { Humidity = telemetry.HumidityValue, BatteryLevel = telemetry.BatteryLevel },
                "motor" => new { Direction = telemetry.Direction, Power = telemetry.Power },
                _ => telemetry
            };
        }
    }
}