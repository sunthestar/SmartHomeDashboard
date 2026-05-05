# 智能家居后台监控系统

## 项目概述

智能家居后台监控系统是一个基于ASP.NET Core的Web应用程序，用于集中管理和监控智能家居设备。系统支持实时设备状态监控、设备控制、自动化场景管理、能耗分析、TCP设备通信及AI语音助手等功能。

## 技术栈

| 技术 | 说明 |
|------|------|
| 后端框架 | ASP.NET Core 8.0 |
| 前端技术 | Razor Pages, JavaScript, SignalR, HTML5/CSS3 |
| 数据库 | SQLite (Entity Framework Core 7.0) |
| 通信协议 | TCP/IP (设备连接与控制) |
| 实时通信 | SignalR WebSocket |
| AI助手 | Web Speech API (语音识别) |

## 项目结构

```
SmartHomeDashboard/
├── App_Data/                          # 数据存储目录
│   └── smarthome.db                    # SQLite数据库文件
│
├── Controllers/                         # API控制器
│   ├── AIAssistantController.cs         # AI助手对话API
│   ├── ApiController.cs                  # 设备管理API (增删查)
│   └── TcpController.cs                   # TCP设备控制API
│
├── Data/                                 # 数据库上下文
│   └── AppDbContext.cs                    # EF Core数据库上下文 + 种子数据
│
├── Hubs/                                 # SignalR通信中心
│   └── DeviceHub.cs                        # 设备实时通信Hub
│
├── Middleware/                           # 中间件
│   └── LoginCheckMiddleware.cs             # 登录状态检查中间件
│
├── Migrations/                           # EF Core迁移文件
│   └── AppDbContextModelSnapshot.cs        # 数据库模型快照
│
├── Models/                                # 数据模型
│   ├── DeviceModels.cs                      # 房间/设备类型/设备/TCP连接模型
│   ├── LoginModels.cs                        # 登录设置模型
│   ├── SceneModel.cs                          # 自动化场景模型
│   ├── SystemLogModel.cs                       # 系统日志模型
│   └── TcpMessage.cs                            # TCP通信消息模型
│
├── Pages/                                   # Razor Pages页面
│   ├── Shared/
│   │   └── _Layout.cshtml                     # 布局页
│   ├── Index.cshtml                            # 主仪表板页
│   ├── Index.cshtml.cs                          # 主仪表板逻辑
│   ├── Login.cshtml                              # 登录页
│   └── Login.cshtml.cs                            # 登录逻辑
│
├── Services/                                 # 业务服务层
│   ├── AIAssistantService.cs                   # AI助手服务
│   ├── DeviceDataService.cs                      # 设备数据服务
│   ├── LoginService.cs                             # 登录验证服务
│   ├── RoomService.cs                               # 房间管理服务
│   ├── SceneService.cs                               # 自动化场景服务
│   ├── SystemLogService.cs                            # 系统日志服务
│   ├── TcpConnectionService.cs                         # TCP连接管理服务
│   ├── TcpDeviceService.cs                              # TCP设备服务
│   └── TcpServerService.cs                               # TCP服务器服务
│
├── wwwroot/                                  # 静态资源
│   ├── css/
│   │   └── site.css                            # 全局样式(日/夜模式)
│   └── js/
│       └── site.js                               # 全局脚本
│
├── Program.cs                                 # 应用程序入口
├── appsettings.json                            # 应用程序配置
└── SmartHomeDashboard.csproj                    # 项目文件
```

## 数据库设计

### 表结构总览

| 表名 | 说明 | 记录数 |
|------|------|--------|
| Rooms | 房间信息 | 7 |
| DeviceTypes | 设备类型 | 8 |
| Devices | 设备信息 | 动态 |
| TcpConnections | TCP连接记录 | 动态 |
| SystemLogs | 系统日志 | 动态 |
| Scenes | 自动化场景 | 4 |
| LoginSettings | 登录设置 | 1 |

### 房间信息表 (Rooms)

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键 |
| RoomId | varchar(50) | 房间标识符(living/master-bedroom等) |
| RoomName | varchar(50) | 房间名称 |
| Description | varchar(200) | 房间描述 |
| DeviceCount | int | 设备数量 |
| OnlineCount | int | 在线数量 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

### 设备类型表 (DeviceTypes)

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键 |
| TypeId | varchar(50) | 类型标识符(ac/light/lock等) |
| TypeName | varchar(50) | 类型名称 |
| Icon | varchar(50) | 图标 |
| Description | varchar(200) | 类型描述 |
| CreatedAt | datetime | 创建时间 |

### 设备信息表 (Devices)

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键 |
| Name | varchar(100) | 设备名称 |
| DeviceNumber | varchar(20) | 设备编号 |
| FullDeviceId | varchar(100) | 完整设备ID |
| RoomId | int | 房间ID(外键) |
| DeviceTypeId | int | 设备类型ID(外键) |
| RoomIdentifier | varchar(50) | 房间标识符副本 |
| TypeIdentifier | varchar(50) | 类型标识符副本 |
| Icon | varchar(50) | 图标 |
| IsOn | bit | 开关状态/在线状态 |
| StatusText | varchar(100) | 状态文本 |
| Detail | varchar(200) | 详情描述 |
| Power | varchar(20) | 功率显示 |
| PowerValue | decimal | 功率数值(kW) |
| Progress | int | 进度百分比 |
| ProgressColor | varchar(20) | 进度条颜色 |
| **语义化字段** | | |
| TemperatureValue | decimal | 温度值(温度传感器) |
| HumidityValue | decimal | 湿度值(湿度传感器) |
| BatteryLevel | int | 电池电量 |
| Brightness | int | 亮度(灯光) |
| ColorTemperature | int | 色温(灯光) |
| AcTemperature | int | 设定温度(空调) |
| AcMode | varchar(20) | 模式(空调) |
| AcFanSpeed | varchar(20) | 风速(空调) |
| FanSpeed | int | 转速(风扇) |
| FanSwing | bit | 摆头(风扇) |
| MotorDirection | varchar(20) | 方向(电机) |
| IsRecording | bit | 录制状态(摄像头) |
| MotionDetected | bit | 移动侦测(摄像头) |
| NightMode | varchar(20) | 夜视模式(摄像头) |
| **兼容字段** | | |
| Temperature | decimal | 温度值(旧) |
| Humidity | int | 湿度/电量(旧) |
| Mode | varchar(20) | 空调模式(旧) |
| Direction | varchar(20) | 电机方向(旧) |
| **时间戳** | | |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

### TCP连接表 (TcpConnections)

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键 |
| DeviceId | int | 设备ID(外键) |
| FullDeviceId | varchar(100) | 完整设备标识 |
| DeviceName | varchar(100) | 设备名称 |
| DeviceType | varchar(50) | 设备类型 |
| IpAddress | varchar(50) | IP地址 |
| Port | int | 端口号 |
| ConnectedTime | datetime | 连接时间 |
| LastHeartbeat | datetime | 最后心跳时间 |
| LastSeen | datetime | 最后在线时间 |
| IsOnline | bit | 在线状态 |
| TimeoutCount | int | 超时次数 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

### 系统日志表 (SystemLogs)

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键 |
| Timestamp | datetime | 日志时间 |
| LogType | varchar(50) | 日志类型(device/system/alert/automation) |
| LogLevel | varchar(20) | 日志级别(info/warning/error) |
| Title | varchar(200) | 日志标题 |
| Content | text | 日志内容 |
| DeviceId | int | 设备ID(外键) |
| DeviceName | varchar(100) | 设备名称 |
| ActionType | varchar(50) | 操作类型 |
| ActionDetail | varchar(500) | 操作详情 |
| IsRead | bit | 是否已读 |

### 自动化场景表 (Scenes)

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键 |
| SceneName | varchar(100) | 场景名称 |
| Icon | varchar(50) | 场景图标 |
| Description | varchar(200) | 场景描述 |
| TriggerType | varchar(50) | 触发类型(manual/time) |
| TriggerCondition | text | 触发条件(JSON) |
| Actions | text | 执行动作(JSON) |
| ExecuteTime | varchar(50) | 执行时间 |
| RepeatDays | varchar(50) | 重复周期 |
| IsActive | bit | 是否启用 |
| ExecuteCount | int | 执行次数 |
| LastExecuteTime | datetime | 最后执行时间 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

### 登录设置表 (LoginSettings)

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 主键(固定为1) |
| Password | varchar(200) | 密码 |
| PasswordSalt | varchar(50) | 密码盐值 |
| IsEnabled | bit | 是否启用 |
| LastLoginTime | datetime | 最后登录时间 |
| LastLoginIp | varchar(50) | 最后登录IP |
| LoginCount | int | 登录次数 |
| FailCount | int | 失败次数 |
| LastFailTime | datetime | 最后失败时间 |
| LockUntil | datetime | 锁定截止时间 |
| CreatedAt | datetime | 创建时间 |
| UpdatedAt | datetime | 更新时间 |

### 表关系图

```
┌─────────────┐       ┌─────────────┐       ┌─────────────┐
│   Rooms     │       │ DeviceTypes │       │LoginSettings│
│  (房间表)   	│       │ (设备类型表)  │       │ (登录设置表)  │
└──────┬──────┘       └──────┬──────┘       └─────────────┘
       │                     │                      
       │                     │                      
       ↓                     ↓                      
    ┌─────────────────────────────────┐             
    │            Devices              │             
    │           (设备表)               │             
    └────────────┬────────────────────┘             
                 │                                  
        ┌────────┴────────┐                         
        ↓                 ↓                         
┌─────────────┐    ┌─────────────┐                  
│TcpConnections│    │ SystemLogs  │                  
│ (TCP连接表)  │    │  (日志表)   │                  
└─────────────┘    └─────────────┘                  
                           │                         
                           ↓                         
                    ┌─────────────┐                  
                    │   Scenes    │                  
                    │ (场景表)    │                  
                    └─────────────┘                  
```

## 核心功能模块

### 1. 设备管理

| 功能 | 说明 |
|------|------|
| 自动注册 | TCP设备通过注册消息自动添加到系统 |
| 设备控制 | 支持开关、温度调节、模式切换、风速调节等 |
| 实时监控 | 通过SignalR推送设备状态变更 |
| 设备管理 | 支持手动添加、删除设备 |
| 房间分组 | 按房间和类型分类管理设备 |
| 设备类型 | 空调、灯光、门锁、摄像头、风扇、温度/湿度传感器、电机 |

### 2. TCP通信服务

| 功能 | 说明 |
|------|------|
| 注册处理 | 新设备自动注册到数据库 |
| 心跳维护 | 30秒心跳检测，90秒超时判定离线 |
| 遥测数据 | 接收温度、湿度、电量等传感器数据 |
| 状态同步 | 设备状态实时同步到数据库和前端 |
| 命令下发 | 支持向设备发送控制命令 |

### 3. 自动化场景

| 功能 | 说明 |
|------|------|
| 定时场景 | 按预设时间自动执行 |
| 手动场景 | 用户手动触发 |
| 场景管理 | 创建、编辑、启用/禁用场景 |

### 4. AI智能助手

| 功能 | 说明 |
|------|------|
| 语音识别 | 支持中文语音指令输入 |
| 文本指令 | 支持自然语言文本输入 |
| 设备控制 | 通过语音/文字控制设备开关 |
| 状态查询 | 查询设备状态、房间状态、能耗情况 |
| 场景管理 | 查看场景列表 |

### 5. 系统安全

| 功能 | 说明 |
|------|------|
| 单用户登录 | 无用户名，仅密码验证(默认密码: 123456) |
| 登录保护 | 5次失败后锁定30分钟 |
| 会话管理 | 8小时会话超时 |
| 登录成功重置 | 登录成功后重置失败计数 |

### 6. 数据可视化

| 功能 | 说明 |
|------|------|
| KPI指标 | 实时功率、平均室温、平均湿度、安全设备统计 |
| 能耗趋势 | 柱状图展示今日能耗趋势 |
| 实时监控 | 摄像头画面模拟及移动侦测提示 |
| 最新动态 | 显示最近4条系统日志 |

## 支持的设备类型

| 设备类型 | 标识符 | 控制功能 | 监控数据 |
|----------|--------|----------|----------|
| 空调 | ac | 开关、模式、温度、风速、扫风、灯光、静音 | 温度、功率 |
| 灯光 | light | 开关、亮度、色温 | 功率 |
| 门锁 | lock | 上锁/解锁、自动落锁 | 电量、解锁记录 |
| 摄像头 | camera | 开关、录制、夜视模式 | 移动侦测、在线/离线状态 |
| 风扇 | fan | 开关、风速、摆头 | 功率 |
| 温度传感器 | temp-sensor | - | 温度、电量 |
| 湿度传感器 | humidity-sensor | - | 湿度、电量 |
| 电机 | motor | 方向、转速 | 功率 |

## 配置说明

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "TcpSettings": {
    "Port": 8888,           // TCP服务器监听端口
    "HeartbeatTimeout": 90, // 心跳超时时间(秒)
    "BufferSize": 4096      // 缓冲区大小
  }
}
```

### 默认登录信息

| 配置项 | 值 |
|--------|-----|
| 密码 | 123456 |
| 失败限制 | 5次错误后锁定30分钟 |
| 会话超时 | 8小时 |

## API端点

| 端点 | 方法 | 说明 |
|------|------|------|
| `/api/devices/list` | GET | 获取所有设备列表 |
| `/api/devices/add` | POST | 添加新设备 |
| `/api/devices/delete` | POST | 删除设备 |
| `/api/tcp/status` | GET | 获取TCP服务器状态 |
| `/api/tcp/devices/{deviceId}/command` | POST | 发送命令到设备 |
| `/api/tcp/devices/{deviceId}/turn-on` | POST | 开启设备 |
| `/api/tcp/devices/{deviceId}/turn-off` | POST | 关闭设备 |
| `/api/AIAssistant/chat` | POST | AI对话接口 |
| `/health` | GET | 健康检查 |
| `/api/tcp/debug` | GET | TCP调试信息 |

## 运行说明

### 环境要求

- .NET 8.0 SDK
- SQLite (自动创建数据库)

### 启动命令

```bash
# 还原依赖包
dotnet restore

# 编译项目
dotnet build

# 运行项目
dotnet run
```

### 访问地址

| 协议 | 地址 |
|------|------|
| HTTP | http://localhost:5130 |
| HTTPS | https://localhost:7123 |

### TCP设备模拟

- 使用配套的智能家居网关模拟器连接
- 服务器地址: 127.0.0.1
- 端口: 8888

## 更新记录

| 版本 | 日期 | 主要更新 |
|------|------|----------|
| v0.0.1 | 2026-03-16 | 基础UI、天气功能、日夜切换、设备类型、JSON存储、日志功能 |
| v0.0.2 | 2026-03-17 | 登录UI、登录/登出功能 |
| v0.0.3 | 2026-03-19 | IP定位逻辑优化 |
| v0.0.4 | 2026-03-20 | 数据库架构重构(SQLite)、迁移Entity Framework |
| v0.0.5 | 2026-05-05 | 语义化字段扩展、摄像头开关/在线状态分离、电池电量显示、TCP服务器ADO.NET优化、AI助手集成、SignalR实时推送、登录失败计数重置、启动时设备重置为离线 |

### v0.0.5 详细更新内容

**数据库字段扩展**
- 新增 TemperatureValue、HumidityValue、BatteryLevel 语义化字段
- 新增 Brightness、ColorTemperature 灯光专用字段
- 新增 AcTemperature、AcMode、AcFanSpeed 空调专用字段
- 新增 FanSpeed、FanSwing 风扇专用字段
- 新增 MotorDirection 电机专用字段
- 新增 IsRecording、MotionDetected、NightMode 摄像头专用字段

**TCP服务器优化**
- 修复ADO.NET查询NULL值问题
- 增加心跳超时时间(30秒→90秒)
- 优化设备注册连接查找逻辑
- 摄像头在线/离线状态与开关状态分离

**前端UI优化**
- 摄像头卡片显示"开启"/"关闭"而非"在线"/"离线"
- 摄像头离线时卡片变灰，状态文本保持原样
- 电池供电设备显示电量图标
- 设备卡片点击事件改用事件委托模式

**其他修复**
- 登录成功后重置失败计数
- 启动时所有设备重置为离线状态
