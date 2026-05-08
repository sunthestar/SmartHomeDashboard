using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHomeDashboard.Models
{
    // 房间模型
    public class RoomModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string RoomId { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string RoomName { get; set; } = "";

        [StringLength(200)]
        public string? Description { get; set; }

        public int DeviceCount { get; set; }

        public int OnlineCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<DeviceModel> Devices { get; set; } = new List<DeviceModel>();
    }

    // 设备类型模型
    public class DeviceTypeModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string TypeId { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string TypeName { get; set; } = "";

        [StringLength(50)]
        public string Icon { get; set; } = "";

        [StringLength(200)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<DeviceModel> Devices { get; set; } = new List<DeviceModel>();
    }

    // 设备模型 - 扩展版
    public class DeviceModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string DeviceNumber { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string FullDeviceId { get; set; } = "";

        // 外键：所属房间
        public int RoomId { get; set; }
        public RoomModel? Room { get; set; }

        // 外键：设备类型
        public int DeviceTypeId { get; set; }
        public DeviceTypeModel? DeviceType { get; set; }

        // 冗余字段（方便查询）
        [StringLength(50)]
        public string RoomIdentifier { get; set; } = "";

        [StringLength(50)]
        public string TypeIdentifier { get; set; } = "";

        [StringLength(50)]
        public string Icon { get; set; } = "";

        // ========== 通用状态属性 ==========
        public bool IsOn { get; set; }

        [StringLength(100)]
        public string StatusText { get; set; } = "";

        [StringLength(200)]
        public string Detail { get; set; } = "";

        [StringLength(20)]
        public string Power { get; set; } = "";

        public double PowerValue { get; set; }

        public int Progress { get; set; }

        [StringLength(20)]
        public string? ProgressColor { get; set; }

        // ========== 兼容旧字段（保留，逐步迁移）==========
        [Column(TypeName = "decimal(5,2)")]
        public double? Temperature { get; set; }  // 【已废弃】温度传感器用 TemperatureValue，空调用 AcTemperature

        public int? Humidity { get; set; }  // 【已废弃】湿度传感器用 HumidityValue，电池设备用 BatteryLevel

        // ========== 新增：语义化字段 ==========

        // ----- 通用传感器 -----
        /// <summary>温度传感器：当前温度值</summary>
        [Column(TypeName = "decimal(5,2)")]
        public double? TemperatureValue { get; set; }

        /// <summary>湿度传感器：当前湿度值</summary>
        [Column(TypeName = "decimal(5,2)")]
        public double? HumidityValue { get; set; }

        /// <summary>电池电量百分比 (0-100)</summary>
        public int? BatteryLevel { get; set; }

        // ----- 灯光设备 -----
        /// <summary>亮度 0-100</summary>
        public int? Brightness { get; set; }

        /// <summary>色温 2700-6500K</summary>
        public int? ColorTemperature { get; set; }

        // ----- 空调设备 -----
        /// <summary>空调设定温度</summary>
        public int? AcTemperature { get; set; }

        /// <summary>空调模式: cool, heat, fan, dry, auto</summary>
        [StringLength(20)]
        public string? AcMode { get; set; }

        /// <summary>空调风速: low, medium, high, auto</summary>
        [StringLength(20)]
        public string? AcFanSpeed { get; set; }

        /// <summary>上下扫风</summary>
        public bool? AcSwingVertical { get; set; }

        /// <summary>左右扫风</summary>
        public bool? AcSwingHorizontal { get; set; }

        /// <summary>空调灯光</summary>
        public bool? AcLight { get; set; }

        /// <summary>静音模式</summary>
        public bool? AcQuiet { get; set; }

        // ----- 风扇设备 -----
        /// <summary>风扇转速 1-5</summary>
        public int? FanSpeed { get; set; }

        /// <summary>摆头</summary>
        public bool? FanSwing { get; set; }

        // ----- 电机设备 -----
        /// <summary>电机转速 RPM</summary>
        public int? MotorSpeed { get; set; }

        /// <summary>电机方向: forward, reverse, stop</summary>
        [StringLength(20)]
        public string? MotorDirection { get; set; }

        // ----- 门锁设备 -----
        /// <summary>最后解锁时间</summary>
        public DateTime? LastUnlockTime { get; set; }

        /// <summary>解锁方式: fingerprint, password, card, app, remote_code</summary>
        [StringLength(50)]
        public string? UnlockMethod { get; set; }

        // ----- 摄像头设备 -----
        /// <summary>是否录制中</summary>
        public bool? IsRecording { get; set; }

        /// <summary>移动侦测</summary>
        public bool? MotionDetected { get; set; }

        /// <summary>夜视模式: auto, on, off</summary>
        [StringLength(20)]
        public string? NightMode { get; set; }

        // ========== 向后兼容的辅助属性 ==========
        [NotMapped]
        public string Mode
        {
            get => AcMode ?? "";
            set => AcMode = value;
        }

        [NotMapped]
        public bool? SwingVertical
        {
            get => AcSwingVertical;
            set => AcSwingVertical = value;
        }

        [NotMapped]
        public bool? SwingHorizontal
        {
            get => AcSwingHorizontal;
            set => AcSwingHorizontal = value;
        }

        [NotMapped]
        public bool? Light
        {
            get => AcLight;
            set => AcLight = value;
        }

        [NotMapped]
        public bool? Quiet
        {
            get => AcQuiet;
            set => AcQuiet = value;
        }

        [NotMapped]
        public string? Direction
        {
            get => MotorDirection;
            set => MotorDirection = value;
        }

        // 时间戳
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // 导航属性
        public TcpConnectionModel? TcpConnection { get; set; }
        public ICollection<SystemLogModel> SystemLogs { get; set; } = new List<SystemLogModel>();
    }

    // TCP设备连接模型
    public class TcpConnectionModel
    {
        [Key]
        public int Id { get; set; }

        public int DeviceId { get; set; }
        public DeviceModel? Device { get; set; }

        [StringLength(100)]
        public string FullDeviceId { get; set; } = "";

        [StringLength(100)]
        public string DeviceName { get; set; } = "";

        [StringLength(50)]
        public string DeviceType { get; set; } = "";

        [StringLength(50)]
        public string IpAddress { get; set; } = "";

        public int Port { get; set; }

        public DateTime ConnectedTime { get; set; }

        public DateTime LastHeartbeat { get; set; }

        public DateTime LastSeen { get; set; }

        public bool IsOnline { get; set; }

        public int TimeoutCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }

    // 系统日志模型
    public class SystemLogModel
    {
        [Key]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string LogType { get; set; } = "";

        [StringLength(20)]
        public string LogLevel { get; set; } = "info";

        [StringLength(200)]
        public string Title { get; set; } = "";

        [StringLength(500)]
        public string Content { get; set; } = "";

        public int? DeviceId { get; set; }
        public DeviceModel? Device { get; set; }

        [StringLength(100)]
        public string DeviceName { get; set; } = "";

        [StringLength(50)]
        public string ActionType { get; set; } = "";

        [StringLength(500)]
        public string ActionDetail { get; set; } = "";

        public bool IsRead { get; set; } = false;
    }

    // 自动化场景模型
    // 自动化场景模型 - 在原有的 SceneModel 中添加以下字段
    public class SceneModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string SceneName { get; set; } = "";

        [StringLength(50)]
        public string Icon { get; set; } = "fa-clock";

        [StringLength(200)]
        public string Description { get; set; } = "";

        [StringLength(50)]
        public string TriggerType { get; set; } = "manual"; // manual, time, condition

        [Column(TypeName = "text")]
        public string TriggerCondition { get; set; } = "{}";

        [Column(TypeName = "text")]
        public string Actions { get; set; } = "[]";

        // 定时触发相关
        [StringLength(50)]
        public string ExecuteTime { get; set; } = "";

        [StringLength(50)]
        public string RepeatDays { get; set; } = "";

        // 条件触发相关（新增）
        [Column(TypeName = "text")]
        public string Conditions { get; set; } = "[]"; // JSON格式存储条件列表

        [StringLength(20)]
        public string ConditionLogic { get; set; } = "and"; // and / or

        // 联动触发相关
        public int? TriggerSceneId { get; set; }
        public string? TriggerSceneAction { get; set; }

        // 联动目标场景列表（JSON格式）
        [Column(TypeName = "text")]
        public string LinkedScenes { get; set; } = "[]";

        public bool IsActive { get; set; } = false;

        public int ExecuteCount { get; set; } = 0;

        public DateTime? LastExecuteTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }

    // 登录设置模型
    public class LoginSettingsModel
    {
        [Key]
        public int Id { get; set; } = 1;

        [Required]
        [StringLength(200)]
        public string Password { get; set; } = "";

        [StringLength(50)]
        public string PasswordSalt { get; set; } = "";

        public bool IsEnabled { get; set; } = true;

        public DateTime? LastLoginTime { get; set; }

        [StringLength(50)]
        public string LastLoginIp { get; set; } = "";

        public int LoginCount { get; set; } = 0;

        public int FailCount { get; set; } = 0;

        public DateTime? LastFailTime { get; set; }

        public DateTime? LockUntil { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }

    // 设备添加模型
    public class DeviceAddModel
    {
        public string Name { get; set; } = "";
        public string RoomId { get; set; } = "";
        public string TypeId { get; set; } = "";
        public string DeviceNumber { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Power { get; set; } = "";
        public bool IsOn { get; set; }
        public int Progress { get; set; }

        // 兼容旧字段
        public double? Temperature { get; set; }
        public int? Humidity { get; set; }
        public int? MotorSpeed { get; set; }
        public string? Mode { get; set; }
        public string? Direction { get; set; }

        // 新字段
        public double? TemperatureValue { get; set; }
        public double? HumidityValue { get; set; }
        public int? BatteryLevel { get; set; }
        public int? Brightness { get; set; }
        public int? ColorTemperature { get; set; }
        public int? AcTemperature { get; set; }
        public string? AcMode { get; set; }
        public string? AcFanSpeed { get; set; }
        public int? FanSpeed { get; set; }
        public string? MotorDirection { get; set; }
    }

    // 房间视图模型
    public class RoomViewModel
    {
        public string RoomId { get; set; } = "";
        public string RoomName { get; set; } = "";
        public int DeviceCount { get; set; }
        public int OnlineCount { get; set; }
        public Dictionary<string, List<DeviceModel>> DevicesByType { get; set; } = new();
    }

    // 设备类型统计
    public class DeviceTypeStat
    {
        public string TypeId { get; set; } = "";
        public string TypeName { get; set; } = "";
        public string Icon { get; set; } = "";
        public int Count { get; set; }
        public int OnlineCount { get; set; }
    }
}