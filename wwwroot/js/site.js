// 日夜切换和实时时间功能
document.addEventListener('DOMContentLoaded', function () {
    // 实时时间更新
    updateDateTime();
    setInterval(updateDateTime, 1000);

    // 自动夜间模式检测
    checkAndApplyAutoNightMode();
    setInterval(checkAndApplyAutoNightMode, 60000);

    // 手动主题切换
    const themeToggle = document.getElementById('themeToggleBtn');
    if (themeToggle) {
        const newToggle = themeToggle.cloneNode(true);
        themeToggle.parentNode.replaceChild(newToggle, themeToggle);

        newToggle.addEventListener('click', function (e) {
            e.preventDefault();
            document.body.classList.toggle('night-mode');
            const isNightMode = document.body.classList.contains('night-mode');
            localStorage.setItem('smartHomeTheme', isNightMode ? 'night' : 'day');
            localStorage.setItem('themeMode', 'manual');
        });
    }

    const savedTheme = localStorage.getItem('smartHomeTheme');
    const themeMode = localStorage.getItem('themeMode');
    if (themeMode === 'manual' && savedTheme === 'night') {
        document.body.classList.add('night-mode');
    }
});

function updateDateTime() {
    const now = new Date();
    const datePart = document.getElementById('datePart');
    const timePart = document.getElementById('timePart');
    if (datePart && timePart) {
        const year = now.getFullYear();
        const month = String(now.getMonth() + 1).padStart(2, '0');
        const day = String(now.getDate()).padStart(2, '0');
        const weekdays = ['周日', '周一', '周二', '周三', '周四', '周五', '周六'];
        const weekday = weekdays[now.getDay()];
        datePart.textContent = `${year}-${month}-${day} ${weekday}`;
        const hours = String(now.getHours()).padStart(2, '0');
        const minutes = String(now.getMinutes()).padStart(2, '0');
        const seconds = String(now.getSeconds()).padStart(2, '0');
        timePart.textContent = `${hours}:${minutes}:${seconds}`;
    }
}

function checkAndApplyAutoNightMode() {
    const now = new Date();
    const hours = now.getHours();
    const isNightTime = hours >= 18 || hours < 6;
    const themeMode = localStorage.getItem('themeMode');
    if (themeMode !== 'manual') {
        if (isNightTime) {
            document.body.classList.add('night-mode');
            localStorage.setItem('smartHomeTheme', 'night');
        } else {
            document.body.classList.remove('night-mode');
            localStorage.setItem('smartHomeTheme', 'day');
        }
        localStorage.setItem('themeMode', 'auto');
    }
}

// ==================== 辅助函数 ====================
function getModeText(mode) {
    switch (mode) {
        case 'cool': return '制冷';
        case 'heat': return '制热';
        case 'fan': return '送风';
        case 'dry': return '除湿';
        case 'auto': return '自动';
        default: return mode || '制冷';
    }
}

function getDirectionText(direction) {
    switch (direction) {
        case 'forward': return '正转';
        case 'reverse': return '反转';
        case 'stop': return '停止';
        default: return '停止';
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ==================== 电池图标更新 ====================
function updateBatteryIcon(element, batteryLevel) {
    if (!element) return;

    let icon = element.querySelector('i');
    if (!icon) {
        icon = document.createElement('i');
        icon.className = 'fas';
        element.innerHTML = '';
        element.appendChild(icon);
    }

    const batteryIcon = batteryLevel >= 75 ? 'fa-battery-full' :
        batteryLevel >= 50 ? 'fa-battery-three-quarters' :
            batteryLevel >= 25 ? 'fa-battery-half' :
                batteryLevel >= 10 ? 'fa-battery-quarter' : 'fa-battery-empty';
    const batteryColor = batteryLevel <= 15 ? '#f44336' :
        batteryLevel <= 30 ? '#ff9800' : '#4caf50';

    icon.className = `fas ${batteryIcon}`;
    icon.style.color = batteryColor;
    icon.title = `电量 ${batteryLevel}%`;
}

// ==================== 设备卡片更新函数 ====================
function updateDeviceCard(card, device) {
    if (!card) return;

    card.dataset.room = device.roomIdentifier;
    card.dataset.type = device.typeIdentifier;
    card.dataset.power = device.powerValue;
    card.dataset.fullId = device.fullDeviceId;

    const statusElement = card.querySelector('.device-status');
    if (statusElement) {
        statusElement.dataset.temperatureValue = device.temperatureValue;
        statusElement.dataset.humidityValue = device.humidityValue;
        statusElement.dataset.batteryLevel = device.batteryLevel;
        statusElement.dataset.acTemperature = device.acTemperature;
        statusElement.dataset.acMode = device.acMode;
        statusElement.dataset.acFanSpeed = device.acFanSpeed;
        statusElement.dataset.fanSpeed = device.fanSpeed;
        statusElement.dataset.motorSpeed = device.motorSpeed;
        statusElement.dataset.motorDirection = device.motorDirection;
        statusElement.dataset.brightness = device.brightness;
        statusElement.dataset.colorTemperature = device.colorTemperature;

        statusElement.dataset.temperature = device.temperature;
        statusElement.dataset.humidity = device.humidity;
        statusElement.dataset.mode = device.mode;
        statusElement.dataset.direction = device.direction;
        statusElement.dataset.speed = device.motorSpeed;

        const isOn = device.isOn && device.statusText !== "离线";

        // 对于摄像头，isOn 表示开关状态（开启/关闭），在线状态单独处理
        if (device.typeIdentifier === 'camera') {
            updateCameraPowerStatus(card, isOn);
        } else {
            updateDevicePowerStatus(card, isOn);
        }

        const statusText = statusElement.querySelector('.status-text');
        if (statusText) {
            if (device.statusText === "离线") {
                statusText.textContent = "离线";
            } else {
                switch (device.typeIdentifier) {
                    case 'temp-sensor':
                        var temp = device.temperatureValue || device.temperature;
                        statusText.textContent = temp ? `温度 ${parseFloat(temp).toFixed(1)}°C` : "--";
                        break;
                    case 'humidity-sensor':
                        var hum = device.humidityValue || device.temperature;
                        statusText.textContent = hum ? `湿度 ${Math.round(hum)}%` : "--";
                        break;
                    case 'light':
                        statusText.textContent = device.isOn ? "开启" : "关闭";
                        break;
                    case 'ac':
                        if (device.isOn) {
                            const modeText = getModeText(device.acMode || device.mode);
                            const temp = device.acTemperature || device.temperature || 24;
                            statusText.textContent = `${modeText} ${temp}°C`;
                        } else {
                            statusText.textContent = "关闭";
                        }
                        break;
                    case 'fan':
                        if (device.isOn) {
                            const speed = device.fanSpeed || device.motorSpeed || 3;
                            statusText.textContent = `风速 ${speed}档`;
                        } else {
                            statusText.textContent = "关闭";
                        }
                        break;
                    case 'lock':
                        statusText.textContent = device.isOn ? "已上锁" : "未上锁";
                        break;
                    case 'camera':
                        // 摄像头：显示"开启"或"关闭"
                        statusText.textContent = device.isOn ? "开启" : "关闭";
                        break;
                    case 'motor':
                        if (device.isOn) {
                            const direction = device.motorDirection || device.direction || "stop";
                            const speed = device.motorSpeed || 0;
                            if (speed > 0) {
                                statusText.textContent = `${getDirectionText(direction)} ${speed}rpm`;
                            } else {
                                statusText.textContent = getDirectionText(direction);
                            }
                        } else {
                            statusText.textContent = "停止";
                        }
                        break;
                    default:
                        statusText.textContent = device.isOn ? "开启" : "关闭";
                        break;
                }
            }
        }
    }

    const detailText = card.querySelector('.device-detail-text');
    if (detailText) {
        if (device.statusText === "离线") {
            detailText.textContent = "设备离线";
        } else {
            switch (device.typeIdentifier) {
                case 'humidity-sensor':
                    detailText.textContent = "湿度传感器 · 在线";
                    break;
                case 'temp-sensor':
                    detailText.textContent = "温度传感器 · 在线";
                    break;
                case 'lock':
                    var battery = device.batteryLevel || device.humidity;
                    detailText.textContent = battery ? `电量 ${battery}%` : device.detail;
                    break;
                case 'camera':
                    detailText.textContent = device.detail || "摄像头 · 在线";
                    break;
                default:
                    detailText.textContent = device.detail;
                    break;
            }
        }
    }

    const powerSpan = card.querySelector('.device-power');
    if (powerSpan) {
        powerSpan.dataset.powerValue = device.powerValue;

        var batteryDevices = ['temp-sensor', 'humidity-sensor', 'lock', 'camera'];
        if (batteryDevices.includes(device.typeIdentifier)) {
            var batteryLevel = device.batteryLevel || device.humidity;
            if (batteryLevel) {
                updateBatteryIcon(powerSpan, batteryLevel);
            }
        } else {
            if (device.powerValue >= 1) {
                powerSpan.textContent = device.powerValue.toFixed(2) + 'kW';
            } else if (device.powerValue > 0) {
                powerSpan.textContent = Math.round(device.powerValue * 1000) + 'W';
            } else {
                powerSpan.textContent = device.power || '0W';
            }
        }
    }

    const progressFill = card.querySelector('.progress-fill');
    if (progressFill) {
        progressFill.style.width = device.progress + '%';
        if (device.progressColor) {
            progressFill.style.background = device.progressColor;
        }
    }

    // 控制设备的离线样式（卡片变灰）
    if (device.typeIdentifier === 'camera') {
        // 摄像头：根据 Detail 字段判断是否在线
        // Detail 为 "摄像头 · 在线" 表示在线，"摄像头 · 离线" 表示离线
        if (device.detail && device.detail.includes('离线')) {
            card.classList.add('offline');
        } else {
            card.classList.remove('offline');
        }
    } else {
        // 其他设备根据 statusText 判断
        if (device.statusText === "离线") {
            card.classList.add('offline');
        } else {
            card.classList.remove('offline');
        }
    }
}

function updateDevicePowerStatus(card, isOn) {
    const statusElement = card.querySelector('.device-status');
    const statusText = statusElement?.querySelector('.status-text');
    const deviceType = card.dataset.type;

    if (statusElement) {
        if (isOn) {
            statusElement.classList.add('on');
            if (!statusElement.querySelector('.fa-circle')) {
                const circle = document.createElement('i');
                circle.className = 'fas fa-circle';
                circle.style.cssText = 'font-size:0.5rem; margin-right:5px; color:#2ecc71;';
                statusElement.insertBefore(circle, statusElement.firstChild);
            }
            const offlineIcon = statusElement.querySelector('.fa-power-off');
            if (offlineIcon) offlineIcon.remove();
        } else {
            statusElement.classList.remove('on');
            const circle = statusElement.querySelector('.fa-circle');
            if (circle) circle.remove();
        }
        statusElement.dataset.isOn = isOn;
    }

    if (statusText) {
        switch (deviceType) {
            case 'lock':
                statusText.textContent = isOn ? "已上锁" : "未上锁";
                break;
            case 'camera':
                statusText.textContent = isOn ? "开启" : "关闭";
                break;
            case 'light':
                statusText.textContent = isOn ? "开启" : "关闭";
                break;
            case 'ac':
                if (isOn) {
                    const mode = statusElement.dataset.acMode || 'cool';
                    const temperature = statusElement.dataset.acTemperature || statusElement.dataset.temperature || '23';
                    const modeText = getModeText(mode);
                    statusText.textContent = `${modeText} ${temperature}°C`;
                } else {
                    statusText.textContent = "关闭";
                }
                break;
            case 'fan':
                if (isOn) {
                    const speed = statusElement.dataset.fanSpeed || statusElement.dataset.speed || '3';
                    statusText.textContent = `风速 ${speed}档`;
                } else {
                    statusText.textContent = "关闭";
                }
                break;
            case 'motor':
                if (isOn) {
                    const direction = statusElement.dataset.motorDirection || 'stop';
                    statusText.textContent = getDirectionText(direction);
                } else {
                    statusText.textContent = "停止";
                }
                break;
            default:
                statusText.textContent = isOn ? "开启" : "关闭";
                break;
        }
    }
}

// 摄像头专用电源状态更新（只更新状态文本，不影响在线状态）
function updateCameraPowerStatus(card, isOn) {
    const statusElement = card.querySelector('.device-status');
    const statusText = statusElement?.querySelector('.status-text');

    if (statusElement) {
        if (isOn) {
            statusElement.classList.add('on');
            if (!statusElement.querySelector('.fa-circle')) {
                const circle = document.createElement('i');
                circle.className = 'fas fa-circle';
                circle.style.cssText = 'font-size:0.5rem; margin-right:5px; color:#2ecc71;';
                statusElement.insertBefore(circle, statusElement.firstChild);
            }
            const offlineIcon = statusElement.querySelector('.fa-power-off');
            if (offlineIcon) offlineIcon.remove();
        } else {
            statusElement.classList.remove('on');
            const circle = statusElement.querySelector('.fa-circle');
            if (circle) circle.remove();
        }
        statusElement.dataset.isOn = isOn;
    }

    if (statusText) {
        statusText.textContent = isOn ? "开启" : "关闭";
    }
}

function updateDeviceOnlineStatus(card, isOnline) {
    const statusElement = card.querySelector('.device-status');
    const statusText = statusElement?.querySelector('.status-text');
    const deviceType = card.dataset.type;

    if (isOnline) {
        // 在线：移除离线样式
        card.classList.remove('offline');
        const deleteIcon = card.querySelector('.delete-device');
        if (deleteIcon) {
            deleteIcon.style.opacity = '1';
            deleteIcon.style.pointerEvents = 'auto';
        }
        const offlineIcon = statusElement?.querySelector('.fa-power-off');
        if (offlineIcon) offlineIcon.remove();

        // 非摄像头设备，在线时状态文本改为"在线"
        if (deviceType !== 'camera') {
            if (statusText && statusText.textContent === "离线") {
                statusText.textContent = "在线";
            }
        }
    } else {
        // 离线：添加离线样式（卡片变灰）
        card.classList.add('offline');
        const deleteIcon = card.querySelector('.delete-device');
        if (deleteIcon) {
            deleteIcon.style.opacity = '0.3';
            deleteIcon.style.pointerEvents = 'none';
        }

        // 摄像头离线时，状态文本保持原样（开启/关闭），但卡片变灰
        if (deviceType !== 'camera') {
            if (statusText && statusText.textContent !== "离线") {
                statusText.textContent = "离线";
            }
        }

        if (statusElement && !statusElement.querySelector('.fa-power-off')) {
            const offlineIcon = document.createElement('i');
            offlineIcon.className = 'fas fa-power-off';
            offlineIcon.style.cssText = 'font-size:0.8rem; margin-right:5px; color:#ff6b6b;';
            statusElement.insertBefore(offlineIcon, statusElement.firstChild);
        }
        const greenDot = statusElement?.querySelector('.fa-circle');
        if (greenDot) greenDot.remove();
    }
}

// ==================== 遥测数据更新函数 ====================
function updateTemperatureDisplay(card, temperature) {
    const statusElement = card.querySelector('.device-status');
    const statusText = statusElement?.querySelector('.status-text');
    const deviceType = card.dataset.type;

    if (statusElement) {
        statusElement.dataset.temperatureValue = temperature;
        statusElement.dataset.temperature = temperature;
    }

    if (statusText && deviceType === 'temp-sensor' && statusElement.classList.contains('on')) {
        statusText.textContent = `温度 ${parseFloat(temperature).toFixed(1)}°C`;
    }
}

function updateHumidityDisplay(card, humidity) {
    const statusElement = card.querySelector('.device-status');
    const statusText = statusElement?.querySelector('.status-text');
    const deviceType = card.dataset.type;

    if (statusElement) {
        statusElement.dataset.humidityValue = humidity;
        statusElement.dataset.temperature = humidity;
    }

    if (statusText && deviceType === 'humidity-sensor' && statusElement.classList.contains('on')) {
        statusText.textContent = `湿度 ${Math.round(humidity)}%`;
    }
}

function updateBatteryLevelDisplay(card, batteryLevel) {
    const powerSpan = card.querySelector('.device-power');
    if (powerSpan) {
        updateBatteryIcon(powerSpan, batteryLevel);
        const statusElement = card.querySelector('.device-status');
        if (statusElement) {
            statusElement.dataset.batteryLevel = batteryLevel;
            statusElement.dataset.humidity = batteryLevel;
        }
    }
}

function updateFanSpeedDisplay(card, speed) {
    const statusElement = card.querySelector('.device-status');
    const statusText = statusElement?.querySelector('.status-text');
    const deviceType = card.dataset.type;

    if (statusElement) {
        statusElement.dataset.fanSpeed = speed;
        statusElement.dataset.speed = speed;
    }

    if (statusText && deviceType === 'fan' && statusElement.classList.contains('on')) {
        statusText.textContent = `风速 ${speed}档`;
    }
}

function updateAcModeDisplay(card, mode) {
    const statusElement = card.querySelector('.device-status');
    const statusText = statusElement?.querySelector('.status-text');

    if (statusElement) {
        statusElement.dataset.acMode = mode;
    }

    if (statusText && card.dataset.type === 'ac' && statusElement.classList.contains('on')) {
        const temp = statusElement.dataset.acTemperature || statusElement.dataset.temperature || 24;
        const modeText = getModeText(mode);
        statusText.textContent = `${modeText} ${temp}°C`;
    }
}

function updateAcTemperatureDisplay(card, temperature) {
    const statusElement = card.querySelector('.device-status');
    const statusText = statusElement?.querySelector('.status-text');

    if (statusElement) {
        statusElement.dataset.acTemperature = temperature;
        statusElement.dataset.temperature = temperature;
    }

    if (statusText && card.dataset.type === 'ac' && statusElement.classList.contains('on')) {
        const mode = statusElement.dataset.acMode || 'cool';
        const modeText = getModeText(mode);
        statusText.textContent = `${modeText} ${Math.round(temperature)}°C`;
    }
}

function updateMotorDirectionDisplay(card, direction) {
    const statusElement = card.querySelector('.device-status');
    const statusText = statusElement?.querySelector('.status-text');

    if (statusElement) {
        statusElement.dataset.motorDirection = direction;
        statusElement.dataset.direction = direction;
    }

    if (statusText && card.dataset.type === 'motor' && statusElement.classList.contains('on')) {
        statusText.textContent = getDirectionText(direction);
    }
}

function updateMotorSpeedDisplay(card, speed) {
    const statusElement = card.querySelector('.device-status');
    const statusText = statusElement?.querySelector('.status-text');
    const deviceType = card.dataset.type;

    if (statusElement) {
        statusElement.dataset.motorSpeed = speed;
        if (deviceType === 'fan') {
            statusElement.dataset.fanSpeed = speed;
            statusElement.dataset.speed = speed;
        }
    }

    if (statusText) {
        if (deviceType === 'fan' && statusElement.classList.contains('on')) {
            statusText.textContent = `风速 ${speed}档`;
        } else if (deviceType === 'motor' && statusElement.classList.contains('on')) {
            const direction = statusElement.dataset.motorDirection || 'stop';
            if (speed > 0) {
                statusText.textContent = `${getDirectionText(direction)} ${speed}rpm`;
            } else {
                statusText.textContent = getDirectionText(direction);
            }
        }
    }
}

function updateBrightnessDisplay(card, brightness) {
    const statusElement = card.querySelector('.device-status');
    if (statusElement) {
        statusElement.dataset.brightness = brightness;
    }

    const progressFill = card.querySelector('.progress-fill');
    if (progressFill && card.dataset.type === 'light') {
        progressFill.style.width = brightness + '%';
    }
}

function updatePowerDisplay(card, power) {
    const powerSpan = card.querySelector('.device-power');
    if (powerSpan && !powerSpan.querySelector('i')) {
        if (power >= 1000) {
            powerSpan.textContent = (power / 1000).toFixed(2) + 'kW';
        } else {
            powerSpan.textContent = Math.round(power) + 'W';
        }
        powerSpan.dataset.powerValue = power / 1000;
    }
}

function updateDeviceFromTelemetry(deviceId, telemetry) {
    const deviceCards = document.querySelectorAll('.device-item');
    deviceCards.forEach(card => {
        const fullId = card.dataset.fullId;
        if (fullId === deviceId) {
            if (telemetry.isOnline !== undefined) {
                updateDeviceOnlineStatus(card, telemetry.isOnline);
            }
            if (telemetry.isOn !== undefined) {
                const deviceType = card.dataset.type;
                if (deviceType === 'camera') {
                    updateCameraPowerStatus(card, telemetry.isOn);
                } else {
                    updateDevicePowerStatus(card, telemetry.isOn);
                }
            }

            if (telemetry.temperatureValue !== undefined) {
                updateTemperatureDisplay(card, telemetry.temperatureValue);
            }
            if (telemetry.humidityValue !== undefined) {
                updateHumidityDisplay(card, telemetry.humidityValue);
            }
            if (telemetry.batteryLevel !== undefined) {
                updateBatteryLevelDisplay(card, telemetry.batteryLevel);
            }
            if (telemetry.speed !== undefined) {
                updateFanSpeedDisplay(card, telemetry.speed);
            }
            if (telemetry.mode !== undefined) {
                updateAcModeDisplay(card, telemetry.mode);
            }
            if (telemetry.temperature !== undefined && card.dataset.type === 'ac') {
                updateAcTemperatureDisplay(card, telemetry.temperature);
            }
            if (telemetry.direction !== undefined) {
                updateMotorDirectionDisplay(card, telemetry.direction);
            }
            if (telemetry.motorSpeed !== undefined) {
                updateMotorSpeedDisplay(card, telemetry.motorSpeed);
            }
            if (telemetry.brightness !== undefined) {
                updateBrightnessDisplay(card, telemetry.brightness);
            }
            if (telemetry.power !== undefined) {
                updatePowerDisplay(card, telemetry.power);
            }

            calculateAverageHumidity();
            calculateAverageRoomTemp();
            calculateTotalPower();
            updateDeviceStats();
        }
    });
}

// ==================== KPI 计算函数 ====================
function calculateAverageRoomTemp() {
    const tempElements = document.querySelectorAll('.device-item[data-type="temp-sensor"] .status-text');
    let totalTemp = 0, count = 0;
    tempElements.forEach(el => {
        const text = el.textContent || '';
        if (text.includes('温度')) {
            const temp = parseFloat(text.replace('温度', '').replace('°C', '').trim());
            if (!isNaN(temp)) {
                totalTemp += temp;
                count++;
            }
        }
    });
    const avgTempElement = document.getElementById('averageRoomTemp');
    if (avgTempElement) {
        avgTempElement.textContent = count > 0 ? (totalTemp / count).toFixed(1) : '--';
    }
}

function calculateAverageHumidity() {
    const humidityElements = document.querySelectorAll('.device-item[data-type="humidity-sensor"] .status-text');
    let totalHumidity = 0, count = 0;
    humidityElements.forEach(el => {
        const text = el.textContent || '';
        if (text.includes('湿度')) {
            const humidity = parseInt(text.replace('湿度', '').replace('%', '').trim());
            if (!isNaN(humidity)) {
                totalHumidity += humidity;
                count++;
            }
        }
    });
    const avgHumidityElement = document.getElementById('averageHumidity');
    if (avgHumidityElement) {
        avgHumidityElement.textContent = count > 0 ? Math.round(totalHumidity / count) : '--';
    }
}

function calculateTotalPower() {
    const deviceItems = document.querySelectorAll('.device-item');
    let totalPowerKW = 0;
    deviceItems.forEach(device => {
        const deviceType = device.dataset.type;
        if (deviceType === 'temp-sensor' || deviceType === 'humidity-sensor') return;
        if (device.classList.contains('offline')) return;

        const powerElement = device.querySelector('.device-power');
        const statusElement = device.querySelector('.device-status');
        const isOn = statusElement && statusElement.classList.contains('on');

        if (isOn && powerElement && !powerElement.querySelector('i')) {
            const powerValue = parseFloat(powerElement.dataset.powerValue);
            if (!isNaN(powerValue)) {
                totalPowerKW += powerValue;
            }
        }
    });
    totalPowerKW = Math.round(totalPowerKW * 100) / 100;

    const powerDisplay = document.getElementById('realTimePower');
    if (powerDisplay) {
        powerDisplay.textContent = totalPowerKW.toFixed(2);
    }

    if (typeof powerHistory !== 'undefined' && powerHistory.length > 0) {
        const lastPower = powerHistory[powerHistory.length - 1];
        const changePercent = lastPower > 0 ? ((totalPowerKW - lastPower) / lastPower * 100).toFixed(1) : 0;
        const trendElement = document.getElementById('powerTrend');
        if (trendElement) {
            if (changePercent > 0) {
                trendElement.innerHTML = `<i class="fas fa-arrow-up"></i> ${changePercent}%`;
                trendElement.style.color = '#f44336';
            } else if (changePercent < 0) {
                trendElement.innerHTML = `<i class="fas fa-arrow-down"></i> ${Math.abs(changePercent)}%`;
                trendElement.style.color = '#4caf50';
            } else {
                trendElement.innerHTML = `<i class="fas fa-minus"></i> 0%`;
                trendElement.style.color = '#1e8f6b';
            }
        }
    }

    if (typeof powerHistory !== 'undefined') {
        powerHistory.push(totalPowerKW);
        if (powerHistory.length > 24) powerHistory.shift();
    }
}

function updateDeviceStats() {
    const onlineDevices = document.querySelectorAll('.device-item:not(.offline)').length;
    const onlineDevicesBadge = document.getElementById('onlineDevicesCount');
    if (onlineDevicesBadge) {
        onlineDevicesBadge.textContent = onlineDevices + ' 台在线';
    }

    const securityDevices = document.querySelectorAll('.device-item[data-type="lock"], .device-item[data-type="camera"]:not(.offline)').length;
    const securityDevicesSpan = document.getElementById('securityDevices');
    if (securityDevicesSpan) {
        securityDevicesSpan.textContent = securityDevices || '0';
    }
}

// ==================== 设备增量更新 ====================
function updateDevicesIncremental(devices) {
    const currentCards = document.querySelectorAll('.device-item');
    const currentDeviceIds = new Set();
    currentCards.forEach(card => currentDeviceIds.add(parseInt(card.dataset.id)));

    const newDevices = devices.filter(d => !currentDeviceIds.has(d.id));
    const removedDeviceIds = Array.from(currentDeviceIds).filter(id => !devices.some(d => d.id === id));

    removedDeviceIds.forEach(id => {
        const card = document.querySelector(`.device-item[data-id="${id}"]`);
        if (card) {
            card.style.transition = 'all 0.3s ease';
            card.style.opacity = '0';
            card.style.transform = 'scale(0.8)';
            setTimeout(() => {
                if (card.parentNode) {
                    card.remove();
                    updateDeviceStats();
                    calculateTotalPower();
                    calculateAverageRoomTemp();
                    calculateAverageHumidity();
                }
            }, 300);
        }
    });

    if (newDevices.length > 0) {
        showNotification(`发现 ${newDevices.length} 个新设备，请刷新页面查看`, 'info');
    }

    devices.forEach(device => {
        const card = document.querySelector(`.device-item[data-id="${device.id}"]`);
        if (card) {
            updateDeviceCard(card, device);
        }
    });

    updateDeviceStats();
    calculateTotalPower();
    calculateAverageRoomTemp();
    calculateAverageHumidity();
}

// ==================== 通知函数 ====================
function showNotification(message, type, duration = 3000) {
    if (!document.getElementById('notification-styles')) {
        const style = document.createElement('style');
        style.id = 'notification-styles';
        style.textContent = `
            .smart-notification{
                position:fixed;
                top:20px;
                right:20px;
                padding:12px 24px;
                color:white;
                border-radius:8px;
                box-shadow:0 4px 12px rgba(0,0,0,0.15);
                z-index:9999;
                opacity:0;
                transform:translateX(100%);
                transition:all 0.3s ease
            }
            .smart-notification.show{
                opacity:1;
                transform:translateX(0)
            }
            .smart-notification.success{
                background:#4caf50
            }
            .smart-notification.error{
                background:#f44336
            }
            .smart-notification.info{
                background:#2196f3
            }
            .smart-notification.warning{
                background:#ff9800
            }
        `;
        document.head.appendChild(style);
    }

    const notification = document.createElement('div');
    notification.className = `smart-notification ${type}`;
    notification.textContent = message;
    document.body.appendChild(notification);

    setTimeout(() => notification.classList.add('show'), 10);
    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => {
            if (notification.parentNode) document.body.removeChild(notification);
        }, 300);
    }, duration);
}

// ==================== 设备卡片点击事件（事件委托版）====================
function initDeviceCardClick() {
    const container = document.getElementById('deviceGrid');
    if (!container) {
        console.warn('deviceGrid 容器未找到，延迟初始化');
        setTimeout(initDeviceCardClick, 500);
        return;
    }

    container.removeEventListener('click', deviceClickHandler);
    container.addEventListener('click', deviceClickHandler);

    console.log('设备卡片点击事件已初始化（事件委托模式）');
}

function deviceClickHandler(e) {
    const card = e.target.closest('.device-item');
    if (!card) return;

    if (e.target.closest('.delete-device')) {
        console.log('点击删除按钮，不打开设置');
        return;
    }

    if (card.classList.contains('offline')) {
        showNotification('设备已离线，无法设置', 'warning');
        return;
    }

    const deviceId = card.dataset.id;
    const deviceNameElem = card.querySelector('.device-name span');
    const deviceName = deviceNameElem ? deviceNameElem.textContent : '未知设备';
    const deviceType = card.dataset.type;

    if (!deviceId) {
        showNotification('无法获取设备信息', 'error');
        return;
    }

    console.log(`打开设备设置: ${deviceName} (ID: ${deviceId}, 类型: ${deviceType})`);
    openDeviceSettings(deviceId, deviceName, deviceType);
}

// ==================== 设备设置功能 ====================
function openDeviceSettings(deviceId, deviceName, deviceType) {
    const modal = document.getElementById('deviceSettingsModal');
    const settingsContent = document.getElementById('settingsContent');
    const deviceNameSpan = document.getElementById('settingsDeviceName');

    if (!modal || !settingsContent || !deviceNameSpan) {
        console.error('模态框元素未找到');
        return;
    }

    deviceNameSpan.textContent = deviceName + ' 设置';
    currentDeviceSettings = { id: deviceId, type: deviceType };

    const deviceCard = document.querySelector(`.device-item[data-id="${deviceId}"]`);
    if (!deviceCard) {
        console.error('未找到设备卡片:', deviceId);
        showNotification('无法获取设备信息', 'error');
        return;
    }

    const statusElement = deviceCard.querySelector('.device-status');
    if (!statusElement) {
        console.error('未找到状态元素');
        return;
    }

    const isOn = statusElement.classList.contains('on');

    let temperature = 0, humidity = 0, speed = 0, mode = 'cool', direction = 'stop', powerValue = 0;

    switch (deviceType) {
        case 'temp-sensor':
            temperature = parseFloat(statusElement.dataset.temperatureValue || statusElement.dataset.temperature || '0');
            break;
        case 'humidity-sensor':
            humidity = parseFloat(statusElement.dataset.humidityValue || statusElement.dataset.temperature || '0');
            break;
        case 'ac':
            temperature = parseFloat(statusElement.dataset.acTemperature || statusElement.dataset.temperature || '24');
            mode = statusElement.dataset.acMode || 'cool';
            break;
        case 'fan':
            speed = parseInt(statusElement.dataset.fanSpeed || statusElement.dataset.speed || '3');
            break;
        case 'motor':
            direction = statusElement.dataset.motorDirection || statusElement.dataset.direction || 'stop';
            speed = parseInt(statusElement.dataset.motorSpeed || '0');
            break;
    }

    const powerSpan = deviceCard.querySelector('.device-power');
    if (powerSpan) {
        powerValue = parseFloat(powerSpan.dataset.powerValue || '0') * 1000;
    }

    let template = null;
    switch (deviceType) {
        case 'light': template = document.getElementById('lightSettingsTemplate'); break;
        case 'ac': template = document.getElementById('acSettingsTemplate'); break;
        case 'lock': template = document.getElementById('lockSettingsTemplate'); break;
        case 'camera': template = document.getElementById('cameraSettingsTemplate'); break;
        case 'fan': template = document.getElementById('fanSettingsTemplate'); break;
        case 'temp-sensor': template = document.getElementById('tempSensorSettingsTemplate'); break;
        case 'humidity-sensor': template = document.getElementById('humiditySensorSettingsTemplate'); break;
        case 'motor': template = document.getElementById('motorSettingsTemplate'); break;
        default:
            settingsContent.innerHTML = '<div style="text-align: center; padding: 40px;"><i class="fas fa-cog" style="font-size: 48px; opacity: 0.3;"></i><br><p>该设备类型暂无设置选项</p></div>';
            modal.style.display = 'flex';
            return;
    }

    if (template) {
        settingsContent.innerHTML = '';
        settingsContent.appendChild(template.content.cloneNode(true));
        initSettingsControls(deviceType, deviceId, isOn, temperature, humidity, speed, mode, direction, powerValue);
    }

    modal.style.display = 'flex';
}

function initSettingsControls(deviceType, deviceId, isOn, temperature, humidity, speed, mode, direction, powerValue) {
    switch (deviceType) {
        case 'light': initLightSettings(deviceId, isOn); break;
        case 'ac': initAcSettings(deviceId, isOn, temperature, mode, powerValue); break;
        case 'lock': initLockSettings(deviceId, isOn); break;
        case 'camera': initCameraSettings(deviceId, isOn); break;
        case 'fan': initFanSettings(deviceId, isOn, speed); break;
        case 'temp-sensor': initTempSensorSettings(temperature); break;
        case 'humidity-sensor': initHumiditySensorSettings(humidity); break;
        case 'motor': initMotorSettings(deviceId, direction); break;
    }
}

function sendDeviceCommand(deviceId, command, parameters, callback) {
    const deviceCard = document.querySelector(`.device-item[data-id="${deviceId}"]`);
    const fullDeviceId = deviceCard?.dataset.fullId;
    const targetId = fullDeviceId || deviceId;

    fetch(`/api/tcp/devices/${targetId}/command`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ command: command, parameters: parameters || {} })
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showNotification(`命令发送成功: ${command}`, 'success');
                if (callback) callback(data);
            } else {
                showNotification(`命令发送失败: ${data.message}`, 'error');
                if (callback) callback(data);
            }
        })
        .catch(error => {
            console.error('发送命令失败:', error);
            showNotification(`发送命令失败: ${error.message}`, 'error');
        });
}

function initLightSettings(deviceId, isOn) {
    const toggle = document.getElementById('lightPowerToggle');
    const status = document.getElementById('lightPowerStatus');
    const brightness = document.getElementById('lightBrightness');
    const brightnessVal = document.getElementById('brightnessValue');

    if (toggle) {
        toggle.classList.toggle('active', isOn);
        toggle.classList.toggle('inactive', !isOn);
        status.textContent = isOn ? '开启' : '关闭';
        toggle.addEventListener('click', function () {
            const newState = !this.classList.contains('active');
            this.classList.toggle('active', newState);
            this.classList.toggle('inactive', !newState);
            status.textContent = newState ? '开启' : '关闭';
            sendDeviceCommand(deviceId, 'set_power', { isOn: newState });
        });
    }
    if (brightness && brightnessVal) {
        brightness.addEventListener('input', function () { brightnessVal.textContent = this.value + '%'; });
        brightness.addEventListener('change', function () { sendDeviceCommand(deviceId, 'set_brightness', { brightness: parseInt(this.value) }); });
    }
}

function initAcSettings(deviceId, isOn, temperature, mode, powerValue) {
    const toggle = document.getElementById('acPowerToggle');
    const status = document.getElementById('acPowerStatus');
    const tempInput = document.getElementById('acTemperature');
    const tempVal = document.getElementById('tempValue');
    const modeSelect = document.getElementById('acMode');
    const fanSpeedSelect = document.getElementById('acFanSpeed');

    if (toggle) {
        toggle.classList.toggle('active', isOn);
        toggle.classList.toggle('inactive', !isOn);
        status.textContent = isOn ? '开启' : '关闭';
        toggle.addEventListener('click', function () {
            const newState = !this.classList.contains('active');
            this.classList.toggle('active', newState);
            this.classList.toggle('inactive', !newState);
            status.textContent = newState ? '开启' : '关闭';
            sendDeviceCommand(deviceId, 'set_power', { isOn: newState });
        });
    }
    if (tempInput && tempVal) {
        tempInput.value = temperature || 24;
        tempVal.textContent = (temperature || 24) + '°C';
        tempInput.addEventListener('input', function () { tempVal.textContent = this.value + '°C'; });
        tempInput.addEventListener('change', function () { sendDeviceCommand(deviceId, 'set_temperature', { temperature: parseFloat(this.value) }); });
    }
    if (modeSelect) {
        modeSelect.value = mode || 'cool';
        modeSelect.addEventListener('change', function () { sendDeviceCommand(deviceId, 'set_mode', { mode: this.value }); });
    }
    if (fanSpeedSelect) {
        fanSpeedSelect.addEventListener('change', function () { sendDeviceCommand(deviceId, 'set_fan_speed', { speed: this.value }); });
    }
}

function initLockSettings(deviceId, isOn) {
    const toggle = document.getElementById('lockToggle');
    const status = document.getElementById('lockStatus');

    if (toggle) {
        toggle.classList.toggle('active', isOn);
        toggle.classList.toggle('inactive', !isOn);
        status.textContent = isOn ? '已上锁' : '未上锁';
        toggle.addEventListener('click', function () {
            const newState = !this.classList.contains('active');
            this.classList.toggle('active', newState);
            this.classList.toggle('inactive', !newState);
            status.textContent = newState ? '已上锁' : '未上锁';
            if (newState) {
                sendDeviceCommand(deviceId, 'lock', {});
            } else {
                const code = prompt('请输入解锁密码：');
                if (code) {
                    sendDeviceCommand(deviceId, 'unlock', { code: code });
                } else {
                    this.classList.toggle('active', !newState);
                    this.classList.toggle('inactive', newState);
                    status.textContent = !newState ? '已上锁' : '未上锁';
                }
            }
        });
    }
}

function initCameraSettings(deviceId, isOn) {
    const toggle = document.getElementById('cameraPowerToggle');
    const status = document.getElementById('cameraPowerStatus');

    if (toggle) {
        toggle.classList.toggle('active', isOn);
        toggle.classList.toggle('inactive', !isOn);
        status.textContent = isOn ? '开启' : '关闭';
        toggle.addEventListener('click', function () {
            const newState = !this.classList.contains('active');
            this.classList.toggle('active', newState);
            this.classList.toggle('inactive', !newState);
            status.textContent = newState ? '开启' : '关闭';
            sendDeviceCommand(deviceId, 'set_power', { isOn: newState });
        });
    }
}

function initFanSettings(deviceId, isOn, speed) {
    const toggle = document.getElementById('fanPowerToggle');
    const status = document.getElementById('fanPowerStatus');
    const speedInput = document.getElementById('fanSpeed');
    const speedVal = document.getElementById('fanSpeedValue');

    if (toggle) {
        toggle.classList.toggle('active', isOn);
        toggle.classList.toggle('inactive', !isOn);
        status.textContent = isOn ? '开启' : '关闭';
        toggle.addEventListener('click', function () {
            const newState = !this.classList.contains('active');
            this.classList.toggle('active', newState);
            this.classList.toggle('inactive', !newState);
            status.textContent = newState ? '开启' : '关闭';
            sendDeviceCommand(deviceId, 'set_power', { isOn: newState });
        });
    }
    if (speedInput && speedVal) {
        speedInput.value = speed || 3;
        speedVal.textContent = (speed || 3) + '档';
        speedInput.addEventListener('input', function () { speedVal.textContent = this.value + '档'; });
        speedInput.addEventListener('change', function () { sendDeviceCommand(deviceId, 'set_speed', { speed: parseInt(this.value) }); });
    }
}

function initTempSensorSettings(temperature) {
    const tempDisplay = document.getElementById('currentTempDisplay');
    if (tempDisplay) {
        tempDisplay.textContent = temperature ? temperature.toFixed(1) : '--';
    }
}

function initHumiditySensorSettings(humidity) {
    const humidityDisplay = document.getElementById('currentHumidityDisplay');
    if (humidityDisplay) {
        humidityDisplay.textContent = humidity || '--';
    }
}

function initMotorSettings(deviceId, direction) {
    const forwardBtn = document.getElementById('motorForward');
    const reverseBtn = document.getElementById('motorReverse');
    const stopBtn = document.getElementById('motorStop');
    const directionDisplay = document.getElementById('motorDirectionDisplay');
    const speedInput = document.getElementById('motorSpeed');
    const speedVal = document.getElementById('motorSpeedValue');

    if (directionDisplay) {
        directionDisplay.textContent = direction === 'forward' ? '正转' : direction === 'reverse' ? '反转' : '停止';
    }
    if (forwardBtn) {
        forwardBtn.addEventListener('click', function () {
            if (directionDisplay) directionDisplay.textContent = '正转';
            sendDeviceCommand(deviceId, 'set_direction', { direction: 'forward' });
        });
    }
    if (reverseBtn) {
        reverseBtn.addEventListener('click', function () {
            if (directionDisplay) directionDisplay.textContent = '反转';
            sendDeviceCommand(deviceId, 'set_direction', { direction: 'reverse' });
        });
    }
    if (stopBtn) {
        stopBtn.addEventListener('click', function () {
            if (directionDisplay) directionDisplay.textContent = '停止';
            sendDeviceCommand(deviceId, 'set_direction', { direction: 'stop' });
        });
    }
    if (speedInput && speedVal) {
        speedInput.addEventListener('input', function () { speedVal.textContent = this.value + ' rpm'; });
        speedInput.addEventListener('change', function () { sendDeviceCommand(deviceId, 'set_speed', { speed: parseInt(this.value) }); });
    }
}

// ==================== 天气功能 ====================
async function getWeatherData() {
    try {
        const ipResponse = await fetch('https://ipapi.co/json/');
        if (!ipResponse.ok) throw new Error('IP定位失败');
        const ipData = await ipResponse.json();
        const city = ipData.city || ipData.region || '未知';
        const latitude = ipData.latitude;
        const longitude = ipData.longitude;

        const cityNameMap = { 'beijing': '北京', 'shanghai': '上海', 'nanchang': '南昌', 'guangzhou': '广州', 'shenzhen': '深圳' };
        let displayCity = city;
        if (city.includes('-')) {
            const parts = city.split('-');
            const lastPart = parts[parts.length - 1].toLowerCase();
            displayCity = cityNameMap[lastPart] || lastPart;
        } else if (cityNameMap[city.toLowerCase()]) {
            displayCity = cityNameMap[city.toLowerCase()];
        }
        document.getElementById('cityName').textContent = displayCity;

        if (latitude && longitude) {
            await getWeatherByCoordinates(latitude, longitude);
        } else {
            await getWeatherByCoordinates(39.9042, 116.4074);
        }
    } catch (error) {
        console.error('获取位置失败:', error);
        document.getElementById('cityName').textContent = '北京';
        await getWeatherByCoordinates(39.9042, 116.4074);
    }
}

async function getWeatherByCoordinates(lat, lon) {
    try {
        const weatherUrl = `https://api.open-meteo.com/v1/forecast?latitude=${lat}&longitude=${lon}&current=temperature_2m,relative_humidity_2m,weathercode&timezone=auto`;
        const weatherResponse = await fetch(weatherUrl);
        if (!weatherResponse.ok) throw new Error('天气API请求失败');
        const weatherData = await weatherResponse.json();

        if (weatherData && weatherData.current) {
            const temperature = Math.round(weatherData.current.temperature_2m);
            const humidity = weatherData.current.relative_humidity_2m;
            const weatherCode = weatherData.current.weathercode;
            const weatherInfo = getWeatherInfo(weatherCode);

            document.getElementById('weatherTemp').textContent = `${temperature}°C`;
            document.getElementById('weatherDesc').textContent = weatherInfo.description;
            document.getElementById('humidityValue').textContent = humidity;
            document.getElementById('weatherIcon').className = `fas ${weatherInfo.icon}`;

            const tempElement = document.getElementById('weatherTemp');
            tempElement.classList.remove('high-temp', 'low-temp');
            if (temperature >= 30) tempElement.classList.add('high-temp');
            else if (temperature <= 10) tempElement.classList.add('low-temp');
        }
    } catch (error) {
        console.error('获取天气数据失败:', error);
    }
}

function getWeatherInfo(code) {
    const weatherMap = {
        0: { icon: 'fa-sun', description: '晴朗' }, 1: { icon: 'fa-sun', description: '晴朗' },
        2: { icon: 'fa-cloud-sun', description: '晴间多云' }, 3: { icon: 'fa-cloud', description: '多云' },
        45: { icon: 'fa-smog', description: '有雾' }, 48: { icon: 'fa-smog', description: '雾凇' },
        51: { icon: 'fa-cloud-rain', description: '毛毛雨' }, 53: { icon: 'fa-cloud-rain', description: '小雨' },
        55: { icon: 'fa-cloud-rain', description: '中雨' }, 61: { icon: 'fa-cloud-rain', description: '小雨' },
        63: { icon: 'fa-cloud-rain', description: '中雨' }, 65: { icon: 'fa-cloud-showers-heavy', description: '大雨' },
        71: { icon: 'fa-snowflake', description: '小雪' }, 73: { icon: 'fa-snowflake', description: '中雪' },
        75: { icon: 'fa-snowflake', description: '大雪' }, 95: { icon: 'fa-bolt', description: '雷雨' }
    };
    return weatherMap[code] || { icon: 'fa-cloud-sun', description: '未知' };
}

// ==================== 其他初始化函数 ====================
function initDeviceTypeSelect() {
    const deviceTypeSelect = document.getElementById('deviceType');
    const sensorTempGroup = document.getElementById('sensorTempGroup');
    const sensorHumidityGroup = document.getElementById('sensorHumidityGroup');
    const motorSpeedGroup = document.getElementById('motorSpeedGroup');
    const batteryLevelGroup = document.getElementById('batteryLevelGroup');

    if (deviceTypeSelect) {
        deviceTypeSelect.addEventListener('change', function () {
            if (sensorTempGroup) sensorTempGroup.style.display = 'none';
            if (sensorHumidityGroup) sensorHumidityGroup.style.display = 'none';
            if (motorSpeedGroup) motorSpeedGroup.style.display = 'none';
            if (batteryLevelGroup) batteryLevelGroup.style.display = 'none';

            if (this.value === 'temp-sensor') {
                if (sensorTempGroup) sensorTempGroup.style.display = 'block';
                if (batteryLevelGroup) batteryLevelGroup.style.display = 'block';
            } else if (this.value === 'humidity-sensor') {
                if (sensorHumidityGroup) sensorHumidityGroup.style.display = 'block';
                if (batteryLevelGroup) batteryLevelGroup.style.display = 'block';
            } else if (this.value === 'motor') {
                if (motorSpeedGroup) motorSpeedGroup.style.display = 'block';
            } else if (this.value === 'lock' || this.value === 'camera') {
                if (batteryLevelGroup) batteryLevelGroup.style.display = 'block';
            }
        });
    }
}

function initRoomTabs() {
    document.querySelectorAll('.room-tab').forEach(tab => {
        tab.addEventListener('click', function () {
            document.querySelectorAll('.room-tab').forEach(t => t.classList.remove('active'));
            this.classList.add('active');
            const room = this.dataset.room;
            document.querySelectorAll('.device-item').forEach(device => {
                device.style.display = (room === 'all' || device.dataset.room === room) ? 'flex' : 'none';
            });
        });
    });
}

function initModal() {
    const addModal = document.getElementById('addDeviceModal');
    const settingsModal = document.getElementById('deviceSettingsModal');
    const logsModal = document.getElementById('logsModal');
    const addBtn = document.getElementById('addDeviceBtn');
    const closeBtns = document.querySelectorAll('.close-modal');

    if (addBtn) addBtn.addEventListener('click', () => addModal.style.display = 'flex');

    closeBtns.forEach(btn => btn.addEventListener('click', () => {
        addModal.style.display = 'none';
        settingsModal.style.display = 'none';
        logsModal.style.display = 'none';
    }));

    window.addEventListener('click', event => {
        if (event.target === addModal) addModal.style.display = 'none';
        if (event.target === settingsModal) settingsModal.style.display = 'none';
        if (event.target === logsModal) logsModal.style.display = 'none';
    });
}

function initSaveDevice() {
    const saveBtn = document.getElementById('saveDeviceBtn');
    if (!saveBtn) return;

    saveBtn.addEventListener('click', function () {
        const form = document.getElementById('addDeviceForm');
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const deviceType = document.getElementById('deviceType').value;
        let icon = 'fa-microchip';
        let temperature = null, humidity = null, motorSpeed = null, batteryLevel = null;

        const selectedType = deviceTypeData.find(t => t.typeId === deviceType);
        if (selectedType) icon = selectedType.icon;

        if (deviceType === 'temp-sensor') {
            temperature = parseFloat(document.getElementById('sensorTemperature').value);
            batteryLevel = parseInt(document.getElementById('batteryLevel')?.value || 100);
        } else if (deviceType === 'humidity-sensor') {
            humidity = parseFloat(document.getElementById('sensorHumidity').value);
            batteryLevel = parseInt(document.getElementById('batteryLevel')?.value || 100);
        } else if (deviceType === 'motor') {
            motorSpeed = parseInt(document.getElementById('motorSpeed').value);
        } else if (deviceType === 'lock' || deviceType === 'camera') {
            batteryLevel = parseInt(document.getElementById('batteryLevel')?.value || 100);
        }

        const deviceData = {
            name: document.getElementById('deviceName').value,
            roomId: document.getElementById('deviceRoom').value,
            typeId: deviceType,
            icon: icon,
            power: document.getElementById('devicePower').value,
            isOn: document.getElementById('deviceStatus').value === 'on',
            progress: parseInt(document.getElementById('deviceProgress').value),
            temperatureValue: temperature,
            humidityValue: humidity,
            batteryLevel: batteryLevel,
            motorSpeed: motorSpeed
        };

        fetch('/api/devices/add', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
            body: JSON.stringify(deviceData)
        })
            .then(async response => {
                const text = await response.text();
                if (!text) throw new Error('服务器返回空响应');
                try { return JSON.parse(text); } catch (e) { throw new Error('服务器返回无效的JSON'); }
            })
            .then(data => {
                if (data.success) {
                    addDeviceLog(document.getElementById('deviceName').value, '添加');
                    showNotification('设备添加成功', 'success');
                    setTimeout(() => location.reload(), 1000);
                } else {
                    showNotification('添加失败: ' + data.message, 'error');
                }
            })
            .catch(error => {
                console.error('添加错误:', error);
                showNotification('添加设备时发生错误', 'error');
            });
    });
}

function initDeleteDevice() {
    document.addEventListener('click', function (e) {
        const deleteIcon = e.target.closest('.delete-device');
        if (!deleteIcon) return;

        e.preventDefault();
        e.stopPropagation();

        const deviceItem = deleteIcon.closest('.device-item');
        if (!deviceItem) return;

        const deviceId = deviceItem.dataset.id;
        const deviceName = deviceItem.querySelector('.device-name span')?.textContent;

        if (!deviceId) {
            showNotification('设备ID无效', 'error');
            return;
        }

        if (confirm(`确定要删除设备 "${deviceName}" 吗？`)) {
            deleteIcon.style.opacity = '0.5';
            deleteIcon.style.cursor = 'wait';
            deleteIcon.classList.remove('fa-trash-alt');
            deleteIcon.classList.add('fa-spinner', 'fa-spin');

            fetch('/api/devices/delete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                body: JSON.stringify({ id: parseInt(deviceId) })
            })
                .then(async response => {
                    const text = await response.text();
                    if (!text) throw new Error('服务器返回空响应');
                    try { return JSON.parse(text); } catch (e) { throw new Error('服务器返回无效的JSON'); }
                })
                .then(data => {
                    if (data.success) {
                        addDeviceLog(deviceName, '删除');
                        deviceItem.style.transition = 'all 0.3s ease';
                        deviceItem.style.opacity = '0';
                        deviceItem.style.transform = 'scale(0.8)';
                        setTimeout(() => {
                            deviceItem.remove();
                            updateDeviceStats();
                            calculateTotalPower();
                            calculateAverageRoomTemp();
                            calculateAverageHumidity();
                            showNotification('设备删除成功', 'success');
                        }, 300);
                    } else {
                        showNotification('删除失败: ' + data.message, 'error');
                        resetDeleteIcon(deleteIcon);
                    }
                })
                .catch(error => {
                    console.error('删除错误:', error);
                    showNotification('删除设备时发生错误', 'error');
                    resetDeleteIcon(deleteIcon);
                });
        }
    });
}

function resetDeleteIcon(deleteIcon) {
    deleteIcon.style.opacity = '1';
    deleteIcon.style.cursor = 'pointer';
    deleteIcon.classList.remove('fa-spinner', 'fa-spin');
    deleteIcon.classList.add('fa-trash-alt');
}

// ==================== SignalR 实时通信 ====================
function initSignalR() {
    signalRConnection = new signalR.HubConnectionBuilder()
        .withUrl("/deviceHub")
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Information)
        .build();

    signalRConnection.on("DevicesUpdated", (devices) => {
        console.log("收到设备列表更新:", devices);
        updateDevicesIncremental(devices);
        updateDeviceStats();
        calculateTotalPower();
        calculateAverageRoomTemp();
        calculateAverageHumidity();
    });

    signalRConnection.on("TelemetryUpdated", (deviceId, telemetry) => {
        console.log(`设备 ${deviceId} 遥测数据更新:`, telemetry);
        updateDeviceFromTelemetry(deviceId, telemetry);
    });

    signalRConnection.start()
        .then(() => {
            console.log("SignalR 连接成功");
            showNotification('实时连接成功', 'success');
            refreshDeviceListFromServer();
        })
        .catch(err => {
            console.error("SignalR 连接失败:", err.toString());
            showNotification('实时连接失败', 'error');
        });
}

async function refreshDeviceListFromServer() {
    try {
        const response = await fetch('/api/devices/list');
        const data = await response.json();
        if (data.success && data.devices) {
            updateDevicesIncremental(data.devices);
        }
    } catch (error) {
        console.error('刷新设备列表失败:', error);
    }
}

// ==================== 自动化场景功能 ====================
function loadAutomationStates() {
    const savedStates = localStorage.getItem('automationStates');
    if (savedStates) {
        const states = JSON.parse(savedStates);
        document.querySelectorAll('.toggle-switch').forEach(toggle => {
            const sceneId = toggle.dataset.sceneId;
            if (states[sceneId] !== undefined) {
                if (states[sceneId]) {
                    toggle.classList.add('active');
                    toggle.classList.remove('inactive');
                } else {
                    toggle.classList.remove('active');
                    toggle.classList.add('inactive');
                }
            }
        });
    }
    updateActiveScenesCount();
}

function saveAutomationStates() {
    const states = {};
    document.querySelectorAll('.toggle-switch').forEach(toggle => {
        states[toggle.dataset.sceneId] = toggle.classList.contains('active');
    });
    localStorage.setItem('automationStates', JSON.stringify(states));
}

function updateActiveScenesCount() {
    const activeScenes = document.querySelectorAll('.toggle-switch.active').length;
    const badge = document.getElementById('activeScenesCount');
    if (badge) badge.textContent = activeScenes + ' 执行中';
}

function initAutomationToggles() {
    document.querySelectorAll('.toggle-switch').forEach(toggle => {
        toggle.addEventListener('click', function (e) {
            e.stopPropagation();
            const sceneItem = this.closest('.scene-item');
            const sceneName = sceneItem?.dataset.sceneName || sceneItem?.querySelector('.scene-left span')?.textContent || '';
            const isActive = this.classList.contains('active');

            if (isActive) {
                this.classList.remove('active');
                this.classList.add('inactive');
            } else {
                this.classList.remove('inactive');
                this.classList.add('active');
            }

            saveAutomationStates();
            updateActiveScenesCount();
            addAutomationLog(sceneName, !isActive ? '启用' : '禁用');
            showNotification(`自动化场景 "${sceneName}" 已${!isActive ? '启用' : '禁用'}`, 'info');
        });
    });
}

// ==================== 日志功能 ====================
function initLogs() {
    loadLogsFromStorage();
    const logsBtn = document.getElementById('logsBtn');
    const logsModal = document.getElementById('logsModal');

    if (logsBtn) {
        logsBtn.addEventListener('click', function () {
            logsModal.style.display = 'flex';
            renderFullLogs();
        });
    }

    document.querySelectorAll('.close-modal').forEach(btn => btn.addEventListener('click', () => logsModal.style.display = 'none'));
    window.addEventListener('click', event => { if (event.target === logsModal) logsModal.style.display = 'none'; });

    const logTypeFilter = document.getElementById('logTypeFilter');
    const logSearch = document.getElementById('logSearch');
    if (logTypeFilter) logTypeFilter.addEventListener('change', renderFullLogs);
    if (logSearch) logSearch.addEventListener('input', debounce(renderFullLogs, 300));

    const clearLogsBtn = document.getElementById('clearLogsBtn');
    if (clearLogsBtn) {
        clearLogsBtn.addEventListener('click', () => {
            if (confirm('确定要清除所有日志吗？')) {
                systemLogs = [];
                saveLogsToStorage();
                renderFullLogs();
                renderRecentLogs();
                showNotification('日志已清除', 'success');
            }
        });
    }

    if (systemLogs.length === 0) addSampleLogs();
    renderRecentLogs();
}

function loadLogsFromStorage() {
    try {
        const savedLogs = localStorage.getItem('systemLogs');
        if (savedLogs) systemLogs = JSON.parse(savedLogs);
    } catch (e) {
        systemLogs = [];
    }
}

function saveLogsToStorage() {
    try {
        localStorage.setItem('systemLogs', JSON.stringify(systemLogs));
    } catch (e) { }
}

function addSampleLogs() {
    const now = new Date();
    const timeStr = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
    systemLogs = [
        { id: Date.now() - 5000, type: 'system', title: '系统启动', description: '智能家居监控系统已启动', time: timeStr, device: '系统', icon: 'fa-power-off' },
        { id: Date.now() - 10000, type: 'device', title: '设备在线', description: '6台设备已连接到系统', time: timeStr, device: '系统', icon: 'fa-wifi' }
    ];
    saveLogsToStorage();
}

function addLog(type, title, description, device = '', icon = '') {
    const now = new Date();
    const timeStr = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}:${String(now.getSeconds()).padStart(2, '0')}`;
    const defaultIcon = type === 'device' ? 'fa-microchip' : type === 'alert' ? 'fa-exclamation-triangle' : type === 'automation' ? 'fa-clock' : 'fa-info-circle';
    const newLog = { id: Date.now(), type, title, description, time: timeStr, date: now.toLocaleDateString(), device: device || '系统', icon: icon || defaultIcon };
    systemLogs.unshift(newLog);
    if (systemLogs.length > 100) systemLogs = systemLogs.slice(0, 100);
    saveLogsToStorage();
    renderRecentLogs();
    const logsModal = document.getElementById('logsModal');
    if (logsModal && logsModal.style.display === 'flex') renderFullLogs();
}

function addDeviceLog(deviceName, action) {
    addLog('device', `设备${action}`, `${deviceName} 已${action}`, deviceName, action === '添加' ? 'fa-plus-circle' : action === '删除' ? 'fa-trash-alt' : 'fa-power-off');
}

function addAutomationLog(sceneName, action) {
    addLog('automation', '自动化场景', `"${sceneName}" 已${action}`, '自动化', 'fa-clock');
}

function renderRecentLogs() {
    const recentLogsList = document.getElementById('recentLogsList');
    if (!recentLogsList) return;

    const recentLogs = systemLogs.slice(0, 4);
    if (recentLogs.length === 0) {
        recentLogsList.innerHTML = '<div class="no-recent-logs">暂无最新动态</div>';
        return;
    }

    let html = '';
    recentLogs.forEach(log => {
        const logType = log.type || 'system';
        const logIcon = log.icon || (logType === 'device' ? 'fa-microchip' : logType === 'automation' ? 'fa-clock' : logType === 'alert' ? 'fa-exclamation-triangle' : 'fa-info-circle');
        html += `<div class="recent-log-item">
            <div class="recent-log-icon ${logType}">
                <i class="fas ${logIcon}"></i>
            </div>
            <div class="recent-log-content">
                <div class="recent-log-title">${log.title || '未知事件'}</div>
                <div class="recent-log-description">${log.description || ''}</div>
                <div class="recent-log-time"><i class="far fa-clock"></i> ${log.time || '刚刚'}</div>
            </div>
        </div>`;
    });
    recentLogsList.innerHTML = html;
}

function renderFullLogs() {
    const logsList = document.getElementById('logsList');
    const typeFilter = document.getElementById('logTypeFilter')?.value || 'all';
    const searchText = document.getElementById('logSearch')?.value.toLowerCase() || '';
    if (!logsList) return;

    let filteredLogs = systemLogs;
    if (typeFilter !== 'all') filteredLogs = filteredLogs.filter(l => l.type === typeFilter);
    if (searchText) filteredLogs = filteredLogs.filter(l => (l.title && l.title.toLowerCase().includes(searchText)) || (l.description && l.description.toLowerCase().includes(searchText)) || (l.device && l.device.toLowerCase().includes(searchText)));

    if (filteredLogs.length === 0) {
        logsList.innerHTML = '<div class="no-logs"><i class="fas fa-inbox" style="font-size:48px;margin-bottom:10px;opacity:0.3;"></i><br>暂无日志记录</div>';
        return;
    }

    let html = '';
    filteredLogs.forEach(log => {
        const logType = log.type || 'system';
        const logIcon = log.icon || (logType === 'device' ? 'fa-microchip' : logType === 'automation' ? 'fa-clock' : logType === 'alert' ? 'fa-exclamation-triangle' : 'fa-info-circle');
        html += `<div class="log-item">
            <div class="log-icon ${logType}">
                <i class="fas ${logIcon}"></i>
            </div>
            <div class="log-content">
                <div style="display:flex;align-items:center;flex-wrap:wrap;gap:10px;">
                    <span class="log-title">${log.title || '未知事件'}</span>
                    <span class="log-device"><i class="fas ${logIcon}" style="margin-right:4px;"></i>${log.device || '系统'}</span>
                </div>
                <div style="display:flex;justify-content:space-between;align-items:center;">
                    <span>${log.description || ''}</span>
                    <span class="log-time"><i class="far fa-clock"></i> ${log.time || '刚刚'}</span>
                </div>
            </div>
        </div>`;
    });
    logsList.innerHTML = html;
}

function debounce(func, wait) {
    let timeout;
    return function (...args) {
        clearTimeout(timeout);
        timeout = setTimeout(() => func(...args), wait);
    };
}

// ==================== AI助手功能 ====================
function initAIAssistant() {
    const aiInput = document.getElementById('aiCommandInput');
    const aiSendBtn = document.getElementById('aiCommandSend');
    const voiceBtn = document.getElementById('voiceAssistantBtn');
    const responseArea = document.getElementById('aiResponseArea');
    const responseContent = document.getElementById('aiResponseContent');
    const responseClose = document.querySelector('.ai-response-close');

    function showResponseArea() { responseArea.style.display = 'block'; }
    function hideResponseArea() { responseArea.style.display = 'none'; }
    if (responseClose) responseClose.addEventListener('click', hideResponseArea);

    function showThinking() { showResponseArea(); responseContent.innerHTML = '<div class="ai-thinking"><span class="ai-loading-spinner"></span> AI助手正在思考...</div>'; }
    function showResponse(text) { showResponseArea(); const formattedText = text.replace(/\n/g, '<br>'); responseContent.innerHTML = `<div style="white-space: pre-wrap;">${escapeHtml(formattedText)}</div>`; responseContent.scrollTop = responseContent.scrollHeight; }
    function showError(errorMsg) { showResponseArea(); responseContent.innerHTML = `<div style="color: #f44336;"><i class="fas fa-exclamation-circle"></i> ${escapeHtml(errorMsg)}</div>`; }

    async function sendMessageToAI(message) {
        if (!message.trim()) return;
        showThinking();
        try {
            const response = await fetch('/api/AIAssistant/chat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ message: message.trim() })
            });
            const data = await response.json();
            if (data.success) {
                showResponse(data.response);
                addLog('system', 'AI对话', `用户: ${message.substring(0, 50)}...`, 'AI助手', 'fa-robot');
            } else {
                showError(data.message || '处理失败，请稍后再试');
            }
        } catch (error) {
            console.error('AI对话错误:', error);
            showError('网络连接失败，请检查网络后重试');
        }
    }

    if (aiSendBtn && aiInput) {
        aiSendBtn.addEventListener('click', function () { sendMessageToAI(aiInput.value); aiInput.value = ''; });
        aiInput.addEventListener('keypress', function (e) { if (e.key === 'Enter') { e.preventDefault(); sendMessageToAI(aiInput.value); aiInput.value = ''; } });
    }

    if (voiceBtn && 'webkitSpeechRecognition' in window) {
        const recognition = new webkitSpeechRecognition();
        recognition.continuous = false;
        recognition.interimResults = false;
        recognition.lang = 'zh-CN';
        let isListening = false;

        voiceBtn.addEventListener('click', function () {
            if (isListening) {
                recognition.stop();
                voiceBtn.classList.remove('voice-listening');
                isListening = false;
                return;
            }
            try {
                recognition.start();
                voiceBtn.classList.add('voice-listening');
                isListening = true;
                showResponseArea();
                responseContent.innerHTML = '<div class="ai-thinking"><i class="fas fa-microphone"></i> 正在聆听，请说话...</div>';
            } catch (e) {
                console.error('语音识别启动失败:', e);
                showError('语音识别启动失败，请检查麦克风权限');
            }
        });

        recognition.onresult = function (event) {
            const transcript = event.results[0][0].transcript;
            if (aiInput) aiInput.value = transcript;
            sendMessageToAI(transcript);
            aiInput.value = '';
            voiceBtn.classList.remove('voice-listening');
            isListening = false;
        };

        recognition.onerror = function (event) {
            console.error('语音识别错误:', event.error);
            voiceBtn.classList.remove('voice-listening');
            isListening = false;
            if (event.error !== 'no-speech') {
                showError('语音识别失败: ' + event.error);
            } else {
                responseContent.innerHTML = '<div class="ai-thinking"><i class="fas fa-microphone-slash"></i> 未检测到语音，请重试</div>';
                setTimeout(() => { if (responseContent.innerHTML.includes('未检测到语音')) hideResponseArea(); }, 2000);
            }
        };

        recognition.onend = function () { voiceBtn.classList.remove('voice-listening'); isListening = false; };
    } else if (voiceBtn) {
        voiceBtn.style.opacity = '0.5';
        voiceBtn.title = '您的浏览器不支持语音识别';
        voiceBtn.addEventListener('click', function () { showError('您的浏览器不支持语音识别功能，请手动输入指令'); });
    }

    function addSuggestionsToResponse() {
        const suggestions = [
            { text: '💡 打开客厅灯光', command: '打开客厅灯光' },
            { text: '❄️ 关闭客厅空调', command: '关闭客厅空调' },
            { text: '📊 查看设备状态', command: '设备状态' },
            { text: '🏠 查看房间状态', command: '房间状态' },
            { text: '⚡ 查看能耗', command: '能耗' },
            { text: '📋 查看场景列表', command: '场景列表' },
            { text: '❓ 帮助', command: '帮助' }
        ];

        const suggestionsHtml = `<div class="suggestions-tags">${suggestions.map(s => `<span class="suggestion-tag" data-command="${escapeHtml(s.command)}">${escapeHtml(s.text)}</span>`).join('')}</div>`;

        const observer = new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                if (mutation.type === 'childList' && responseContent.children.length === 1) {
                    const content = responseContent.innerHTML;
                    if (!content.includes('suggestions-tags') && !content.includes('ai-thinking')) {
                        responseContent.insertAdjacentHTML('beforeend', suggestionsHtml);
                        document.querySelectorAll('.suggestion-tag').forEach(tag => {
                            tag.addEventListener('click', function () {
                                const command = this.dataset.command;
                                if (aiInput) aiInput.value = command;
                                sendMessageToAI(command);
                                aiInput.value = '';
                            });
                        });
                    }
                }
            });
        });
        observer.observe(responseContent, { childList: true });

        if (!responseContent.innerHTML.includes('suggestions-tags')) {
            responseContent.insertAdjacentHTML('beforeend', suggestionsHtml);
            document.querySelectorAll('.suggestion-tag').forEach(tag => {
                tag.addEventListener('click', function () {
                    const command = this.dataset.command;
                    if (aiInput) aiInput.value = command;
                    sendMessageToAI(command);
                    aiInput.value = '';
                });
            });
        }
    }
    addSuggestionsToResponse();

    document.addEventListener('click', function (e) {
        if (responseArea.style.display === 'block') {
            if (!responseArea.contains(e.target) && !aiInput?.contains(e.target) && !aiSendBtn?.contains(e.target) && !voiceBtn?.contains(e.target)) {
                setTimeout(() => {
                    if (responseArea.style.display === 'block' && !responseArea.matches(':hover') && !aiInput?.matches(':focus')) {
                        hideResponseArea();
                    }
                }, 300);
            }
        }
    });
}

// ==================== 全局变量 ====================
let systemLogs = [];
let powerHistory = [];
let currentDeviceSettings = null;
let signalRConnection = null;
let roomData = [];
let deviceTypeData = [];

// ==================== 页面初始化入口 ====================
document.addEventListener('DOMContentLoaded', function () {
    // 获取数据
    if (typeof roomData === 'undefined') roomData = [];
    if (typeof deviceTypeData === 'undefined') deviceTypeData = [];

    getWeatherData();
    loadAutomationStates();
    calculateTotalPower();
    calculateAverageRoomTemp();
    calculateAverageHumidity();
    updateDateTime();
    setInterval(updateDateTime, 1000);
    setInterval(() => {
        calculateTotalPower();
        calculateAverageRoomTemp();
        calculateAverageHumidity();
    }, 30000);

    initRoomTabs();
    initModal();
    initSaveDevice();
    initDeleteDevice();
    initLogs();
    initAutomationToggles();
    initDeviceCardClick();
    initDeviceTypeSelect();
    initSignalR();
    initAIAssistant();
    updateDeviceStats();
});

// 导出全局函数
window.showNotification = showNotification;
window.updateDeviceFromTelemetry = updateDeviceFromTelemetry;
window.updateDevicesIncremental = updateDevicesIncremental;
window.calculateTotalPower = calculateTotalPower;
window.calculateAverageRoomTemp = calculateAverageRoomTemp;
window.calculateAverageHumidity = calculateAverageHumidity;
window.updateDeviceStats = updateDeviceStats;
window.getModeText = getModeText;
window.getDirectionText = getDirectionText;
window.updateBatteryIcon = updateBatteryIcon;