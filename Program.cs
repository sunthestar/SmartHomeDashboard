using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartHomeDashboard.Services;
using SmartHomeDashboard.Hubs;
using SmartHomeDashboard.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 添加 Session 服务
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8); // 设置会话超时时间
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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// 使用 Session
app.UseSession();

// 使用登录检查中间件
app.UseMiddleware<LoginCheckMiddleware>();

app.UseAuthorization();

// 映射 SignalR Hub
app.MapHub<DeviceHub>("/deviceHub");

app.MapRazorPages();
app.MapControllers();

// 添加一个简单的健康检查端点
app.MapGet("/health", () => Results.Ok(new { status = "healthy", time = DateTime.Now }));

app.Run();