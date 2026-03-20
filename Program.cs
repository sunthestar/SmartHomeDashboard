using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using SmartHomeDashboard.Services;
using SmartHomeDashboard.Hubs;
using SmartHomeDashboard.Middleware;
using SmartHomeDashboard.Data;

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
    options.EnableSensitiveDataLogging(); // 开发环境可以启用
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
builder.Services.AddControllers();

// 添加 SignalR
builder.Services.AddSignalR();

// 注册服务
builder.Services.AddSingleton<DeviceDataService>();
builder.Services.AddSingleton<TcpServerService>();
builder.Services.AddSingleton<TcpDeviceService>();
builder.Services.AddSingleton<RoomService>();
builder.Services.AddSingleton<LoginService>();
builder.Services.AddSingleton<SceneService>();
builder.Services.AddSingleton<SystemLogService>();
builder.Services.AddSingleton<TcpConnectionService>();

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

// 数据库信息检查端点
app.MapGet("/db-info", async (IDbContextFactory<AppDbContext> dbContextFactory) =>
{
    using var context = await dbContextFactory.CreateDbContextAsync();
    var tables = new List<string>();

    using var command = context.Database.GetDbConnection().CreateCommand();
    command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;";
    await context.Database.OpenConnectionAsync();

    using var result = await command.ExecuteReaderAsync();
    while (await result.ReadAsync())
    {
        tables.Add(result.GetString(0));
    }

    var roomCount = await context.Rooms.CountAsync();
    var deviceTypeCount = await context.DeviceTypes.CountAsync();
    var deviceCount = await context.Devices.CountAsync();
    var sceneCount = await context.Scenes.CountAsync();
    var logCount = await context.SystemLogs.CountAsync();
    var connectionCount = await context.TcpConnections.CountAsync();

    return Results.Ok(new
    {
        databasePath = context.DbPath,
        databaseExists = File.Exists(context.DbPath),
        tables = tables,
        roomCount = roomCount,
        deviceTypeCount = deviceTypeCount,
        deviceCount = deviceCount,
        sceneCount = sceneCount,
        logCount = logCount,
        connectionCount = connectionCount
    });
});

app.Run();