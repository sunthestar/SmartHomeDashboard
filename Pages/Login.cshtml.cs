using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SmartHomeDashboard.Services;

namespace SmartHomeDashboard.Pages
{
    public class LoginModel : PageModel
    {
        private readonly LoginService _loginService;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(LoginService loginService, ILogger<LoginModel> logger)
        {
            _loginService = loginService;
            _logger = logger;
        }

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public bool IsLocked { get; set; }
        public int? RemainingAttempts { get; set; }

        public async Task<IActionResult> OnGet()
        {
            // 如果已经登录，直接跳转到首页
            if (HttpContext.Session.GetString("IsLoggedIn") == "true")
            {
                return RedirectToPage("/Index");
            }

            // 获取登录状态信息
            var settings = await _loginService.GetLoginSettingsAsync();
            if (settings != null)
            {
                if (settings.LockUntil.HasValue && settings.LockUntil > DateTime.Now)
                {
                    IsLocked = true;
                    var minutesLeft = (int)(settings.LockUntil.Value - DateTime.Now).TotalMinutes;
                    ErrorMessage = $"账户已锁定，请 {minutesLeft} 分钟后再试";
                }
                else
                {
                    RemainingAttempts = 5 - (settings.FailCount % 5);
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "请输入密码";
                return Page();
            }

            // 验证密码
            var isValid = await _loginService.ValidatePasswordAsync(Password);

            if (isValid)
            {
                // 密码正确，设置登录状态
                HttpContext.Session.SetString("IsLoggedIn", "true");
                HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                // 获取客户端IP
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "未知";
                HttpContext.Session.SetString("LoginIp", ip);

                _logger.LogInformation($"登录成功，IP: {ip}");

                return RedirectToPage("/Index");
            }
            else
            {
                // 密码错误
                var settings = await _loginService.GetLoginSettingsAsync();
                if (settings != null)
                {
                    if (settings.LockUntil.HasValue && settings.LockUntil > DateTime.Now)
                    {
                        var minutesLeft = (int)(settings.LockUntil.Value - DateTime.Now).TotalMinutes;
                        ErrorMessage = $"密码错误，账户已锁定，请 {minutesLeft} 分钟后再试";
                        IsLocked = true;
                    }
                    else
                    {
                        var attemptsLeft = 5 - (settings.FailCount % 5);
                        ErrorMessage = $"密码错误，还剩 {attemptsLeft} 次尝试机会";
                        RemainingAttempts = attemptsLeft;
                    }
                }
                else
                {
                    ErrorMessage = "密码错误";
                }

                return Page();
            }
        }

        public async Task<IActionResult> OnGetLogout()
        {
            // 清除登录状态
            HttpContext.Session.Remove("IsLoggedIn");
            HttpContext.Session.Remove("LoginTime");
            HttpContext.Session.Remove("LoginIp");

            // 重置失败计数（可选）
            await _loginService.ResetFailCountAsync();

            return RedirectToPage("/Login");
        }
    }
}