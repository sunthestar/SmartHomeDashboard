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

        // 导航属性
        public ICollection<DeviceModel> Devices { get; set; } = new List<DeviceModel>();
    }

    // 设备模型（增强版）
    public class DeviceModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        // 设备编号（房间内唯一）
        [Required]
        [StringLength(20)]
        public string DeviceNumber { get; set; } = "";  // 例如：001, 002

        // 完整设备ID：房间ID-类型ID-编号
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
    }

    // 设备添加模型
    public class DeviceAddModel
    {
        public string Name { get; set; } = "";
        public string RoomId { get; set; } = "";  // 房间标识符
        public string TypeId { get; set; } = "";   // 类型标识符
        public string DeviceNumber { get; set; } = "";  // 设备编号（可选，不提供则自动生成）
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

    // 房间视图模型（用于前端显示）
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