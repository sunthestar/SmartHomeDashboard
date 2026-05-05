using Microsoft.EntityFrameworkCore;
using SmartHomeDashboard.Data;
using SmartHomeDashboard.Models;
using System.Security.Cryptography;
using System.Text;

namespace SmartHomeDashboard.Services
{
    public class LoginService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly ILogger<LoginService> _logger;

        public LoginService(IDbContextFactory<AppDbContext> dbContextFactory, ILogger<LoginService> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        // 验证密码
        public async Task<bool> ValidatePasswordAsync(string password)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                // 获取登录设置（只有一条记录，ID=1）
                var loginSettings = await context.LoginSettings.FindAsync(1);

                if (loginSettings == null || !loginSettings.IsEnabled)
                {
                    _logger.LogWarning("登录设置不存在或已禁用");
                    return false;
                }

                // 检查是否被锁定
                if (loginSettings.LockUntil.HasValue && loginSettings.LockUntil > DateTime.Now)
                {
                    var minutesLeft = (int)(loginSettings.LockUntil.Value - DateTime.Now).TotalMinutes;
                    _logger.LogWarning($"账户已被锁定，剩余 {minutesLeft} 分钟");
                    return false;
                }

                // 验证密码（简单版，实际应用需要加密）
                bool isValid = password == loginSettings.Password;

                if (isValid)
                {
                    // 登录成功，重置失败计数和锁定状态
                    loginSettings.LastLoginTime = DateTime.Now;
                    loginSettings.LoginCount++;
                    loginSettings.FailCount = 0;  // 重置失败计数
                    loginSettings.LockUntil = null;  // 解锁账户
                    loginSettings.UpdatedAt = DateTime.Now;

                    _logger.LogInformation($"登录成功，总登录次数: {loginSettings.LoginCount}，失败计数已重置");
                }
                else
                {
                    // 登录失败，更新失败计数
                    loginSettings.FailCount++;
                    loginSettings.LastFailTime = DateTime.Now;

                    // 连续失败5次，锁定30分钟
                    if (loginSettings.FailCount >= 5)
                    {
                        loginSettings.LockUntil = DateTime.Now.AddMinutes(30);
                        _logger.LogWarning($"连续失败 {loginSettings.FailCount} 次，账户已锁定30分钟");
                    }
                    else
                    {
                        _logger.LogWarning($"登录失败，失败次数: {loginSettings.FailCount}/5");
                    }

                    loginSettings.UpdatedAt = DateTime.Now;
                }

                await context.SaveChangesAsync();
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证密码失败");
                return false;
            }
        }

        // 更新密码
        public async Task<bool> UpdatePasswordAsync(string oldPassword, string newPassword)
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();

                var loginSettings = await context.LoginSettings.FindAsync(1);
                if (loginSettings == null)
                {
                    return false;
                }

                // 验证旧密码
                if (loginSettings.Password != oldPassword)
                {
                    _logger.LogWarning("旧密码错误");
                    return false;
                }

                // 更新密码
                loginSettings.Password = newPassword;
                loginSettings.UpdatedAt = DateTime.Now;
                loginSettings.FailCount = 0;
                loginSettings.LockUntil = null;

                await context.SaveChangesAsync();
                _logger.LogInformation("密码更新成功");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新密码失败");
                return false;
            }
        }

        // 获取登录设置
        public async Task<LoginSettingsModel?> GetLoginSettingsAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                return await context.LoginSettings.FindAsync(1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取登录设置失败");
                return null;
            }
        }

        // 重置失败计数
        public async Task ResetFailCountAsync()
        {
            try
            {
                using var context = await _dbContextFactory.CreateDbContextAsync();
                var loginSettings = await context.LoginSettings.FindAsync(1);
                if (loginSettings != null)
                {
                    loginSettings.FailCount = 0;
                    loginSettings.LockUntil = null;
                    loginSettings.UpdatedAt = DateTime.Now;
                    await context.SaveChangesAsync();
                    _logger.LogInformation("失败计数已重置");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置失败计数失败");
            }
        }

        // 加密密码（预留，当前版本使用明文）
        private string HashPassword(string password, string salt)
        {
            using var sha256 = SHA256.Create();
            var saltedPassword = password + salt;
            var bytes = Encoding.UTF8.GetBytes(saltedPassword);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // 生成盐值
        private string GenerateSalt()
        {
            var bytes = new byte[16];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}