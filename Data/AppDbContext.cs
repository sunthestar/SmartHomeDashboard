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
                entity.Property(e => e.Power).HasMaxLength(20);
                entity.Property(e => e.Mode).HasMaxLength(20);
                entity.Property(e => e.Direction).HasMaxLength(20);
                entity.Property(e => e.ProgressColor).HasMaxLength(20);

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

            // 初始化房间数据
            SeedRooms(modelBuilder);

            // 初始化设备类型数据
            SeedDeviceTypes(modelBuilder);

            // 初始化设备数据
            SeedDevices(modelBuilder);
        }

        private void SeedRooms(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RoomModel>().HasData(
                new RoomModel { Id = 1, RoomId = "living", RoomName = "客厅", Description = "主要活动区域", DeviceCount = 0, OnlineCount = 0 },
                new RoomModel { Id = 2, RoomId = "master-bedroom", RoomName = "主卧", Description = "主人卧室", DeviceCount = 0, OnlineCount = 0 },
                new RoomModel { Id = 3, RoomId = "second-bedroom", RoomName = "次卧", Description = "次卧/客房", DeviceCount = 0, OnlineCount = 0 },
                new RoomModel { Id = 4, RoomId = "kitchen", RoomName = "厨房", Description = "烹饪区域", DeviceCount = 0, OnlineCount = 0 },
                new RoomModel { Id = 5, RoomId = "bathroom", RoomName = "浴室", Description = "洗浴区域", DeviceCount = 0, OnlineCount = 0 },
                new RoomModel { Id = 6, RoomId = "dining", RoomName = "餐厅", Description = "用餐区域", DeviceCount = 0, OnlineCount = 0 },
                new RoomModel { Id = 7, RoomId = "entrance", RoomName = "入口", Description = "玄关/入口", DeviceCount = 0, OnlineCount = 0 }
            );
        }

        private void SeedDeviceTypes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DeviceTypeModel>().HasData(
                new DeviceTypeModel { Id = 1, TypeId = "ac", TypeName = "空调", Icon = "fa-wind", Description = "智能空调" },
                new DeviceTypeModel { Id = 2, TypeId = "light", TypeName = "灯光", Icon = "fa-lightbulb", Description = "智能灯泡" },
                new DeviceTypeModel { Id = 3, TypeId = "lock", TypeName = "门锁", Icon = "fa-lock", Description = "智能门锁" },
                new DeviceTypeModel { Id = 4, TypeId = "camera", TypeName = "摄像头", Icon = "fa-camera", Description = "网络摄像头" },
                new DeviceTypeModel { Id = 5, TypeId = "fan", TypeName = "风扇", Icon = "fa-fan", Description = "智能风扇" },
                new DeviceTypeModel { Id = 6, TypeId = "temp-sensor", TypeName = "温度传感器", Icon = "fa-thermometer-half", Description = "温度传感器" },
                new DeviceTypeModel { Id = 7, TypeId = "humidity-sensor", TypeName = "湿度传感器", Icon = "fa-tint", Description = "湿度传感器" },
                new DeviceTypeModel { Id = 8, TypeId = "motor", TypeName = "电机", Icon = "fa-cogs", Description = "电机设备" }
            );
        }

        private void SeedDevices(ModelBuilder modelBuilder)
        {
            // 获取房间和类型ID
            int livingRoomId = 1;
            int masterBedroomId = 2;
            int kitchenId = 4;
            int bathroomId = 5;
            int entranceId = 7;

            int acTypeId = 1;
            int lightTypeId = 2;
            int lockTypeId = 3;
            int tempSensorTypeId = 6;
            int humiditySensorTypeId = 7;
            int fanTypeId = 5;

            modelBuilder.Entity<DeviceModel>().HasData(
                // 客厅设备
                new DeviceModel
                {
                    Id = 1,
                    Name = "客厅空调",
                    DeviceNumber = "001",
                    FullDeviceId = "living-ac-001",
                    RoomId = livingRoomId,
                    DeviceTypeId = acTypeId,
                    RoomIdentifier = "living",
                    TypeIdentifier = "ac",
                    Icon = "fa-wind",
                    IsOn = false,
                    StatusText = "离线",
                    Detail = "空调 · 等待连接",
                    Power = "0W",
                    PowerValue = 0,
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
                new DeviceModel
                {
                    Id = 2,
                    Name = "客厅灯光",
                    DeviceNumber = "001",
                    FullDeviceId = "living-light-001",
                    RoomId = livingRoomId,
                    DeviceTypeId = lightTypeId,
                    RoomIdentifier = "living",
                    TypeIdentifier = "light",
                    Icon = "fa-lightbulb",
                    IsOn = false,
                    StatusText = "离线",
                    Detail = "灯光 · 等待连接",
                    Power = "0W",
                    PowerValue = 0,
                    Progress = 0,
                    ProgressColor = "#a0a0a0",
                    CreatedAt = DateTime.Now
                },

                // 入口设备
                new DeviceModel
                {
                    Id = 3,
                    Name = "入口门锁",
                    DeviceNumber = "001",
                    FullDeviceId = "entrance-lock-001",
                    RoomId = entranceId,
                    DeviceTypeId = lockTypeId,
                    RoomIdentifier = "entrance",
                    TypeIdentifier = "lock",
                    Icon = "fa-lock",
                    IsOn = false,
                    StatusText = "离线",
                    Detail = "门锁 · 等待连接",
                    Power = "0W",
                    PowerValue = 0,
                    Progress = 0,
                    ProgressColor = "#a0a0a0",
                    Humidity = 100,
                    CreatedAt = DateTime.Now
                },

                // 厨房设备
                new DeviceModel
                {
                    Id = 4,
                    Name = "厨房温度传感器",
                    DeviceNumber = "001",
                    FullDeviceId = "kitchen-temp-sensor-001",
                    RoomId = kitchenId,
                    DeviceTypeId = tempSensorTypeId,
                    RoomIdentifier = "kitchen",
                    TypeIdentifier = "temp-sensor",
                    Icon = "fa-thermometer-half",
                    IsOn = false,
                    StatusText = "离线",
                    Detail = "温度传感器 · 等待连接",
                    Power = "0W",
                    PowerValue = 0,
                    Progress = 0,
                    ProgressColor = "#a0a0a0",
                    Temperature = 22.5,
                    CreatedAt = DateTime.Now
                },

                // 浴室设备
                new DeviceModel
                {
                    Id = 5,
                    Name = "浴室湿度传感器",
                    DeviceNumber = "001",
                    FullDeviceId = "bathroom-humidity-sensor-001",
                    RoomId = bathroomId,
                    DeviceTypeId = humiditySensorTypeId,
                    RoomIdentifier = "bathroom",
                    TypeIdentifier = "humidity-sensor",
                    Icon = "fa-tint",
                    IsOn = false,
                    StatusText = "离线",
                    Detail = "湿度传感器 · 等待连接",
                    Power = "0W",
                    PowerValue = 0,
                    Progress = 0,
                    ProgressColor = "#a0a0a0",
                    Humidity = 65,
                    CreatedAt = DateTime.Now
                },

                // 主卧设备
                new DeviceModel
                {
                    Id = 6,
                    Name = "卧室风扇",
                    DeviceNumber = "001",
                    FullDeviceId = "master-bedroom-fan-001",
                    RoomId = masterBedroomId,
                    DeviceTypeId = fanTypeId,
                    RoomIdentifier = "master-bedroom",
                    TypeIdentifier = "fan",
                    Icon = "fa-fan",
                    IsOn = false,
                    StatusText = "离线",
                    Detail = "风扇 · 等待连接",
                    Power = "0W",
                    PowerValue = 0,
                    Progress = 0,
                    ProgressColor = "#a0a0a0",
                    MotorSpeed = 3,
                    CreatedAt = DateTime.Now
                }
            );
        }
    }
}