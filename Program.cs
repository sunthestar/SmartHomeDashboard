using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using SmartHomeDashboard.Services;
using SmartHomeDashboard.Hubs;
using SmartHomeDashboard.Middleware;
using SmartHomeDashboard.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 确保 App_Data 目录存在
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
if (!Directory.Exists(dataDir))
{
    Directory.CreateDirectory(dataDir);
}

// 添加数据库上下文
builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    var dbPath = Path.Combine(dataDir, "smarthome.db");
    options.UseSqlite($"Data Source={dbPath}");
    options.EnableSensitiveDataLogging();
});

// 添加 Session 服务
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".SmartHome.Session";
});

// 添加 Razor Pages 和 控制器
builder.Services.AddRazorPages();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// 添加 SignalR
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// 注册服务
builder.Services.AddSingleton<DeviceDataService>();
builder.Services.AddSingleton<TcpServerService>();
builder.Services.AddSingleton<TcpDeviceService>();
builder.Services.AddSingleton<RoomService>();
builder.Services.AddSingleton<LoginService>();
builder.Services.AddSingleton<SceneService>();
builder.Services.AddSingleton<SystemLogService>();
builder.Services.AddSingleton<TcpConnectionService>();
builder.Services.AddSingleton<AIAssistantService>();
builder.Services.AddHostedService<SceneSchedulerService>();

// 注册后台服务
try
{
    builder.Services.AddHostedService(sp => sp.GetRequiredService<TcpServerService>());
}
catch (Exception ex)
{
    Console.WriteLine($"TCP服务器服务注册失败: {ex.Message}");
}

var app = builder.Build();

// 确保数据库已创建
using (var scope = app.Services.CreateScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var context = dbContextFactory.CreateDbContext();
    context.Database.EnsureCreated();

    Console.WriteLine($"数据库路径: {context.DbPath}");
}

// ========== 启动时重置所有设备为离线状态 ==========
using (var scope = app.Services.CreateScope())
{
    try
    {
        var deviceDataService = scope.ServiceProvider.GetRequiredService<DeviceDataService>();
        await deviceDataService.ResetAllDevicesToOfflineAsync();
        Console.WriteLine("已将所有设备重置为离线状态");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"重置设备状态失败: {ex.Message}");
    }
}
// ================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapHub<DeviceHub>("/deviceHub");
app.MapRazorPages();
app.MapControllers();

// 健康检查端点
app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTime.Now }));

// TCP调试端点
app.MapGet("/api/tcp/debug", async (TcpServerService tcpService, DeviceDataService deviceService) =>
{
    var tcpDevices = tcpService.GetAllDevices();
    var dbDevices = deviceService.GetAllDevices();

    var result = new
    {
        tcpConnectionCount = tcpDevices.Count,
        tcpDevices = tcpDevices.Select(d => new
        {
            d.DeviceId,
            d.DeviceName,
            d.IpAddress,
            d.LastSeen,
            d.IsOnline
        }),
        dbDeviceCount = dbDevices.Count,
        dbDevices = dbDevices.Select(d => new
        {
            d.Id,
            d.FullDeviceId,
            d.Name,
            d.IsOn,
            d.StatusText,
            d.UpdatedAt
        }),
        inconsistency = tcpDevices.Select(t => t.DeviceId)
            .Except(dbDevices.Where(d => d.IsOn && d.StatusText != "离线").Select(d => d.FullDeviceId))
            .Concat(dbDevices.Where(d => d.IsOn && d.StatusText != "离线").Select(d => d.FullDeviceId)
            .Except(tcpDevices.Select(t => t.DeviceId)))
    };

    return Results.Ok(result);
});

app.MapGet("/api/tcp/status", (TcpServerService tcpService) =>
{
    var devices = tcpService.GetAllDevices();
    return Results.Ok(new
    {
        success = true,
        isRunning = true,
        onlineDevices = devices.Count,
        totalDevices = devices.Count,
        devices = devices.Select(d => new
        {
            d.DeviceId,
            d.DeviceName,
            d.IpAddress,
            d.LastSeen,
            d.IsOnline
        })
    });
});

app.Run();