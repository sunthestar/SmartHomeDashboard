using Microsoft.Extensions.Hosting;

namespace SmartHomeDashboard.Services
{
    public class SceneSchedulerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SceneSchedulerService> _logger;
        private Timer? _timer;

        public SceneSchedulerService(IServiceProvider serviceProvider, ILogger<SceneSchedulerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("场景定时调度服务已启动");
            // 缩短检查间隔到3秒，提高响应速度
            _timer = new Timer(CheckScenes, null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
            return Task.CompletedTask;
        }

        private async void CheckScenes(object? state)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sceneService = scope.ServiceProvider.GetRequiredService<SceneService>();

                // 检查定时场景
                await sceneService.CheckAndExecuteScheduledScenesAsync();

                // 检查条件场景
                await sceneService.CheckAndExecuteConditionScenesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查场景失败");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("场景定时调度服务正在停止");
            _timer?.Change(Timeout.Infinite, 0);
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}