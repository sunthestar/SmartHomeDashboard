using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using SmartHomeDashboard.Services;
using SmartHomeDashboard.Models;

namespace SmartHomeDashboard.Pages
{
    public class IndexModel : PageModel
    {
        private readonly DeviceDataService _deviceService;
        private readonly RoomService _roomService;
        private readonly SceneService _sceneService;
        private readonly SystemLogService _logService;

        public IndexModel(
            DeviceDataService deviceService,
            RoomService roomService,
            SceneService sceneService,
            SystemLogService logService)
        {
            _deviceService = deviceService;
            _roomService = roomService;
            _sceneService = sceneService;
            _logService = logService;
        }

        // KPI 数据
        public string RealTimePower { get; set; } = "0.00";
        public string AverageHumidity { get; set; } = "--";
        public string AverageTemp { get; set; } = "--";
        public int SecurityDevices { get; set; } = 0;

        // 设备统计
        public int OnlineDevices { get; set; }

        // 能耗数据
        public string TotalEnergy { get; set; } = "18.4";
        public string EstimatedEnergy { get; set; } = "32.1";
        public string PeakPower { get; set; } = "3.2";

        // 柱状图数据
        public int[] EnergyData { get; set; } = new int[] { 32, 48, 58, 70, 62, 40, 30 };

        // 监控摄像头
        public int CameraCount { get; set; } = 2;

        // 自动化场景统计
        public int ActiveScenes { get; set; } = 0;

        // 设备列表
        public List<DeviceModel> Devices { get; set; } = new List<DeviceModel>();

        // 房间列表
        public List<RoomModel> Rooms { get; set; } = new List<RoomModel>();

        // 设备类型列表
        public List<DeviceTypeModel> DeviceTypes { get; set; } = new List<DeviceTypeModel>();

        // 场景列表
        public List<SceneModel> Scenes { get; set; } = new List<SceneModel>();

        // 未读日志数量
        public int UnreadLogCount { get; set; }

        // 监控摄像头列表
        public List<CameraModel> Cameras { get; set; } = new List<CameraModel>
        {
            new CameraModel
            {
                Name = "正门猫眼",
                Icon = "fa-door-open",
                HasMotion = true,
                TimeText = "10 秒前 · 人员停留",
                Status = "访客按铃 | 双向对讲"
            },
            new CameraModel
            {
                Name = "后院庭院",
                Icon = "fa-tree",
                HasMotion = false,
                TimeText = "画面静止 · 正常",
                Status = ""
            }
        };

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            // 获取设备列表
            Devices = await _deviceService.GetAllDevicesAsync() ?? new List<DeviceModel>();

            // 获取房间列表
            Rooms = await _roomService.GetAllRoomsAsync() ?? new List<RoomModel>();

            // 获取设备类型列表
            DeviceTypes = await GetDeviceTypesAsync();

            // 获取场景列表
            Scenes = await _sceneService.GetAllScenesAsync();

            // 获取未读日志数量
            UnreadLogCount = await _logService.GetUnreadCountAsync();

            // 重新计算在线设备数量
            OnlineDevices = Devices.Count(d => d.IsOn && d.StatusText != "离线");

            // 计算实时功率
            double totalPower = 0;
            foreach (var device in Devices)
            {
                if (device.IsOn && device.StatusText != "离线")
                {
                    totalPower += device.PowerValue;
                }
            }
            RealTimePower = totalPower.ToString("F2");

            // 计算平均室温
            CalculateAverageRoomTemp();

            // 计算平均湿度
            CalculateAverageHumidity();

            // 计算安全设备数量（门锁和摄像头）
            SecurityDevices = Devices.Count(d => d.TypeIdentifier == "lock" || d.TypeIdentifier == "camera");

            // 计算活动场景数量
            ActiveScenes = Scenes.Count(s => s.IsActive);

            Console.WriteLine($"Index页面加载，发现 {Devices.Count} 个设备，其中 {OnlineDevices} 个在线");
        }

        private async Task<List<DeviceTypeModel>> GetDeviceTypesAsync()
        {
            // 这里应该从数据库获取，暂时返回默认列表
            return await Task.FromResult(new List<DeviceTypeModel>
            {
                new DeviceTypeModel { Id = 1, TypeId = "ac", TypeName = "空调", Icon = "fa-wind", Description = "智能空调" },
                new DeviceTypeModel { Id = 2, TypeId = "light", TypeName = "灯光", Icon = "fa-lightbulb", Description = "智能灯泡" },
                new DeviceTypeModel { Id = 3, TypeId = "lock", TypeName = "门锁", Icon = "fa-lock", Description = "智能门锁" },
                new DeviceTypeModel { Id = 4, TypeId = "camera", TypeName = "摄像头", Icon = "fa-camera", Description = "网络摄像头" },
                new DeviceTypeModel { Id = 5, TypeId = "fan", TypeName = "风扇", Icon = "fa-fan", Description = "智能风扇" },
                new DeviceTypeModel { Id = 6, TypeId = "temp-sensor", TypeName = "温度传感器", Icon = "fa-thermometer-half", Description = "温度传感器" },
                new DeviceTypeModel { Id = 7, TypeId = "humidity-sensor", TypeName = "湿度传感器", Icon = "fa-tint", Description = "湿度传感器" },
                new DeviceTypeModel { Id = 8, TypeId = "motor", TypeName = "电机", Icon = "fa-cogs", Description = "电机设备" }
            });
        }

        private void CalculateAverageRoomTemp()
        {
            var tempSensors = Devices.Where(d => d.TypeIdentifier == "temp-sensor" && d.Temperature.HasValue).ToList();
            if (tempSensors.Any())
            {
                var avgTemp = tempSensors.Average(d => d.Temperature.Value);
                AverageTemp = avgTemp.ToString("F1");
            }
            else
            {
                AverageTemp = "--";
            }
        }

        private void CalculateAverageHumidity()
        {
            var humiditySensors = Devices.Where(d => d.TypeIdentifier == "humidity-sensor" && d.Humidity.HasValue).ToList();
            if (humiditySensors.Any())
            {
                var avgHumidity = humiditySensors.Average(d => d.Humidity.Value);
                AverageHumidity = avgHumidity.ToString("F0");
            }
            else
            {
                AverageHumidity = "--";
            }
        }

        public class CameraModel
        {
            public string Name { get; set; } = "";
            public string Icon { get; set; } = "";
            public bool HasMotion { get; set; }
            public string TimeText { get; set; } = "";
            public string Status { get; set; } = "";
        }
    }
}