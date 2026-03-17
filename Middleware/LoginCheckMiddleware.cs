using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SmartHomeDashboard.Middleware
{
    public class LoginCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public LoginCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 允许访问登录页面、静态资源、API和健康检查
            var path = context.Request.Path.ToString().ToLower();

            if (path == "/login" ||
                path.StartsWith("/css/") ||
                path.StartsWith("/js/") ||
                path.StartsWith("/lib/") ||
                path.StartsWith("/api/") ||
                path == "/health" ||
                path == "/devicehub" ||
                path.StartsWith("/_framework") ||
                path.StartsWith("/_content"))
            {
                await _next(context);
                return;
            }

            // 检查登录状态
            var isLoggedIn = context.Session.GetString("IsLoggedIn") == "true";

            if (!isLoggedIn && !path.StartsWith("/login"))
            {
                context.Response.Redirect("/Login");
                return;
            }

            await _next(context);
        }
    }
}