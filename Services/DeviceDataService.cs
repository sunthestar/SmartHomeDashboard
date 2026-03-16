using System.Text.Json;

namespace SmartHomeDashboard.Services
{
    public class DeviceDataService
    {
        private readonly string _dataFilePath;
        private List<DeviceModel> _devices;

        public DeviceDataService(IWebHostEnvironment env)
        {
            var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
            _dataFilePath = Path.Combine(dataDir, "devices.json");
            _devices = LoadDevices();

            if (_devices == null)
            {
                _devices = new List<DeviceModel>();
            }

            Console.WriteLine($"DeviceDataService 初始化完成，当前设备数量: {_devices.Count}");
        }

        private List<DeviceModel> LoadDevices()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    var json = File.ReadAllText(_dataFilePath);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("devices.json 文件为空，返回空列表");
                        return new List<DeviceModel>();
                    }

                    var devices = JsonSerializer.Deserialize<List<DeviceModel>>(json);
                    if (devices != null)
                    {
                        // 强制将所有设备设置为离线状态
                        foreach (var device in devices)
                        {
                            device.IsOn = false;
                            device.StatusText = "离线";
                            device.ProgressColor = "#a0a0a0";
                        }
                        Console.WriteLine($"成功加载 {devices.Count} 个设备");
                        return devices;
                    }
                }
                else
                {
                    Console.WriteLine("devices.json 文件不存在，创建空文件");
                    SaveDevices(new List<DeviceModel>());
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"JSON解析错误: {ex.Message}");
                SaveDevices(new List<DeviceModel>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载设备数据失败: {ex.Message}");
            }

            return new List<DeviceModel>();
        }

        private List<DeviceModel> GetDefaultDevices()
        {
            // 返回一些默认设备用于测试，但都设置为离线状态
            return new List<DeviceModel>
            {
                new DeviceModel { Id = 1, Name = "客厅空调", Room = "living", Type = "ac", Icon = "fa-wind", IsOn = false, StatusText = "离线", Detail = "空调 · 等待连接", Power = "0W", PowerValue = 0, Progress = 0, ProgressColor = "#a0a0a0", Mode = "cool", Temperature = 24, SwingVertical = false, SwingHorizontal = false, Light = false, Quiet = false },
                new DeviceModel { Id = 2, Name = "客厅灯光", Room = "living", Type = "light", Icon = "fa-lightbulb", IsOn = false, StatusText = "离线", Detail = "灯光 · 等待连接", Power = "0W", PowerValue = 0, Progress = 0, ProgressColor = "#a0a0a0" },
                new DeviceModel { Id = 3, Name = "入口门锁", Room = "entrance", Type = "lock", Icon = "fa-lock", IsOn = false, StatusText = "离线", Detail = "门锁 · 等待连接", Power = "0W", PowerValue = 0, Progress = 0, ProgressColor = "#a0a0a0", Humidity = 100 },
                new DeviceModel { Id = 4, Name = "厨房温度传感器", Room = "kitchen", Type = "temp-sensor", Icon = "fa-thermometer-half", IsOn = false, StatusText = "离线", Detail = "温度传感器 · 等待连接", Power = "0W", PowerValue = 0, Progress = 0, ProgressColor = "#a0a0a0", Temperature = 22.5 },
                new DeviceModel { Id = 5, Name = "浴室湿度传感器", Room = "bathroom", Type = "humidity-sensor", Icon = "fa-tint", IsOn = false, StatusText = "离线", Detail = "湿度传感器 · 等待连接", Power = "0W", PowerValue = 0, Progress = 0, ProgressColor = "#a0a0a0", Humidity = 65 },
                new DeviceModel { Id = 6, Name = "卧室风扇", Room = "master-bedroom", Type = "fan", Icon = "fa-fan", IsOn = false, StatusText = "离线", Detail = "风扇 · 等待连接", Power = "0W", PowerValue = 0, Progress = 0, ProgressColor = "#a0a0a0", MotorSpeed = 3 }
            };
        }

        private void SaveDevices(List<DeviceModel> devices)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(devices, options);
                File.WriteAllText(_dataFilePath, json);
                Console.WriteLine($"设备数据已保存到: {_dataFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存设备数据失败: {ex.Message}");
            }
        }

        private void SaveDevices()
        {
            SaveDevices(_devices);
        }

        public List<DeviceModel> GetAllDevices()
        {
            return _devices ?? new List<DeviceModel>();
        }

        public DeviceModel AddDevice(DeviceAddModel newDevice)
        {
            int newId = _devices.Count > 0 ? _devices.Max(d => d.Id) + 1 : 1;

            double powerValue = ParsePowerValue(newDevice.Power);

            // 新设备默认为离线状态
            string statusText = "离线";
            string detail = "等待连接";

            // 根据设备类型设置详情
            switch (newDevice.Type)
            {
                case "ac":
                    detail = "空调 · 等待连接";
                    break;
                case "lock":
                    detail = "门锁 · 等待连接";
                    break;
                case "temp-sensor":
                    detail = "温度传感器 · 等待连接";
                    break;
                case "humidity-sensor":
                    detail = "湿度传感器 · 等待连接";
                    break;
                case "fan":
                    detail = "风扇 · 等待连接";
                    break;
                case "motor":
                    detail = "电机 · 等待连接";
                    break;
                case "light":
                    detail = "灯光 · 等待连接";
                    break;
                case "camera":
                    detail = "摄像头 · 等待连接";
                    break;
            }

            var device = new DeviceModel
            {
                Id = newId,
                Name = newDevice.Name,
                Room = newDevice.Room,
                Type = newDevice.Type,
                Icon = newDevice.Icon,
                IsOn = false,
                StatusText = statusText,
                Detail = detail,
                Power = newDevice.Power,
                PowerValue = powerValue,
                Progress = newDevice.Progress,
                ProgressColor = "#a0a0a0",
                Temperature = newDevice.Temperature,
                Humidity = newDevice.Humidity,
                MotorSpeed = newDevice.MotorSpeed,
                Mode = newDevice.Mode,
                Direction = newDevice.Direction,
                SwingVertical = false,
                SwingHorizontal = false,
                Light = false,
                Quiet = false
            };

            _devices.Add(device);
            SaveDevices();
            return device;
        }

        public bool DeleteDevice(int id)
        {
            var device = _devices.FirstOrDefault(d => d.Id == id);
            if (device != null)
            {
                _devices.Remove(device);
                SaveDevices();
                return true;
            }
            return false;
        }

        public bool UpdateDeviceStatus(int id, bool isOn, string statusText)
        {
            var device = _devices.FirstOrDefault(d => d.Id == id);
            if (device != null)
            {
                device.IsOn = isOn;
                device.StatusText = statusText;

                if (!isOn && statusText == "离线")
                {
                    device.ProgressColor = "#a0a0a0";
                }
                else
                {
                    device.ProgressColor = device.IsOn ? null : "#a0b8d0";
                }

                SaveDevices();
                return true;
            }
            return false;
        }

        public bool UpdateDeviceTemperature(int id, double temperature)
        {
            var device = _devices.FirstOrDefault(d => d.Id == id);
            if (device != null && device.Type == "temp-sensor")
            {
                device.Temperature = temperature;
                if (device.IsOn)
                {
                    device.StatusText = $"温度 {temperature:F1}°C";
                }
                SaveDevices();
                return true;
            }
            return false;
        }

        public bool UpdateDeviceHumidity(int id, int humidity)
        {
            var device = _devices.FirstOrDefault(d => d.Id == id);
            if (device != null)
            {
                device.Humidity = humidity;
                if (device.Type == "humidity-sensor" && device.IsOn)
                {
                    device.StatusText = $"湿度 {humidity}%";
                }
                SaveDevices();
                return true;
            }
            return false;
        }

        public bool UpdateDeviceMotorSpeed(int id, int speed)
        {
            var device = _devices.FirstOrDefault(d => d.Id == id);
            if (device != null && (device.Type == "motor" || device.Type == "fan"))
            {
                device.MotorSpeed = speed;
                if (device.Type == "fan" && device.IsOn)
                {
                    device.StatusText = $"风速 {speed}档";
                }
                SaveDevices();
                return true;
            }
            return false;
        }

        public bool UpdateDeviceMode(int id, string mode)
        {
            var device = _devices.FirstOrDefault(d => d.Id == id);
            if (device != null && device.Type == "ac")
            {
                device.Mode = mode;

                if (device.IsOn)
                {
                    string modeText = mode switch
                    {
                        "cool" => "制冷",
                        "heat" => "制热",
                        "fan" => "送风",
                        "dry" => "除湿",
                        "auto" => "自动",
                        _ => mode
                    };
                    device.StatusText = $"{modeText} {device.Temperature ?? 23}°C";
                }

                SaveDevices();
                return true;
            }
            return false;
        }

        public bool UpdateDeviceDirection(int id, string direction)
        {
            var device = _devices.FirstOrDefault(d => d.Id == id);
            if (device != null && device.Type == "motor")
            {
                device.Direction = direction;
                if (device.IsOn)
                {
                    device.StatusText = direction switch
                    {
                        "forward" => "正转",
                        "reverse" => "反转",
                        _ => "停止"
                    };
                }
                SaveDevices();
                return true;
            }
            return false;
        }

        public bool UpdateDeviceAcTemperature(int id, double temperature)
        {
            var device = _devices.FirstOrDefault(d => d.Id == id);
            if (device != null && device.Type == "ac")
            {
                device.Temperature = temperature;

                if (device.IsOn)
                {
                    string modeText = device.Mode switch
                    {
                        "cool" => "制冷",
                        "heat" => "制热",
                        "fan" => "送风",
                        "dry" => "除湿",
                        "auto" => "自动",
                        _ => device.Mode ?? "制冷"
                    };
                    device.StatusText = $"{modeText} {temperature}°C";
                }

                SaveDevices();
                return true;
            }
            return false;
        }

        public bool UpdateDevicePower(int id, double power)
        {
            var device = _devices.FirstOrDefault(d => d.Id == id);
            if (device != null)
            {
                device.PowerValue = power / 1000;
                device.Power = power >= 1000 ? $"{(power / 1000):F2}kW" : $"{power:F0}W";
                SaveDevices();
                return true;
            }
            return false;
        }

        public bool UpdateDeviceSwingVertical(int id, bool enabled)
        {
            var device = _devices.FirstOrDefault(d => d.Id == id);
            if (device != null && device.Type == "ac")
            {
                device.SwingVertical = enabled;
                SaveDevices();
                return true;
            }
            return false;
        }

        public bool UpdateDeviceSwingHorizontal(int id, bool enabled)
        {
            var device = _devices.FirstOrDefault(d => d.Id == id);
            if (device != null && device.Type == "ac")
            {
                device.SwingHorizontal = enabled;
                SaveDevices();
                return true;
            }
            return false;
        }

        public bool UpdateDeviceLight(int id, bool enabled)
        {
            var device = _devices.FirstOrDefault(d => d.Id == id);
            if (device != null && device.Type == "ac")
            {
                device.Light = enabled;
                SaveDevices();
                return true;
            }
            return false;
        }

        public bool UpdateDeviceQuiet(int id, bool enabled)
        {
            var device = _devices.FirstOrDefault(d => d.Id == id);
            if (device != null && device.Type == "ac")
            {
                device.Quiet = enabled;
                SaveDevices();
                return true;
            }
            return false;
        }

        private double ParsePowerValue(string powerText)
        {
            if (string.IsNullOrEmpty(powerText)) return 0;

            powerText = powerText.ToLower().Trim();

            if (powerText.Contains('w'))
            {
                double value = double.TryParse(powerText.Replace("w", "").Trim(), out var result) ? result : 0;
                return value / 1000;
            }

            if (powerText.Contains("kw"))
            {
                return double.TryParse(powerText.Replace("kw", "").Trim(), out var result) ? result : 0;
            }

            if (double.TryParse(powerText, out var numValue))
            {
                return numValue / 1000;
            }

            return 0;
        }

        public void ResetAllDevicesToOffline()
        {
            foreach (var device in _devices)
            {
                device.IsOn = false;
                device.StatusText = "离线";
                device.ProgressColor = "#a0a0a0";
            }
            SaveDevices();
        }

        public class DeviceAddModel
        {
            public string Name { get; set; } = "";
            public string Room { get; set; } = "";
            public string Type { get; set; } = "";
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

        public class DeviceModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Room { get; set; } = "";
            public string Type { get; set; } = "";
            public string Icon { get; set; } = "";
            public bool IsOn { get; set; }
            public string StatusText { get; set; } = "";
            public string Detail { get; set; } = "";
            public string Power { get; set; } = "";
            public double PowerValue { get; set; }
            public int Progress { get; set; }
            public string? ProgressColor { get; set; }

            // 传感器属性
            public double? Temperature { get; set; }
            public int? Humidity { get; set; }

            // 电机/风扇属性
            public int? MotorSpeed { get; set; }

            // 空调属性
            public string? Mode { get; set; }
            public bool? SwingVertical { get; set; }
            public bool? SwingHorizontal { get; set; }
            public bool? Light { get; set; }
            public bool? Quiet { get; set; }

            // 电机方向
            public string? Direction { get; set; }
        }
    }
}