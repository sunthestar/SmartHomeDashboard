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
        public string RoomId { get; set; } = "";  // 房间标识符：living, master-bedroom 等

        [Required]
        [StringLength(50)]
        public string RoomName { get; set; } = "";  // 房间显示名称：客厅、主卧等

        [StringLength(200)]
        public string? Description { get; set; }

        public int DeviceCount { get; set; }

        public int OnlineCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // 导航属性
        public ICollection<DeviceModel> Devices { get; set; } = new List<DeviceModel>();
    }

    // 设备类型模型
    public class DeviceTypeModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string TypeId { get; set; } = "";  // 类型标识符：light, ac, lock 等

        [Required]
        [StringLength(50)]
        public string TypeName { get; set; } = "";  // 类型显示名称：灯光、空调、门锁等

        [StringLength(50)]
        public string Icon { get; set; } = "";  // 默认图标

        [StringLength(200)]
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 导航属性
        public ICollection<DeviceModel> Devices { get; set; } = new List<DeviceModel>();
    }

    // 设备模型
    public class DeviceModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string DeviceNumber { get; set; } = "";  // 例如：001, 002

        [Required]
        [StringLength(100)]
        public string FullDeviceId { get; set; } = "";  // 例如：living-light-001

        // 外键：所属房间
        public int RoomId { get; set; }
        public RoomModel? Room { get; set; }

        // 外键：设备类型
        public int DeviceTypeId { get; set; }
        public DeviceTypeModel? DeviceType { get; set; }

        // 冗余字段（方便查询）
        [StringLength(50)]
        public string RoomIdentifier { get; set; } = "";  // 房间标识符副本

        [StringLength(50)]
        public string TypeIdentifier { get; set; } = "";  // 类型标识符副本

        [StringLength(50)]
        public string Icon { get; set; } = "";

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

        // 传感器属性
        [Column(TypeName = "decimal(5,2)")]
        public double? Temperature { get; set; }

        public int? Humidity { get; set; }

        // 电机/风扇属性
        public int? MotorSpeed { get; set; }

        // 空调属性
        [StringLength(20)]
        public string? Mode { get; set; }

        public bool? SwingVertical { get; set; }
        public bool? SwingHorizontal { get; set; }
        public bool? Light { get; set; }
        public bool? Quiet { get; set; }

        // 电机方向
        [StringLength(20)]
        public string? Direction { get; set; }

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

        // 外键：关联的设备
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
        public string LogType { get; set; } = "";  // device, system, alert, automation

        [StringLength(20)]
        public string LogLevel { get; set; } = "info";  // info, warning, error

        [StringLength(200)]
        public string Title { get; set; } = "";

        [StringLength(500)]
        public string Content { get; set; } = "";

        // 外键：关联的设备（可选）
        public int? DeviceId { get; set; }
        public DeviceModel? Device { get; set; }

        [StringLength(100)]
        public string DeviceName { get; set; } = "";

        [StringLength(50)]
        public string ActionType { get; set; } = "";  // add, delete, update, control

        [StringLength(500)]
        public string ActionDetail { get; set; } = "";

        public bool IsRead { get; set; } = false;
    }

    // 自动化场景模型
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
        public string TriggerType { get; set; } = "manual";  // manual, time, device

        [Column(TypeName = "text")]
        public string TriggerCondition { get; set; } = "{}";  // JSON格式

        [Column(TypeName = "text")]
        public string Actions { get; set; } = "[]";  // JSON格式

        [StringLength(50)]
        public string ExecuteTime { get; set; } = "";

        [StringLength(50)]
        public string RepeatDays { get; set; } = "";  // mon,tue,wed...

        public bool IsActive { get; set; } = false;

        public int ExecuteCount { get; set; } = 0;

        public DateTime? LastExecuteTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }

    // 登录设置模型（单用户模式）
    public class LoginSettingsModel
    {
        [Key]
        public int Id { get; set; } = 1;  // 只有一条记录，ID固定为1

        [Required]
        [StringLength(200)]
        public string Password { get; set; } = "";  // 加密后的密码

        [StringLength(50)]
        public string PasswordSalt { get; set; } = "";  // 密码盐值

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
        public double? Temperature { get; set; }
        public int? Humidity { get; set; }
        public int? MotorSpeed { get; set; }
        public string? Mode { get; set; }
        public string? Direction { get; set; }
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