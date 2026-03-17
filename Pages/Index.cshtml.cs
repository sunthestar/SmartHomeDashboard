using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartHomeDashboard.Services;

namespace SmartHomeDashboard.Pages
{
    public class IndexModel : PageModel
    {
        private readonly DeviceDataService _deviceService;

        public IndexModel(DeviceDataService deviceService)
        {
            _deviceService = deviceService;
        }

        public IActionResult OnGetLogout()
        {
            HttpContext.Session.Remove("IsLoggedIn");
            return RedirectToPage("/Login");
        }

        // KPI 数据
        public string RealTimePower { get; set; } = "0.00";
        public string WaterUsage { get; set; } = "1.42";
        public string AverageTemp { get; set; } = "21.5";
        public int SecurityDevices { get; set; } = 8;

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
        public int ActiveScenes { get; set; } = 2;

        // 设备列表
        public List<DeviceDataService.DeviceModel> Devices { get; set; } = new List<DeviceDataService.DeviceModel>();

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

        // 自动化场景列表
        public List<SceneModel> Scenes { get; set; } = new List<SceneModel>
        {
            new SceneModel { Id = 1, Name = "晚安模式", Icon = "fa-moon", IsActive = true, Description = "关闭所有灯光，调整空调温度", Time = "22:00" },
            new SceneModel { Id = 2, Name = "晨间唤醒 (07:30)", Icon = "fa-sun", IsActive = true, Description = "打开卧室窗帘，启动咖啡机", Time = "07:30" },
            new SceneModel { Id = 3, Name = "离家布防", Icon = "fa-umbrella-beach", IsActive = false, Description = "关闭所有设备，启动安防系统", Time = "手动" },
            new SceneModel { Id = 4, Name = "晚餐模式 (18:00)", Icon = "fa-pizza-slice", IsActive = false, Description = "调暗灯光，播放音乐", Time = "18:00" }
        };

        public void OnGet()
        {
            Devices = _deviceService.GetAllDevices() ?? new List<DeviceDataService.DeviceModel>();

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

            // 计算安全设备数量（门锁和摄像头）
            SecurityDevices = Devices.Count(d => d.Type == "lock" || d.Type == "camera");

            // 计算活动场景数量
            ActiveScenes = Scenes.Count(s => s.IsActive);

            Console.WriteLine($"Index页面加载，发现 {Devices.Count} 个设备，其中 {OnlineDevices} 个在线");
        }

        public class CameraModel
        {
            public string Name { get; set; } = "";
            public string Icon { get; set; } = "";
            public bool HasMotion { get; set; }
            public string TimeText { get; set; } = "";
            public string Status { get; set; } = "";
        }

        public class SceneModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Icon { get; set; } = "";
            public bool IsActive { get; set; }
            public string Description { get; set; } = "";
            public string Time { get; set; } = "";
        }
    }
}