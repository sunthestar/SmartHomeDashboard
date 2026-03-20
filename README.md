### 项目概述

智能家居后台监控系统是一个基于ASP.NET Core的Web应用程序，用于集中管理和监控智能家居设备。系统支持实时设备状态监控、设备控制、自动化场景管理、能耗分析等功能。

### 技术栈

- **后端框架**: ASP.NET Core 8.0
- **前端技术**: Razor Pages, JavaScript, SignalR
- **数据库**: SQLite (Entity Framework Core)
- **通信协议**: TCP (设备连接)
- **实时通信**: SignalR WebSocket

### 项目结构

```
SmartHomeDashboard/
├── App_Data/                          # 数据存储目录
│   ├── smarthome.db                    # SQLite数据库文件
│   └── devices.json (v0.0.1-0.0.3)     # 旧版JSON数据文件(已废弃)
│
├── Controllers/                         # API控制器
│   ├── ApiController.cs                  # 设备管理API
│   ├── SceneController.cs                 # 自动化场景API
│   └── TcpController.cs                   # TCP设备控制API
│
├── Data/                                 # 数据库上下文
│   └── AppDbContext.cs                    # EF Core数据库上下文
│
├── Hubs/                                 # SignalR通信中心
│   └── DeviceHub.cs                        # 设备实时通信Hub
│
├── Middleware/                           # 中间件
│   └── LoginCheckMiddleware.cs             # 登录状态检查中间件
│
├── Models/                                # 数据模型
│   ├── DeviceModel.cs                       # 设备相关模型(房间/设备类型/设备/TCP连接)
│   ├── LoginModels.cs                        # 登录设置模型(单用户无用户名)
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
│   ├── DatabaseInitializerService.cs           # 数据库初始化服务
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
│   │   └── site.css                            # 全局样式
│   └── js/
│       └── site.js                               # 全局脚本
│
├── Program.cs                                 # 应用程序入口
├── appsettings.json                            # 应用程序配置
└── SmartHomeDashboard.csproj                    # 项目文件
```

### 数据库设计 (7张表)

#### 房间信息表 (Rooms)

| 编号 | 字段含义 | 字段名称 | 数据类型 | 长度 | 主键 | 外键 |
|------|----------|----------|----------|------|------|------|
| 1 | 主键 | Id | int | 4 | √ | |
| 2 | 房间标识符 | RoomId | varchar | 50 | | |
| 3 | 房间名称 | RoomName | varchar | 50 | | |
| 4 | 房间描述 | Description | varchar | 200 | | |
| 5 | 设备数量 | DeviceCount | int | 4 | | |
| 6 | 在线数量 | OnlineCount | int | 4 | | |
| 7 | 创建时间 | CreatedAt | datetime | 8 | | |
| 8 | 更新时间 | UpdatedAt | datetime | 8 | | |

#### 设备类型表 (DeviceTypes)

| 编号 | 字段含义 | 字段名称 | 数据类型 | 长度 | 主键 | 外键 |
|------|----------|----------|----------|------|------|------|
| 1 | 主键 | Id | int | 4 | √ | |
| 2 | 类型标识符 | TypeId | varchar | 50 | | |
| 3 | 类型名称 | TypeName | varchar | 50 | | |
| 4 | 图标 | Icon | varchar | 50 | | |
| 5 | 类型描述 | Description | varchar | 200 | | |
| 6 | 创建时间 | CreatedAt | datetime | 8 | | |

#### 设备信息表 (Devices)

| 编号 | 字段含义 | 字段名称 | 数据类型 | 长度 | 主键 | 外键 |
|------|----------|----------|----------|------|------|------|
| 1 | 主键 | Id | int | 4 | √ | |
| 2 | 设备名称 | Name | varchar | 100 | | |
| 3 | 设备编号 | DeviceNumber | varchar | 20 | | |
| 4 | 完整设备ID | FullDeviceId | varchar | 100 | | |
| 5 | 房间ID | RoomId | int | 4 | | √ |
| 6 | 设备类型ID | DeviceTypeId | int | 4 | | √ |
| 7 | 房间标识符 | RoomIdentifier | varchar | 50 | | |
| 8 | 类型标识符 | TypeIdentifier | varchar | 50 | | |
| 9 | 图标 | Icon | varchar | 50 | | |
| 10 | 开关状态 | IsOn | bit | 1 | | |
| 11 | 状态文本 | StatusText | varchar | 100 | | |
| 12 | 详情描述 | Detail | varchar | 200 | | |
| 13 | 功率显示 | Power | varchar | 20 | | |
| 14 | 功率数值 | PowerValue | decimal | 8 | | |
| 15 | 进度百分比 | Progress | int | 4 | | |
| 16 | 进度条颜色 | ProgressColor | varchar | 20 | | |
| 17 | 温度值 | Temperature | decimal | 8 | | |
| 18 | 湿度/电量 | Humidity | int | 4 | | |
| 19 | 电机转速 | MotorSpeed | int | 4 | | |
| 20 | 空调模式 | Mode | varchar | 20 | | |
| 21 | 上下扫风 | SwingVertical | bit | 1 | | |
| 22 | 左右扫风 | SwingHorizontal | bit | 1 | | |
| 23 | 灯光控制 | Light | bit | 1 | | |
| 24 | 静音模式 | Quiet | bit | 1 | | |
| 25 | 电机方向 | Direction | varchar | 20 | | |
| 26 | 创建时间 | CreatedAt | datetime | 8 | | |
| 27 | 更新时间 | UpdatedAt | datetime | 8 | | |

#### TCP连接表 (TcpConnections)

| 编号 | 字段含义 | 字段名称 | 数据类型 | 长度 | 主键 | 外键 |
|------|----------|----------|----------|------|------|------|
| 1 | 主键 | Id | int | 4 | √ | |
| 2 | 设备ID | DeviceId | int | 4 | | √ |
| 3 | 完整设备标识 | FullDeviceId | varchar | 100 | | |
| 4 | 设备名称 | DeviceName | varchar | 100 | | |
| 5 | 设备类型 | DeviceType | varchar | 50 | | |
| 6 | IP地址 | IpAddress | varchar | 50 | | |
| 7 | 端口号 | Port | int | 4 | | |
| 8 | 连接时间 | ConnectedTime | datetime | 8 | | |
| 9 | 最后心跳时间 | LastHeartbeat | datetime | 8 | | |
| 10 | 最后在线时间 | LastSeen | datetime | 8 | | |
| 11 | 在线状态 | IsOnline | bit | 1 | | |
| 12 | 超时次数 | TimeoutCount | int | 4 | | |
| 13 | 创建时间 | CreatedAt | datetime | 8 | | |
| 14 | 更新时间 | UpdatedAt | datetime | 8 | | |

#### 系统日志表 (SystemLogs)

| 编号 | 字段含义 | 字段名称 | 数据类型 | 长度 | 主键 | 外键 |
|------|----------|----------|----------|------|------|------|
| 1 | 主键 | Id | int | 4 | √ | |
| 2 | 日志时间 | Timestamp | datetime | 8 | | |
| 3 | 日志类型 | LogType | varchar | 50 | | |
| 4 | 日志级别 | LogLevel | varchar | 20 | | |
| 5 | 日志标题 | Title | varchar | 200 | | |
| 6 | 日志内容 | Content | text | - | | |
| 7 | 设备ID | DeviceId | int | 4 | | √ |
| 8 | 设备名称 | DeviceName | varchar | 100 | | |
| 9 | 操作类型 | ActionType | varchar | 50 | | |
| 10 | 操作详情 | ActionDetail | varchar | 500 | | |
| 11 | 是否已读 | IsRead | bit | 1 | | |

#### 自动化场景表 (Scenes)

| 编号 | 字段含义 | 字段名称 | 数据类型 | 长度 | 主键 | 外键 |
|------|----------|----------|----------|------|------|------|
| 1 | 主键 | Id | int | 4 | √ | |
| 2 | 场景名称 | SceneName | varchar | 100 | | |
| 3 | 场景图标 | Icon | varchar | 50 | | |
| 4 | 场景描述 | Description | varchar | 200 | | |
| 5 | 触发类型 | TriggerType | varchar | 50 | | |
| 6 | 触发条件 | TriggerCondition | text | - | | |
| 7 | 执行动作 | Actions | text | - | | |
| 8 | 执行时间 | ExecuteTime | varchar | 50 | | |
| 9 | 重复周期 | RepeatDays | varchar | 50 | | |
| 10 | 是否启用 | IsActive | bit | 1 | | |
| 11 | 执行次数 | ExecuteCount | int | 4 | | |
| 12 | 最后执行时间 | LastExecuteTime | datetime | 8 | | |
| 13 | 创建时间 | CreatedAt | datetime | 8 | | |
| 14 | 更新时间 | UpdatedAt | datetime | 8 | | |

#### 登录设置表 (LoginSettings) - 单用户无用户名模式

| 编号 | 字段含义 | 字段名称 | 数据类型 | 长度 | 主键 | 外键 |
|------|----------|----------|----------|------|------|------|
| 1 | 主键 | Id | int | 4 | √ | |
| 2 | 密码 | Password | varchar | 200 | | |
| 3 | 密码盐值 | PasswordSalt | varchar | 50 | | |
| 4 | 是否启用 | IsEnabled | bit | 1 | | |
| 5 | 最后登录时间 | LastLoginTime | datetime | 8 | | |
| 6 | 最后登录IP | LastLoginIp | varchar | 50 | | |
| 7 | 登录次数 | LoginCount | int | 4 | | |
| 8 | 失败次数 | FailCount | int | 4 | | |
| 9 | 最后失败时间 | LastFailTime | datetime | 8 | | |
| 10 | 锁定截止时间 | LockUntil | datetime | 8 | | |
| 11 | 创建时间 | CreatedAt | datetime | 8 | | |
| 12 | 更新时间 | UpdatedAt | datetime | 8 | | |

### 表关系图

```
┌─────────────┐       ┌─────────────┐       ┌─────────────┐
│   Rooms     │       │ DeviceTypes │       │LoginSettings│
│  (房间表)   │       │ (设备类型表)│       │ (登录设置表)│
└──────┬──────┘       └──────┬──────┘       └─────────────┘
       │                     │                      
       │                     │                      
       ↓                     ↓                      
    ┌─────────────────────────────────┐             
    │            Devices               │             
    │           (设备表)                │             
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

### 核心功能模块

#### 设备管理
- **设备发现**：TCP设备自动注册
- **设备控制**：远程控制各类智能设备
- **状态监控**：实时监控设备在线状态和数据
- **设备分组**：按房间和类型分类管理

#### 自动化场景
- **定时场景**：按预设时间自动执行
- **手动场景**：用户手动触发
- **场景管理**：创建、编辑、启用/禁用场景

#### 系统安全
- **单用户登录**：无用户名，仅密码验证
- **登录保护**：5次失败后锁定30分钟
- **会话管理**：8小时会话超时

#### 数据持久化
- **SQLite数据库**：所有数据持久化存储
- **实时同步**：TCP数据实时写入数据库
- **日志记录**：所有操作记录到系统日志

### 更新记录

| 版本 | 日期 | 主要更新 |
|------|------|----------|
| Version 0.0.1 | 2026-03-16 | 基础UI、天气功能、日夜切换、设备类型、JSON存储、日志功能 |
| Version 0.0.2 | 2026-03-17 | 登录UI、登录/登出功能 |
| Version 0.0.3 | 2026-03-19 | IP定位逻辑优化 |
| Version 0.0.4| 2026-03-20 | 数据库架构修改、优化Readme文件 |
