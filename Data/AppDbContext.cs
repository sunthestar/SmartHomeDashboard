using Microsoft.EntityFrameworkCore;
using SmartHomeDashboard.Models;

namespace SmartHomeDashboard.Data
{
    public class AppDbContext : DbContext
    {
        public string DbPath { get; private set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            var folder = Environment.CurrentDirectory;
            DbPath = Path.Join(folder, "App_Data", "smarthome.db");
        }

        public DbSet<RoomModel> Rooms { get; set; }
        public DbSet<DeviceTypeModel> DeviceTypes { get; set; }
        public DbSet<DeviceModel> Devices { get; set; }
        public DbSet<TcpConnectionModel> TcpConnections { get; set; }
        public DbSet<SystemLogModel> SystemLogs { get; set; }
        public DbSet<SceneModel> Scenes { get; set; }
        public DbSet<LoginSettingsModel> LoginSettings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var directory = Path.GetDirectoryName(DbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!options.IsConfigured)
            {
                options.UseSqlite($"Data Source={DbPath}");
                options.EnableSensitiveDataLogging(); // 开发环境使用
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置房间表
            modelBuilder.Entity<RoomModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RoomId).IsUnique();
                entity.Property(e => e.RoomId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RoomName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // 配置设备类型表
            modelBuilder.Entity<DeviceTypeModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.TypeId).IsUnique();
                entity.Property(e => e.TypeId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.TypeName).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Icon).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // 配置设备表
            modelBuilder.Entity<DeviceModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.FullDeviceId).IsUnique();
                entity.HasIndex(e => new { e.RoomId, e.DeviceTypeId, e.DeviceNumber }).IsUnique();

                entity.Property(e => e.FullDeviceId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DeviceNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.RoomIdentifier).HasMaxLength(50);
                entity.Property(e => e.TypeIdentifier).HasMaxLength(50);
                entity.Property(e => e.Icon).HasMaxLength(50);
                entity.Property(e => e.StatusText).HasMaxLength(100);
                entity.Property(e => e.Detail).HasMaxLength(200);
                entity.Property(e => e.Mode).HasMaxLength(20);
                entity.Property(e => e.Direction).HasMaxLength(20);
                entity.Property(e => e.ProgressColor).HasMaxLength(20);
                entity.Property(e => e.Temperature).HasColumnType("decimal(5,2)");
                entity.Property(e => e.CreatedAt).IsRequired();

                // 外键关系
                entity.HasOne(e => e.Room)
                    .WithMany(r => r.Devices)
                    .HasForeignKey(e => e.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.DeviceType)
                    .WithMany(t => t.Devices)
                    .HasForeignKey(e => e.DeviceTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 配置TCP连接表
            modelBuilder.Entity<TcpConnectionModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.FullDeviceId).IsUnique();
                entity.HasIndex(e => e.DeviceId);

                entity.Property(e => e.FullDeviceId).HasMaxLength(100);
                entity.Property(e => e.DeviceName).HasMaxLength(100);
                entity.Property(e => e.DeviceType).HasMaxLength(50);
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();

                // 外键关系
                entity.HasOne(e => e.Device)
                    .WithOne(d => d.TcpConnection)
                    .HasForeignKey<TcpConnectionModel>(e => e.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 配置系统日志表
            modelBuilder.Entity<SystemLogModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.LogType);
                entity.HasIndex(e => e.DeviceId);

                entity.Property(e => e.LogType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.LogLevel).HasMaxLength(20);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Content).HasMaxLength(500);
                entity.Property(e => e.DeviceName).HasMaxLength(100);
                entity.Property(e => e.ActionType).HasMaxLength(50);
                entity.Property(e => e.ActionDetail).HasMaxLength(500);
                entity.Property(e => e.Timestamp).IsRequired();

                // 外键关系
                entity.HasOne(e => e.Device)
                    .WithMany(d => d.SystemLogs)
                    .HasForeignKey(e => e.DeviceId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // 配置自动化场景表
            modelBuilder.Entity<SceneModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SceneName);
                entity.HasIndex(e => e.TriggerType);
                entity.HasIndex(e => e.IsActive);

                entity.Property(e => e.SceneName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Icon).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.TriggerType).HasMaxLength(50);
                entity.Property(e => e.TriggerCondition).HasColumnType("text");
                entity.Property(e => e.Actions).HasColumnType("text");
                entity.Property(e => e.ExecuteTime).HasMaxLength(50);
                entity.Property(e => e.RepeatDays).HasMaxLength(50);
                entity.Property(e => e.LinkedScenes).HasColumnType("text");  // 新增
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // 配置登录设置表（单用户模式）
            modelBuilder.Entity<LoginSettingsModel>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Id).IsUnique();

                entity.Property(e => e.Password).IsRequired().HasMaxLength(200);
                entity.Property(e => e.PasswordSalt).HasMaxLength(50);
                entity.Property(e => e.LastLoginIp).HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();

                // 确保只有一条记录，ID固定为1
                entity.HasData(new LoginSettingsModel
                {
                    Id = 1,
                    Password = "123456", // 实际应用中需要加密
                    PasswordSalt = "",
                    IsEnabled = true,
                    LoginCount = 0,
                    FailCount = 0,
                    CreatedAt = DateTime.Now
                });
            });

            // 初始化房间数据
            SeedRooms(modelBuilder);

            // 初始化设备类型数据
            SeedDeviceTypes(modelBuilder);

            // 初始化设备数据
            SeedDevices(modelBuilder);

            // 初始化场景数据
            SeedScenes(modelBuilder);
        }

        private void SeedRooms(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RoomModel>().HasData(
                new RoomModel { Id = 1, RoomId = "living", RoomName = "客厅", Description = "主要活动区域", DeviceCount = 2, OnlineCount = 0, CreatedAt = DateTime.Now },
                new RoomModel { Id = 2, RoomId = "master-bedroom", RoomName = "主卧", Description = "主人卧室", DeviceCount = 1, OnlineCount = 0, CreatedAt = DateTime.Now },
                new RoomModel { Id = 3, RoomId = "second-bedroom", RoomName = "次卧", Description = "次卧/客房", DeviceCount = 0, OnlineCount = 0, CreatedAt = DateTime.Now },
                new RoomModel { Id = 4, RoomId = "kitchen", RoomName = "厨房", Description = "烹饪区域", DeviceCount = 1, OnlineCount = 0, CreatedAt = DateTime.Now },
                new RoomModel { Id = 5, RoomId = "bathroom", RoomName = "浴室", Description = "洗浴区域", DeviceCount = 1, OnlineCount = 0, CreatedAt = DateTime.Now },
                new RoomModel { Id = 6, RoomId = "dining", RoomName = "餐厅", Description = "用餐区域", DeviceCount = 0, OnlineCount = 0, CreatedAt = DateTime.Now },
                new RoomModel { Id = 7, RoomId = "entrance", RoomName = "入口", Description = "玄关/入口", DeviceCount = 1, OnlineCount = 0, CreatedAt = DateTime.Now }
            );
        }

        private void SeedDeviceTypes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DeviceTypeModel>().HasData(
                new DeviceTypeModel { Id = 1, TypeId = "ac", TypeName = "空调", Icon = "fa-wind", Description = "智能空调", CreatedAt = DateTime.Now },
                new DeviceTypeModel { Id = 2, TypeId = "light", TypeName = "灯光", Icon = "fa-lightbulb", Description = "智能灯泡", CreatedAt = DateTime.Now },
                new DeviceTypeModel { Id = 3, TypeId = "lock", TypeName = "门锁", Icon = "fa-lock", Description = "智能门锁", CreatedAt = DateTime.Now },
                new DeviceTypeModel { Id = 4, TypeId = "camera", TypeName = "摄像头", Icon = "fa-camera", Description = "网络摄像头", CreatedAt = DateTime.Now },
                new DeviceTypeModel { Id = 5, TypeId = "fan", TypeName = "风扇", Icon = "fa-fan", Description = "智能风扇", CreatedAt = DateTime.Now },
                new DeviceTypeModel { Id = 6, TypeId = "temp-sensor", TypeName = "温度传感器", Icon = "fa-thermometer-half", Description = "温度传感器", CreatedAt = DateTime.Now },
                new DeviceTypeModel { Id = 7, TypeId = "humidity-sensor", TypeName = "湿度传感器", Icon = "fa-tint", Description = "湿度传感器", CreatedAt = DateTime.Now },
                new DeviceTypeModel { Id = 8, TypeId = "motor", TypeName = "电机", Icon = "fa-cogs", Description = "电机设备", CreatedAt = DateTime.Now }
            );
        }

        private void SeedDevices(ModelBuilder modelBuilder)
        {
            // 房间ID
            int livingRoomId = 1;
            int masterBedroomId = 2;
            int kitchenId = 4;
            int bathroomId = 5;
            int entranceId = 7;

            // 设备类型ID
            int acTypeId = 1;
            int lightTypeId = 2;
            int lockTypeId = 3;
            int tempSensorTypeId = 6;
            int humiditySensorTypeId = 7;
            int fanTypeId = 5;

            modelBuilder.Entity<DeviceModel>().HasData(
                // 1. 客厅空调 - ac-liv-001
                new DeviceModel
                {
                    Id = 1,
                    Name = "客厅空调",
                    DeviceNumber = "001",
                    FullDeviceId = "ac-liv-001",
                    RoomId = livingRoomId,
                    DeviceTypeId = acTypeId,
                    RoomIdentifier = "living",
                    TypeIdentifier = "ac",
                    Icon = "fa-wind",
                    IsOn = false,
                    StatusText = "离线",
                    Detail = "空调 · 等待连接",
                    Progress = 0,
                    ProgressColor = "#a0a0a0",
                    Mode = "cool",
                    Temperature = 24,
                    SwingVertical = false,
                    SwingHorizontal = false,
                    Light = false,
                    Quiet = false,
                    CreatedAt = DateTime.Now
                },
                // 2. 客厅灯光 - light-liv-001
                new DeviceModel
                {
                    Id = 2,
                    Name = "客厅灯光",
                    DeviceNumber = "001",
                    FullDeviceId = "light-liv-001",
                    RoomId = livingRoomId,
                    DeviceTypeId = lightTypeId,
                    RoomIdentifier = "living",
                    TypeIdentifier = "light",
                    Icon = "fa-lightbulb",
                    IsOn = false,
                    StatusText = "离线",
                    Detail = "灯光 · 等待连接",
                    Progress = 0,
                    ProgressColor = "#a0a0a0",
                    CreatedAt = DateTime.Now
                },
                // 3. 入口门锁 - lock-ent-001
                new DeviceModel
                {
                    Id = 3,
                    Name = "入口门锁",
                    DeviceNumber = "001",
                    FullDeviceId = "lock-ent-001",
                    RoomId = entranceId,
                    DeviceTypeId = lockTypeId,
                    RoomIdentifier = "entrance",
                    TypeIdentifier = "lock",
                    Icon = "fa-lock",
                    IsOn = false,
                    StatusText = "离线",
                    Detail = "门锁 · 等待连接",
                    Progress = 0,
                    ProgressColor = "#a0a0a0",
                    Humidity = 100,
                    CreatedAt = DateTime.Now
                },
                // 4. 厨房温度传感器 - temp-kit-001
                new DeviceModel
                {
                    Id = 4,
                    Name = "厨房温度传感器",
                    DeviceNumber = "001",
                    FullDeviceId = "temp-kit-001",
                    RoomId = kitchenId,
                    DeviceTypeId = tempSensorTypeId,
                    RoomIdentifier = "kitchen",
                    TypeIdentifier = "temp-sensor",
                    Icon = "fa-thermometer-half",
                    IsOn = false,
                    StatusText = "离线",
                    Detail = "温度传感器 · 等待连接",
                    Progress = 0,
                    ProgressColor = "#a0a0a0",
                    Temperature = 22.5,
                    CreatedAt = DateTime.Now
                },
                // 5. 浴室湿度传感器 - hum-bat-001
                new DeviceModel
                {
                    Id = 5,
                    Name = "浴室湿度传感器",
                    DeviceNumber = "001",
                    FullDeviceId = "hum-bat-001",
                    RoomId = bathroomId,
                    DeviceTypeId = humiditySensorTypeId,
                    RoomIdentifier = "bathroom",
                    TypeIdentifier = "humidity-sensor",
                    Icon = "fa-tint",
                    IsOn = false,
                    StatusText = "离线",
                    Detail = "湿度传感器 · 等待连接",
                    Progress = 0,
                    ProgressColor = "#a0a0a0",
                    Humidity = 65,
                    CreatedAt = DateTime.Now
                },
                // 6. 主卧风扇 - fan-mbd-001
                new DeviceModel
                {
                    Id = 6,
                    Name = "卧室风扇",
                    DeviceNumber = "001",
                    FullDeviceId = "fan-mbd-001",
                    RoomId = masterBedroomId,
                    DeviceTypeId = fanTypeId,
                    RoomIdentifier = "master-bedroom",
                    TypeIdentifier = "fan",
                    Icon = "fa-fan",
                    IsOn = false,
                    StatusText = "离线",
                    Detail = "风扇 · 等待连接",
                    Progress = 0,
                    ProgressColor = "#a0a0a0",
                    MotorSpeed = 3,
                    CreatedAt = DateTime.Now
                }
            );
        }

        private void SeedScenes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SceneModel>().HasData(
                new SceneModel
                {
                    Id = 1,
                    SceneName = "晚安模式",
                    Icon = "fa-moon",
                    Description = "关闭所有灯光，调整空调温度",
                    TriggerType = "time",
                    TriggerCondition = "{\"time\":\"22:00\"}",
                    Actions = "[{\"type\":\"light\",\"action\":\"off\"},{\"type\":\"ac\",\"action\":\"set_temperature\",\"value\":24}]",
                    ExecuteTime = "22:00",
                    RepeatDays = "mon,tue,wed,thu,fri,sat,sun",
                    IsActive = true,
                    ExecuteCount = 0,
                    CreatedAt = DateTime.Now
                },
                new SceneModel
                {
                    Id = 2,
                    SceneName = "晨间唤醒",
                    Icon = "fa-sun",
                    Description = "打开卧室窗帘，启动咖啡机",
                    TriggerType = "time",
                    TriggerCondition = "{\"time\":\"07:30\"}",
                    Actions = "[{\"type\":\"curtain\",\"action\":\"open\"},{\"type\":\"coffee\",\"action\":\"on\"}]",
                    ExecuteTime = "07:30",
                    RepeatDays = "mon,tue,wed,thu,fri",
                    IsActive = true,
                    ExecuteCount = 0,
                    CreatedAt = DateTime.Now
                },
                new SceneModel
                {
                    Id = 3,
                    SceneName = "离家布防",
                    Icon = "fa-umbrella-beach",
                    Description = "关闭所有设备，启动安防系统",
                    TriggerType = "manual",
                    TriggerCondition = "{}",
                    Actions = "[{\"type\":\"all\",\"action\":\"off\"},{\"type\":\"security\",\"action\":\"on\"}]",
                    ExecuteTime = "",
                    RepeatDays = "",
                    IsActive = false,
                    ExecuteCount = 0,
                    CreatedAt = DateTime.Now
                },
                new SceneModel
                {
                    Id = 4,
                    SceneName = "晚餐模式",
                    Icon = "fa-pizza-slice",
                    Description = "调暗灯光，播放音乐",
                    TriggerType = "time",
                    TriggerCondition = "{\"time\":\"18:00\"}",
                    Actions = "[{\"type\":\"light\",\"action\":\"dim\",\"value\":50},{\"type\":\"music\",\"action\":\"play\",\"playlist\":\"dinner\"}]",
                    ExecuteTime = "18:00",
                    RepeatDays = "mon,tue,wed,thu,fri",
                    IsActive = false,
                    ExecuteCount = 0,
                    CreatedAt = DateTime.Now
                }
            );
        }
    }
}