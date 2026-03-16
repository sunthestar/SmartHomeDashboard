// 日夜切换和实时时间功能
document.addEventListener('DOMContentLoaded', function () {
    // 实时时间更新
    updateDateTime();
    setInterval(updateDateTime, 1000);

    // 自动夜间模式检测
    checkAndApplyAutoNightMode();
    // 每分钟检查一次时间，自动切换主题
    setInterval(checkAndApplyAutoNightMode, 60000);

    // 手动主题切换
    const themeToggle = document.getElementById('themeToggleBtn');
    if (themeToggle) {
        themeToggle.addEventListener('click', function () {
            document.body.classList.toggle('night-mode');

            // 保存用户手动偏好，并标记为手动模式
            const isNightMode = document.body.classList.contains('night-mode');
            localStorage.setItem('smartHomeTheme', isNightMode ? 'night' : 'day');
            localStorage.setItem('themeMode', 'manual'); // 标记为手动模式
        });

        // 恢复上次的主题选择，但只在非自动模式下生效
        const savedTheme = localStorage.getItem('smartHomeTheme');
        const themeMode = localStorage.getItem('themeMode');

        // 如果没有手动模式标记，或者标记为自动，则使用自动检测
        if (themeMode !== 'manual') {
            // 自动模式，不需要恢复保存的主题
            localStorage.removeItem('smartHomeTheme');
        } else if (savedTheme === 'night') {
            document.body.classList.add('night-mode');
        }
    }
});

// 更新时间显示
function updateDateTime() {
    const now = new Date();
    const datePart = document.getElementById('datePart');
    const timePart = document.getElementById('timePart');

    if (datePart && timePart) {
        // 格式化日期：YYYY-MM-DD 星期
        const year = now.getFullYear();
        const month = String(now.getMonth() + 1).padStart(2, '0');
        const day = String(now.getDate()).padStart(2, '0');
        const weekdays = ['周日', '周一', '周二', '周三', '周四', '周五', '周六'];
        const weekday = weekdays[now.getDay()];

        datePart.textContent = `${year}-${month}-${day} ${weekday}`;

        // 格式化时间：HH:MM:SS
        const hours = String(now.getHours()).padStart(2, '0');
        const minutes = String(now.getMinutes()).padStart(2, '0');
        const seconds = String(now.getSeconds()).padStart(2, '0');

        timePart.textContent = `${hours}:${minutes}:${seconds}`;
    }
}

// 检查时间并自动应用夜间模式 (18:00 - 06:00)
function checkAndApplyAutoNightMode() {
    const now = new Date();
    const hours = now.getHours();

    // 夜间模式时间：18:00 - 06:00 (晚上6点到早上6点)
    const isNightTime = hours >= 18 || hours < 6;

    // 获取当前主题模式
    const themeMode = localStorage.getItem('themeMode');

    // 只有在非手动模式下才自动切换
    if (themeMode !== 'manual') {
        if (isNightTime) {
            document.body.classList.add('night-mode');
            // 可选：保存当前状态，但不标记为手动
            localStorage.setItem('smartHomeTheme', 'night');
        } else {
            document.body.classList.remove('night-mode');
            localStorage.setItem('smartHomeTheme', 'day');
        }
        localStorage.setItem('themeMode', 'auto');
    }
}