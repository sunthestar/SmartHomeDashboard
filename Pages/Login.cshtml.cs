using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmartHomeDashboard.Pages
{
    public class LoginModel : PageModel
    {
        // 静态密码定义 - 可以在这里修改密码
        private const string StaticPassword = "123456"; // 默认密码，可修改

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet()
        {
            // 如果已经登录，直接跳转到首页
            if (HttpContext.Session.GetString("IsLoggedIn") == "true")
            {
                return RedirectToPage("/Index");
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            if (string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "请输入密码";
                return Page();
            }

            // 验证密码
            if (Password == StaticPassword)
            {
                // 密码正确，设置登录状态
                HttpContext.Session.SetString("IsLoggedIn", "true");

                // 可选：记录登录时间
                HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                return RedirectToPage("/Index");
            }
            else
            {
                ErrorMessage = "密码错误，请重新输入";
                return Page();
            }
        }

        public IActionResult OnGetLogout()
        {
            // 清除登录状态
            HttpContext.Session.Remove("IsLoggedIn");
            return RedirectToPage("/Login");
        }
    }
}