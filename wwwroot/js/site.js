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
    if (!text) return '';
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
        statusElement.dataset.fanSpeed = device.fanSpeed;
        statusElement.dataset.motorSpeed = device.motorSpeed;
        statusElement.dataset.motorDirection = device.motorDirection;
        statusElement.dataset.brightness = device.brightness;

        statusElement.dataset.temperature = device.temperature;
        statusElement.dataset.humidity = device.humidity;
        statusElement.dataset.mode = device.mode;
        statusElement.dataset.direction = device.direction;
        statusElement.dataset.speed = device.motorSpeed;

        // 判断设备是否在线：不是离线状态
        const isOnline = device.statusText !== "离线";
        const isOn = device.isOn && isOnline;

        updateDevicePowerStatus(card, isOn);
        updateDeviceOnlineStatus(card, isOnline);

        const statusText = statusElement.querySelector('.status-text');
        if (statusText) {
            if (!isOnline) {
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
                    var battery = device.batteryLevel || device.humidity;
                    if (battery) {
                        detailText.textContent = `湿度传感器 · 电量 ${battery}%`;
                    } else {
                        detailText.textContent = "湿度传感器 · 在线";
                    }
                    break;
                case 'temp-sensor':
                    var battery = device.batteryLevel || device.humidity;
                    if (battery) {
                        detailText.textContent = `温度传感器 · 电量 ${battery}%`;
                    } else {
                        detailText.textContent = "温度传感器 · 在线";
                    }
                    break;
                case 'lock':
                    var battery = device.batteryLevel || device.humidity;
                    if (battery) {
                        detailText.textContent = `电量 ${battery}%`;
                    } else {
                        detailText.textContent = device.detail;
                    }
                    break;
                case 'camera':
                    detailText.textContent = "摄像头 · 在线";
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

    if (device.statusText === "离线") {
        card.classList.add('offline');
    } else {
        card.classList.remove('offline');
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

function updateDeviceOnlineStatus(card, isOnline) {
    const statusElement = card.querySelector('.device-status');
    const statusText = statusElement?.querySelector('.status-text');
    const deviceType = card.dataset.type;

    if (isOnline) {
        card.classList.remove('offline');
        const deleteIcon = card.querySelector('.delete-device');
        if (deleteIcon) {
            deleteIcon.style.opacity = '1';
            deleteIcon.style.pointerEvents = 'auto';
            deleteIcon.style.cursor = 'pointer';
        }
        const offlineIcon = statusElement?.querySelector('.fa-power-off');
        if (offlineIcon) offlineIcon.remove();

        if (deviceType !== 'camera') {
            if (statusText && statusText.textContent === "离线") {
                if (deviceType === 'light') {
                    statusText.textContent = "关闭";
                } else {
                    statusText.textContent = "在线";
                }
            }
        }
    } else {
        card.classList.add('offline');
        const deleteIcon = card.querySelector('.delete-device');
        if (deleteIcon) {
            deleteIcon.style.opacity = '0.7';
            deleteIcon.style.pointerEvents = 'auto';
            deleteIcon.style.cursor = 'pointer';
        }

        if (statusText && statusText.textContent !== "离线") {
            statusText.textContent = "离线";
        }

        if (statusElement && !statusElement.querySelector('.fa-power-off')) {
            const offlineIcon = document.createElement('i');
            offlineIcon.className = 'fas fa-power-off';
            offlineIcon.style.cssText = 'font-size:0.8rem; margin-right:5px; color:#ff6b6b;';
            statusElement.insertBefore(offlineIcon, statusElement.firstChild);
        }
        const greenDot = statusElement?.querySelector('.fa-circle');
        if (greenDot) greenDot.remove();

        statusElement?.classList.remove('on');
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

function updateDeviceDetailBattery(card, batteryLevel) {
    const detailText = card.querySelector('.device-detail-text');
    const deviceType = card.dataset.type;
    if (!detailText) return;

    let currentText = detailText.textContent;

    switch (deviceType) {
        case 'temp-sensor':
            if (currentText.includes('温度传感器')) {
                if (currentText.includes('电量')) {
                    detailText.textContent = currentText.replace(/电量 \d+%/, `电量 ${batteryLevel}%`);
                } else {
                    detailText.textContent = `温度传感器 · 电量 ${batteryLevel}%`;
                }
            }
            break;
        case 'humidity-sensor':
            if (currentText.includes('湿度传感器')) {
                if (currentText.includes('电量')) {
                    detailText.textContent = currentText.replace(/电量 \d+%/, `电量 ${batteryLevel}%`);
                } else {
                    detailText.textContent = `湿度传感器 · 电量 ${batteryLevel}%`;
                }
            }
            break;
        case 'lock':
            if (currentText.includes('电量')) {
                detailText.textContent = currentText.replace(/电量 \d+%/, `电量 ${batteryLevel}%`);
            } else {
                detailText.textContent = `电量 ${batteryLevel}%`;
            }
            break;
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
                const isOnline = telemetry.isOnline !== undefined ? telemetry.isOnline : !card.classList.contains('offline');
                if (isOnline) {
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
                updateDeviceDetailBattery(card, telemetry.batteryLevel);
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

    if (powerHistory.length > 0) {
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
    powerHistory.push(totalPowerKW);
    if (powerHistory.length > 24) powerHistory.shift();
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

// ==================== 创建设备卡片HTML ====================
function createDeviceCardHtml(device) {
    const isOffline = device.statusText === "离线";
    const isOn = device.isOn && !isOffline;
    const deviceType = device.typeIdentifier;

    let statusTextValue = "";
    if (isOffline) {
        statusTextValue = "离线";
    } else {
        switch (deviceType) {
            case 'temp-sensor':
                var temp = device.temperatureValue || device.temperature;
                statusTextValue = temp ? `温度 ${parseFloat(temp).toFixed(1)}°C` : "--";
                break;
            case 'humidity-sensor':
                var hum = device.humidityValue || device.temperature;
                statusTextValue = hum ? `湿度 ${Math.round(hum)}%` : "--";
                break;
            case 'light':
                statusTextValue = isOn ? "开启" : "关闭";
                break;
            case 'ac':
                if (isOn) {
                    const modeText = getModeText(device.acMode || device.mode);
                    const temp = device.acTemperature || device.temperature || 24;
                    statusTextValue = `${modeText} ${temp}°C`;
                } else {
                    statusTextValue = "关闭";
                }
                break;
            case 'fan':
                if (isOn) {
                    const speed = device.fanSpeed || device.motorSpeed || 3;
                    statusTextValue = `风速 ${speed}档`;
                } else {
                    statusTextValue = "关闭";
                }
                break;
            case 'lock':
                statusTextValue = isOn ? "已上锁" : "未上锁";
                break;
            case 'camera':
                statusTextValue = isOn ? "开启" : "关闭";
                break;
            case 'motor':
                if (isOn) {
                    const direction = device.motorDirection || device.direction || "stop";
                    const speed = device.motorSpeed || 0;
                    if (speed > 0) {
                        statusTextValue = `${getDirectionText(direction)} ${speed}rpm`;
                    } else {
                        statusTextValue = getDirectionText(direction);
                    }
                } else {
                    statusTextValue = "停止";
                }
                break;
            default:
                statusTextValue = isOn ? "开启" : "关闭";
                break;
        }
    }

    let detailText = "";
    if (isOffline) {
        detailText = "设备离线";
    } else {
        switch (deviceType) {
            case 'lock':
                var battery = device.batteryLevel || device.humidity;
                detailText = battery ? `电量 ${battery}%` : (device.detail || "");
                break;
            case 'camera':
                detailText = "摄像头 · 在线";
                break;
            case 'temp-sensor':
                var battery = device.batteryLevel || device.humidity;
                detailText = battery ? `温度传感器 · 电量 ${battery}%` : "温度传感器 · 在线";
                break;
            case 'humidity-sensor':
                var battery = device.batteryLevel || device.humidity;
                detailText = battery ? `湿度传感器 · 电量 ${battery}%` : "湿度传感器 · 在线";
                break;
            default:
                detailText = device.detail || "";
                break;
        }
    }

    let powerHtml = "";
    const batteryDevices = ['temp-sensor', 'humidity-sensor', 'lock', 'camera'];
    if (batteryDevices.includes(deviceType)) {
        var batteryLevel = device.batteryLevel || device.humidity;
        if (batteryLevel) {
            const batteryIcon = batteryLevel >= 75 ? 'fa-battery-full' :
                batteryLevel >= 50 ? 'fa-battery-three-quarters' :
                    batteryLevel >= 25 ? 'fa-battery-half' :
                        batteryLevel >= 10 ? 'fa-battery-quarter' : 'fa-battery-empty';
            const batteryColor = batteryLevel <= 15 ? '#f44336' :
                batteryLevel <= 30 ? '#ff9800' : '#4caf50';
            powerHtml = `<i class="fas ${batteryIcon}" style="color: ${batteryColor};" title="电量 ${batteryLevel}%"></i>`;
        } else {
            powerHtml = device.power || '0W';
        }
    } else {
        if (device.powerValue >= 1) {
            powerHtml = device.powerValue.toFixed(2) + 'kW';
        } else if (device.powerValue > 0) {
            powerHtml = Math.round(device.powerValue * 1000) + 'W';
        } else {
            powerHtml = device.power || '0W';
        }
    }

    const statusIcon = (isOn && !isOffline) ? '<i class="fas fa-circle" style="font-size:0.5rem; margin-right:5px; color:#2ecc71;"></i>' : '';
    const offlineIcon = isOffline ? '<i class="fas fa-power-off" style="font-size:0.8rem; margin-right:5px; color:#ff6b6b;"></i>' : '';

    return `
        <div class="device-item ${isOffline ? 'offline' : ''}" 
             data-id="${device.id}"
             data-full-id="${device.fullDeviceId}"
             data-room="${device.roomIdentifier}"
             data-type="${device.typeIdentifier}"
             data-power="${device.powerValue}"
             style="cursor: pointer;"
             title="点击设置">
            <div class="device-row">
                <div class="device-name">
                    <i class="fas ${device.icon}"></i>
                    <span>${escapeHtml(device.name)}</span>
                    <span class="device-number-badge" style="font-size:0.7rem; color:#666; margin-left:5px;">#${device.deviceNumber}</span>
                </div>
                <div style="display: flex; align-items: center; gap: 8px; flex-wrap: wrap;">
                    <div class="device-status ${(isOn && !isOffline) ? 'on' : ''}">
                        ${statusIcon}
                        ${offlineIcon}
                        <span class="status-text">${escapeHtml(statusTextValue)}</span>
                    </div>
                    <i class="fas fa-trash-alt delete-device" style="color: #ff6b6b; cursor: pointer; font-size: 1rem;" title="删除设备"></i>
                </div>
            </div>
            <div class="device-detail">
                <span class="device-detail-text">${escapeHtml(detailText)}</span>
                <span class="device-power" data-power-value="${device.powerValue}">${powerHtml}</span>
            </div>
            <div class="progress-bg">
                <div class="progress-fill" style="width: ${device.progress}%;${device.progressColor ? ' background:' + device.progressColor : ''}"></div>
            </div>
        </div>
    `;
}

// ==================== 设备增量更新 ====================
function updateDevicesIncremental(devices) {
    const currentCards = document.querySelectorAll('.device-item');
    const currentDeviceIds = new Set();
    currentCards.forEach(card => currentDeviceIds.add(parseInt(card.dataset.id)));

    const newDevices = devices.filter(d => !currentDeviceIds.has(d.id));
    const removedDeviceIds = Array.from(currentDeviceIds).filter(id => !devices.some(d => d.id === id));
    const updatedDevices = devices.filter(d => currentDeviceIds.has(d.id));

    // 处理删除的设备
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
                    showNotification(`设备已删除`, 'info');
                }
            }, 300);
        }
    });

    // 处理新增的设备 - 动态添加到页面
    if (newDevices.length > 0) {
        console.log(`发现 ${newDevices.length} 个新设备，动态添加中...`);
        const deviceGrid = document.getElementById('deviceGrid');
        if (deviceGrid) {
            newDevices.forEach(device => {
                if (document.querySelector(`.device-item[data-id="${device.id}"]`)) {
                    return;
                }
                const deviceCard = createDeviceCardHtml(device);
                if (deviceCard) {
                    deviceGrid.insertAdjacentHTML('beforeend', deviceCard);
                    const newCard = deviceGrid.lastElementChild;
                    if (newCard) {
                        newCard.style.opacity = '0';
                        newCard.style.transform = 'scale(0.8)';
                        setTimeout(() => {
                            newCard.style.transition = 'all 0.3s ease';
                            newCard.style.opacity = '1';
                            newCard.style.transform = 'scale(1)';
                        }, 10);
                    }
                }
            });
        }
        showNotification(`发现 ${newDevices.length} 个新设备，已自动添加`, 'success');
    }

    // 处理更新的设备
    if (updatedDevices.length > 0) {
        let statusChangedCount = 0;
        updatedDevices.forEach(device => {
            const card = document.querySelector(`.device-item[data-id="${device.id}"]`);
            if (card) {
                const oldStatus = card.querySelector('.status-text')?.textContent;
                updateDeviceCard(card, device);
                const newStatus = card.querySelector('.status-text')?.textContent;
                if (oldStatus !== newStatus) {
                    statusChangedCount++;
                }
            }
        });
        if (statusChangedCount > 0) {
            console.log(`${statusChangedCount} 个设备状态已更新`);
        }
    }

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
                z-index:10000;
                opacity:0;
                transform:translateX(100%);
                transition:all 0.3s ease;
                font-size:0.9rem;
                max-width:350px;
                word-wrap:break-word;
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

    // 移除旧的通知（避免堆积）
    const oldNotifications = document.querySelectorAll('.smart-notification');
    oldNotifications.forEach(notif => {
        if (notif.parentNode) {
            notif.parentNode.removeChild(notif);
        }
    });

    const notification = document.createElement('div');
    notification.className = `smart-notification ${type}`;
    notification.innerHTML = message;
    document.body.appendChild(notification);

    // 强制重绘
    notification.offsetHeight;

    setTimeout(() => notification.classList.add('show'), 10);
    setTimeout(() => {
        notification.classList.remove('show');
        setTimeout(() => {
            if (notification.parentNode) {
                document.body.removeChild(notification);
            }
        }, 300);
    }, duration);
}

// ==================== 设备卡片点击事件 ====================
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

    // 离线设备只能删除，不能打开设置详情
    if (card.classList.contains('offline')) {
        showNotification('设备已离线，无法设置，如需删除请点击删除按钮', 'warning');
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
    let batteryLevel = null;

    const powerSpan = deviceCard.querySelector('.device-power');
    if (powerSpan) {
        const batteryIcon = powerSpan.querySelector('i');
        if (batteryIcon && batteryIcon.title) {
            const match = batteryIcon.title.match(/电量 (\d+)%/);
            if (match) {
                batteryLevel = parseInt(match[1]);
            }
        }
    }
    if (batteryLevel === null && statusElement) {
        batteryLevel = parseInt(statusElement.dataset.batteryLevel || statusElement.dataset.humidity || '0');
    }

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
        initSettingsControls(deviceType, deviceId, isOn, temperature, humidity, speed, mode, direction, powerValue, batteryLevel);
    }

    modal.style.display = 'flex';
}

function initSettingsControls(deviceType, deviceId, isOn, temperature, humidity, speed, mode, direction, powerValue, batteryLevel) {
    switch (deviceType) {
        case 'light': initLightSettings(deviceId, isOn); break;
        case 'ac': initAcSettings(deviceId, isOn, temperature, mode, powerValue); break;
        case 'lock': initLockSettings(deviceId, isOn, batteryLevel); break;
        case 'camera': initCameraSettings(deviceId, isOn, batteryLevel); break;
        case 'fan': initFanSettings(deviceId, isOn, speed); break;
        case 'temp-sensor': initTempSensorSettings(temperature, batteryLevel); break;
        case 'humidity-sensor': initHumiditySensorSettings(humidity, batteryLevel); break;
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

function initLockSettings(deviceId, isOn, batteryLevel) {
    const toggle = document.getElementById('lockToggle');
    const status = document.getElementById('lockStatus');

    const batteryFill = document.getElementById('lockBatteryFill');
    const batteryText = document.getElementById('lockBatteryLevel');
    if (batteryFill && batteryText && batteryLevel !== undefined && batteryLevel !== null && batteryLevel > 0) {
        const level = Math.min(100, Math.max(0, batteryLevel));
        batteryFill.style.width = level + '%';
        batteryFill.style.background = level <= 15 ? '#f44336' : level <= 30 ? '#ff9800' : '#4caf50';
        batteryText.textContent = level + '%';
    }

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

    const autoLock = document.getElementById('lockAutoLock');
    if (autoLock) {
        autoLock.addEventListener('change', function () {
            sendDeviceCommand(deviceId, 'set_auto_lock', { seconds: parseInt(this.value) });
        });
    }

    const generateBtn = document.getElementById('generateTempPwd');
    if (generateBtn) {
        generateBtn.addEventListener('click', function () {
            sendDeviceCommand(deviceId, 'generate_temp_password', {}, (r) => {
                if (r.success) showNotification('临时密码已生成', 'info');
            });
        });
    }
}

function initCameraSettings(deviceId, isOn, batteryLevel) {
    const toggle = document.getElementById('cameraPowerToggle');
    const status = document.getElementById('cameraPowerStatus');

    const batteryFill = document.getElementById('cameraBatteryFill');
    const batteryText = document.getElementById('cameraBatteryLevel');
    if (batteryFill && batteryText && batteryLevel !== undefined && batteryLevel !== null && batteryLevel > 0) {
        const level = Math.min(100, Math.max(0, batteryLevel));
        batteryFill.style.width = level + '%';
        batteryFill.style.background = level <= 15 ? '#f44336' : level <= 30 ? '#ff9800' : '#4caf50';
        batteryText.textContent = level + '%';
    }

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

function initTempSensorSettings(temperature, batteryLevel) {
    const tempDisplay = document.getElementById('currentTempDisplay');
    if (tempDisplay) {
        tempDisplay.textContent = temperature ? temperature.toFixed(1) : '--';
    }

    const batteryFill = document.getElementById('tempBatteryFill');
    const batteryText = document.getElementById('tempBatteryLevel');
    if (batteryFill && batteryText && batteryLevel !== undefined && batteryLevel !== null && batteryLevel > 0) {
        const level = Math.min(100, Math.max(0, batteryLevel));
        batteryFill.style.width = level + '%';
        batteryFill.style.background = level <= 15 ? '#f44336' : level <= 30 ? '#ff9800' : '#4caf50';
        batteryText.textContent = level + '%';
    }

    const intervalSelect = document.getElementById('sensorInterval');
    if (intervalSelect && currentDeviceSettings) {
        intervalSelect.addEventListener('change', function () {
            sendDeviceCommand(currentDeviceSettings.id, 'set_report_interval', { interval: parseInt(this.value) * 60 });
        });
    }
}

function initHumiditySensorSettings(humidity, batteryLevel) {
    const humidityDisplay = document.getElementById('currentHumidityDisplay');
    if (humidityDisplay) {
        humidityDisplay.textContent = humidity || '--';
    }

    const batteryFill = document.getElementById('humidityBatteryFill');
    const batteryText = document.getElementById('humidityBatteryLevel');
    if (batteryFill && batteryText && batteryLevel !== undefined && batteryLevel !== null && batteryLevel > 0) {
        const level = Math.min(100, Math.max(0, batteryLevel));
        batteryFill.style.width = level + '%';
        batteryFill.style.background = level <= 15 ? '#f44336' : level <= 30 ? '#ff9800' : '#4caf50';
        batteryText.textContent = level + '%';
    }

    const intervalSelect = document.getElementById('humiditySensorInterval');
    if (intervalSelect && currentDeviceSettings) {
        intervalSelect.addEventListener('change', function () {
            sendDeviceCommand(currentDeviceSettings.id, 'set_report_interval', { interval: parseInt(this.value) * 60 });
        });
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
            showNotification('实时连接成功，设备状态将自动同步', 'success');
            refreshDeviceListFromServer();
        })
        .catch(err => {
            console.error("SignalR 连接失败:", err.toString());
            showNotification('实时连接失败，请刷新页面重试', 'error');
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

// ==================== 智能场景触发功能 ====================
function initSceneTriggers() {
    document.querySelectorAll('.trigger-scene-btn').forEach(btn => {
        btn.addEventListener('click', async function (e) {
            e.stopPropagation();
            const sceneItem = this.closest('.scene-item');
            const sceneId = this.dataset.sceneId;
            const sceneName = sceneItem?.dataset.sceneName || '';

            // 禁用按钮，防止重复点击
            this.disabled = true;
            const originalHtml = this.innerHTML;
            this.innerHTML = '<i class="fas fa-spinner fa-spin"></i> 执行中...';

            showNotification(`正在执行场景 "${sceneName}"...`, 'info');

            try {
                const response = await fetch(`/api/scenes/execute/${sceneId}`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' }
                });
                const data = await response.json();

                if (data.success) {
                    addAutomationLog(sceneName, '执行');
                    showNotification(`✅ 场景 "${sceneName}" 执行成功: ${data.message}`, 'success');
                } else {
                    if (data.offlineDevices && data.offlineDevices.length > 0) {
                        const offlineMsg = `⚠️ 以下设备离线: ${data.offlineDevices.join(', ')}。是否继续执行？（在线设备将正常执行）`;
                        if (confirm(offlineMsg)) {
                            const confirmResponse = await fetch(`/api/scenes/execute/${sceneId}/confirm`, {
                                method: 'POST',
                                headers: { 'Content-Type': 'application/json' },
                                body: JSON.stringify({ skipDeviceTypes: data.offlineDevices })
                            });
                            const confirmData = await confirmResponse.json();
                            if (confirmData.success) {
                                addAutomationLog(sceneName, '执行');
                                showNotification(`✅ ${confirmData.message}`, 'success');
                            } else {
                                showNotification(`❌ ${confirmData.message || '场景执行失败'}`, 'error');
                            }
                        } else {
                            showNotification(`场景 "${sceneName}" 未执行`, 'warning');
                        }
                    } else {
                        showNotification(`❌ ${data.message || '场景执行失败'}`, 'error');
                    }
                }
            } catch (error) {
                console.error('执行场景失败:', error);
                showNotification('❌ 执行场景失败，请检查网络连接', 'error');
            } finally {
                // 恢复按钮状态
                this.disabled = false;
                this.innerHTML = originalHtml;
            }
        });
    });
}

// ==================== 智能场景管理功能 ====================
let cachedDevices = [];

async function fetchOnlineDevices() {
    try {
        const response = await fetch('/api/devices/list');
        const data = await response.json();
        if (data.success && data.devices) {
            cachedDevices = data.devices.filter(d => d.statusText !== "离线");
            console.log(`获取到 ${cachedDevices.length} 个在线设备`);
            return cachedDevices;
        }
        return [];
    } catch (error) {
        console.error('获取设备列表失败:', error);
        return [];
    }
}

// 加载传感器设备用于条件触发
async function loadSensorDevices() {
    try {
        const response = await fetch('/api/devices/list');
        const data = await response.json();
        if (data.success && data.devices) {
            return data.devices.filter(d => d.typeIdentifier === 'temp-sensor' || d.typeIdentifier === 'humidity-sensor');
        }
        return [];
    } catch (error) {
        console.error('加载传感器设备失败:', error);
        return [];
    }
}

function getDeviceDisplayName(device) {
    let roomName = device.roomIdentifier || '未知房间';
    const room = roomData.find(r => r.roomId === device.roomIdentifier);
    if (room) {
        roomName = room.roomName;
    }
    return `${roomName} ${device.name}`;
}

function buildDeviceOptions(devices, selectedDeviceId = null) {
    let html = '<option value="">请选择设备</option>';
    devices.forEach(device => {
        const displayName = getDeviceDisplayName(device);
        const selected = selectedDeviceId === device.fullDeviceId ? 'selected' : '';
        html += `<option value="${device.fullDeviceId}" data-type="${device.typeIdentifier}" ${selected}>${escapeHtml(displayName)}</option>`;
    });
    return html;
}

async function refreshDeviceSelect(selectElement, selectedDeviceId = null) {
    const devices = await fetchOnlineDevices();
    selectElement.innerHTML = buildDeviceOptions(devices, selectedDeviceId);
}

// 刷新条件触发设备下拉框
async function refreshConditionDeviceSelect(selectElement, selectedDeviceId = null) {
    const devices = await loadSensorDevices();
    let html = '<option value="">选择传感器设备</option>';
    devices.forEach(device => {
        const displayName = getDeviceDisplayName(device);
        const selected = selectedDeviceId === device.fullDeviceId ? 'selected' : '';
        html += `<option value="${device.fullDeviceId}" data-type="${device.typeIdentifier}" ${selected}>${escapeHtml(displayName)}</option>`;
    });
    selectElement.innerHTML = html;
}

function getActionOptionsByDeviceType(deviceType) {
    switch (deviceType) {
        case 'ac':
            return '<option value="on">开启</option><option value="off">关闭</option><option value="set_temperature">设置温度</option><option value="set_mode">设置模式</option>';
        case 'fan':
            return '<option value="on">开启</option><option value="off">关闭</option><option value="set_speed">设置风速</option>';
        case 'light':
            return '<option value="on">开启</option><option value="off">关闭</option><option value="set_brightness">调节亮度</option>';
        default:
            return '<option value="on">开启</option><option value="off">关闭</option>';
    }
}

function requiresParameter(deviceType, action) {
    if (deviceType === 'ac' && action === 'set_temperature') return true;
    if (deviceType === 'ac' && action === 'set_mode') return true;
    if (deviceType === 'fan' && action === 'set_speed') return true;
    if (deviceType === 'light' && action === 'set_brightness') return true;
    return false;
}

function getParameterPlaceholder(deviceType, action) {
    if (deviceType === 'ac' && action === 'set_temperature') return '温度(16-30)';
    if (deviceType === 'ac' && action === 'set_mode') return '模式(cool/heat/fan/dry/auto)';
    if (deviceType === 'fan' && action === 'set_speed') return '风速(1-5)';
    if (deviceType === 'light' && action === 'set_brightness') return '亮度(0-100)';
    return '参数';
}

function addSceneAction(actionData = null) {
    const actionsList = document.getElementById('sceneActionsList');
    const newAction = document.createElement('div');
    newAction.className = 'scene-action-item';
    newAction.style.cssText = 'display: flex; gap: 10px; margin-bottom: 10px; align-items: center;';
    newAction.innerHTML = `
        <select class="action-device-select" style="flex: 3; padding: 8px; border-radius: 8px; border: 1px solid #b9cfec;">
            <option value="">加载设备中...</option>
        </select>
        <select class="action-type" style="flex: 2; padding: 8px; border-radius: 8px; border: 1px solid #b9cfec;">
            <option value="on">开启</option>
            <option value="off">关闭</option>
        </select>
        <input type="text" class="action-value" placeholder="参数" style="flex: 2; padding: 8px; border-radius: 8px; border: 1px solid #b9cfec; display: none;">
        <button type="button" class="remove-action-btn" style="background: #ff6b6b; color: white; border: none; padding: 8px 12px; border-radius: 8px; cursor: pointer;">删除</button>
    `;

    actionsList.appendChild(newAction);

    const deviceSelect = newAction.querySelector('.action-device-select');
    const actionTypeSelect = newAction.querySelector('.action-type');
    const actionValueInput = newAction.querySelector('.action-value');

    refreshDeviceSelect(deviceSelect).then(() => {
        if (actionData) {
            const option = Array.from(deviceSelect.options).find(opt => opt.value === actionData.deviceId);
            if (option) {
                deviceSelect.value = actionData.deviceId;
                const deviceType = option.dataset.type;
                actionTypeSelect.innerHTML = getActionOptionsByDeviceType(deviceType);
                actionTypeSelect.value = actionData.action || 'on';
                if (actionData.value) {
                    actionValueInput.value = actionData.value;
                    actionValueInput.style.display = 'block';
                }
            }
        }
    });

    deviceSelect.addEventListener('change', function () {
        const selectedOption = this.options[this.selectedIndex];
        const deviceType = selectedOption?.dataset.type;
        if (deviceType) {
            actionTypeSelect.innerHTML = getActionOptionsByDeviceType(deviceType);
        }
        actionTypeSelect.dispatchEvent(new Event('change'));
    });

    actionTypeSelect.addEventListener('change', function () {
        const selectedOption = deviceSelect.options[deviceSelect.selectedIndex];
        const deviceType = selectedOption?.dataset.type;
        const action = this.value;

        if (deviceType && requiresParameter(deviceType, action)) {
            actionValueInput.style.display = 'block';
            actionValueInput.placeholder = getParameterPlaceholder(deviceType, action);
        } else {
            actionValueInput.style.display = 'none';
            actionValueInput.value = '';
        }
    });

    newAction.querySelector('.remove-action-btn').addEventListener('click', function () {
        newAction.remove();
    });
}

function addSceneActionWithData(actionData, devices) {
    const actionsList = document.getElementById('sceneActionsList');
    const newAction = document.createElement('div');
    newAction.className = 'scene-action-item';
    newAction.style.cssText = 'display: flex; gap: 10px; margin-bottom: 10px; align-items: center;';
    newAction.innerHTML = `
        <select class="action-device-select" style="flex: 3; padding: 8px; border-radius: 8px; border: 1px solid #b9cfec;">
            ${buildDeviceOptions(devices, actionData.deviceId)}
        </select>
        <select class="action-type" style="flex: 2; padding: 8px; border-radius: 8px; border: 1px solid #b9cfec;">
            ${getActionOptionsByDeviceType(actionData.deviceType)}
        </select>
        <input type="text" class="action-value" placeholder="参数" style="flex: 2; padding: 8px; border-radius: 8px; border: 1px solid #b9cfec; display: ${actionData.value ? 'block' : 'none'};" value="${escapeHtml(actionData.value || '')}">
        <button type="button" class="remove-action-btn" style="background: #ff6b6b; color: white; border: none; padding: 8px 12px; border-radius: 8px; cursor: pointer;">删除</button>
    `;

    const deviceSelect = newAction.querySelector('.action-device-select');
    const actionTypeSelect = newAction.querySelector('.action-type');
    const actionValueInput = newAction.querySelector('.action-value');

    actionTypeSelect.value = actionData.action || 'on';

    deviceSelect.addEventListener('change', function () {
        const selectedOption = this.options[this.selectedIndex];
        const deviceType = selectedOption?.dataset.type;
        if (deviceType) {
            actionTypeSelect.innerHTML = getActionOptionsByDeviceType(deviceType);
        }
        actionTypeSelect.dispatchEvent(new Event('change'));
    });

    actionTypeSelect.addEventListener('change', function () {
        const selectedOption = deviceSelect.options[deviceSelect.selectedIndex];
        const deviceType = selectedOption?.dataset.type;
        const action = this.value;

        if (deviceType && requiresParameter(deviceType, action)) {
            actionValueInput.style.display = 'block';
            actionValueInput.placeholder = getParameterPlaceholder(deviceType, action);
        } else {
            actionValueInput.style.display = 'none';
            actionValueInput.value = '';
        }
    });

    actionsList.appendChild(newAction);

    newAction.querySelector('.remove-action-btn').addEventListener('click', function () {
        newAction.remove();
    });
}

// 添加条件
function addCondition(conditionData = null, sensorDevices = []) {
    const conditionList = document.getElementById('conditionList');
    const newItem = document.createElement('div');
    newItem.className = 'condition-item';
    newItem.style.cssText = 'display: flex; gap: 10px; margin-bottom: 10px; align-items: center;';

    let deviceOptions = '<option value="">选择传感器设备</option>';
    sensorDevices.forEach(device => {
        const displayName = getDeviceDisplayName(device);
        const selected = conditionData && conditionData.deviceId === device.fullDeviceId ? 'selected' : '';
        deviceOptions += `<option value="${device.fullDeviceId}" data-type="${device.typeIdentifier}" ${selected}>${escapeHtml(displayName)}</option>`;
    });

    newItem.innerHTML = `
        <select class="condition-device" style="flex: 2; padding: 8px; border-radius: 8px; border: 1px solid #b9cfec;">
            ${deviceOptions}
        </select>
        <select class="condition-operator" style="flex: 1; padding: 8px; border-radius: 8px; border: 1px solid #b9cfec;">
            <option value=">" ${conditionData?.operator === '>' ? 'selected' : ''}>大于</option>
            <option value="<" ${conditionData?.operator === '<' ? 'selected' : ''}>小于</option>
            <option value="=" ${conditionData?.operator === '=' ? 'selected' : ''}>等于</option>
            <option value=">=" ${conditionData?.operator === '>=' ? 'selected' : ''}>大于等于</option>
            <option value="<=" ${conditionData?.operator === '<=' ? 'selected' : ''}>小于等于</option>
        </select>
        <input type="text" class="condition-value" placeholder="阈值" style="flex: 1; padding: 8px; border-radius: 8px; border: 1px solid #b9cfec;" value="${conditionData?.value || ''}">
        <button type="button" class="remove-condition-btn" style="background: #ff6b6b; color: white; border: none; padding: 8px 12px; border-radius: 8px; cursor: pointer;">删除</button>
    `;

    conditionList.appendChild(newItem);

    newItem.querySelector('.remove-condition-btn').addEventListener('click', function () {
        newItem.remove();
    });
}

// 加载可用场景列表用于联动
async function loadAvailableScenesForLink(excludeId = 0) {
    try {
        const response = await fetch(`/api/scenes/available-for-link?excludeId=${excludeId}`);
        const data = await response.json();
        if (data.success) {
            return data.scenes;
        }
        return [];
    } catch (error) {
        console.error('加载场景列表失败:', error);
        return [];
    }
}

// 添加联动场景
function addLinkedScene(sceneData = null, availableScenes = []) {
    const linkedList = document.getElementById('linkedScenesList');
    const newItem = document.createElement('div');
    newItem.className = 'linked-scene-item';
    newItem.style.cssText = 'display: flex; gap: 10px; margin-bottom: 10px; align-items: center;';

    let sceneOptions = '<option value="">请选择联动场景</option>';
    availableScenes.forEach(scene => {
        const selected = sceneData && sceneData.sceneId === scene.id ? 'selected' : '';
        sceneOptions += `<option value="${scene.id}" ${selected}>${escapeHtml(scene.sceneName)}</option>`;
    });

    newItem.innerHTML = `
        <select class="linked-scene-select" style="flex: 3; padding: 8px; border-radius: 8px; border: 1px solid #b9cfec;">
            ${sceneOptions}
        </select>
        <select class="linked-scene-action" style="flex: 2; padding: 8px; border-radius: 8px; border: 1px solid #b9cfec;">
            <option value="execute" ${sceneData?.action === 'execute' ? 'selected' : ''}>执行场景</option>
            <option value="enable" ${sceneData?.action === 'enable' ? 'selected' : ''}>启用场景</option>
            <option value="disable" ${sceneData?.action === 'disable' ? 'selected' : ''}>禁用场景</option>
            <option value="toggle" ${sceneData?.action === 'toggle' ? 'selected' : ''}>切换启用状态</option>
        </select>
        <button type="button" class="remove-linked-btn" style="background: #ff6b6b; color: white; border: none; padding: 8px 12px; border-radius: 8px; cursor: pointer;">删除</button>
    `;

    linkedList.appendChild(newItem);

    newItem.querySelector('.remove-linked-btn').addEventListener('click', function () {
        newItem.remove();
    });
}

// 触发方式切换
function initTriggerTypeSelector() {
    const radioButtons = document.querySelectorAll('input[name="triggerType"]');
    const timeSettings = document.getElementById('timeTriggerSettings');
    const conditionSettings = document.getElementById('conditionTriggerSettings');

    if (!radioButtons.length) return;

    radioButtons.forEach(radio => {
        radio.addEventListener('change', function () {
            if (timeSettings) timeSettings.style.display = 'none';
            if (conditionSettings) conditionSettings.style.display = 'none';

            if (this.value === 'time' && timeSettings) {
                timeSettings.style.display = 'block';
            } else if (this.value === 'condition' && conditionSettings) {
                conditionSettings.style.display = 'block';
                // 加载传感器设备
                loadSensorDevices().then(devices => {
                    const deviceSelects = document.querySelectorAll('.condition-device');
                    if (deviceSelects.length === 1 && deviceSelects[0].options.length === 1) {
                        refreshConditionDeviceSelect(deviceSelects[0]);
                    }
                });
            }
        });
    });
}

function initSceneManagement() {
    const addSceneBtn = document.getElementById('addSceneBtn');
    if (addSceneBtn) {
        addSceneBtn.addEventListener('click', () => openSceneModal());
    }

    const sceneList = document.getElementById('sceneList');
    if (sceneList) {
        sceneList.addEventListener('click', (e) => {
            const editIcon = e.target.closest('.edit-scene-icon');
            const deleteIcon = e.target.closest('.delete-scene-icon');
            const triggerBtn = e.target.closest('.trigger-scene-btn');
            const sceneItem = e.target.closest('.scene-item');

            if (triggerBtn) {
                return;
            }

            if (editIcon && sceneItem) {
                const sceneId = sceneItem.dataset.sceneId;
                openSceneModal(sceneId);
                e.stopPropagation();
            } else if (deleteIcon && sceneItem) {
                const sceneId = sceneItem.dataset.sceneId;
                const sceneName = sceneItem.dataset.sceneName;
                deleteScene(sceneId, sceneName);
                e.stopPropagation();
            }
        });
    }

    document.querySelectorAll('.close-scene-modal').forEach(btn => {
        btn.addEventListener('click', () => {
            document.getElementById('sceneModal').style.display = 'none';
        });
    });

    const addActionBtn = document.getElementById('addActionBtn');
    if (addActionBtn) {
        addActionBtn.addEventListener('click', () => addSceneAction());
    }

    const saveSceneBtn = document.getElementById('saveSceneBtn');
    if (saveSceneBtn) {
        saveSceneBtn.addEventListener('click', saveScene);
    }

    // 添加联动场景按钮
    const addLinkedSceneBtn = document.getElementById('addLinkedSceneBtn');
    if (addLinkedSceneBtn) {
        addLinkedSceneBtn.addEventListener('click', async () => {
            const availableScenes = await loadAvailableScenesForLink(document.getElementById('sceneId').value || 0);
            addLinkedScene(null, availableScenes);
        });
    }

    // 添加条件按钮
    const addConditionBtn = document.getElementById('addConditionBtn');
    if (addConditionBtn) {
        addConditionBtn.addEventListener('click', async () => {
            const devices = await loadSensorDevices();
            addCondition(null, devices);
        });
    }

    document.querySelectorAll('.icon-option').forEach(icon => {
        icon.addEventListener('click', function () {
            document.querySelectorAll('.icon-option').forEach(i => i.classList.remove('selected'));
            this.classList.add('selected');
            document.getElementById('sceneIcon').value = this.dataset.icon;
        });
    });
}

async function openSceneModal(sceneId = null) {
    const modal = document.getElementById('sceneModal');
    const title = document.getElementById('sceneModalTitle');
    const form = document.getElementById('sceneForm');

    form.reset();
    document.getElementById('sceneId').value = '0';
    document.getElementById('sceneIcon').value = 'fa-home';

    // 重置触发方式
    const manualRadio = document.querySelector('input[name="triggerType"][value="manual"]');
    if (manualRadio) manualRadio.checked = true;
    const timeSettings = document.getElementById('timeTriggerSettings');
    const conditionSettings = document.getElementById('conditionTriggerSettings');
    if (timeSettings) timeSettings.style.display = 'none';
    if (conditionSettings) conditionSettings.style.display = 'none';
    document.getElementById('sceneExecuteTime').value = '';
    document.querySelectorAll('#timeTriggerSettings input[type="checkbox"]').forEach(cb => {
        cb.checked = false;
    });

    // 清空条件列表
    const conditionList = document.getElementById('conditionList');
    if (conditionList) conditionList.innerHTML = '';

    const actionsList = document.getElementById('sceneActionsList');
    if (actionsList) {
        actionsList.innerHTML = '';
        addSceneAction();
    }

    const linkedList = document.getElementById('linkedScenesList');
    if (linkedList) linkedList.innerHTML = '';

    // 加载可用场景用于联动
    const availableScenes = await loadAvailableScenesForLink(sceneId || 0);
    const sensorDevices = await loadSensorDevices();

    document.querySelectorAll('.icon-option').forEach(icon => {
        icon.classList.remove('selected');
        if (icon.dataset.icon === 'fa-home') {
            icon.classList.add('selected');
        }
    });

    if (sceneId) {
        title.textContent = '编辑智能场景';
        await loadSceneData(sceneId, availableScenes, sensorDevices);
    } else {
        title.textContent = '添加智能场景';
    }

    modal.style.display = 'flex';
}

async function loadSceneData(sceneId, availableScenes, sensorDevices) {
    try {
        const response = await fetch(`/api/scenes/${sceneId}`);
        const data = await response.json();
        if (data.success) {
            const scene = data.scene;
            document.getElementById('sceneId').value = scene.id;
            document.getElementById('sceneName').value = scene.sceneName;
            document.getElementById('sceneDescription').value = scene.description || '';

            // 设置触发方式
            const triggerType = scene.triggerType || 'manual';
            const radio = document.querySelector(`input[name="triggerType"][value="${triggerType}"]`);
            if (radio) radio.checked = true;

            // 定时设置
            if (scene.executeTime) {
                document.getElementById('sceneExecuteTime').value = scene.executeTime;
            }
            if (scene.repeatDays) {
                const days = scene.repeatDays.split(',');
                document.querySelectorAll('#timeTriggerSettings input[type="checkbox"]').forEach(cb => {
                    cb.checked = days.includes(cb.value);
                });
            }

            // 条件设置
            if (scene.conditions && scene.conditions.length > 0) {
                const conditionList = document.getElementById('conditionList');
                if (conditionList) conditionList.innerHTML = '';
                scene.conditions.forEach(condition => {
                    addCondition(condition, sensorDevices);
                });
                const conditionLogic = document.getElementById('conditionLogic');
                if (conditionLogic && scene.conditionLogic) {
                    conditionLogic.value = scene.conditionLogic;
                }
            }

            // 显示对应的设置面板
            if (triggerType === 'time') {
                const timeSettings = document.getElementById('timeTriggerSettings');
                if (timeSettings) timeSettings.style.display = 'block';
            } else if (triggerType === 'condition') {
                const conditionSettings = document.getElementById('conditionTriggerSettings');
                if (conditionSettings) conditionSettings.style.display = 'block';
                if (scene.conditions && scene.conditions.length === 0) {
                    addCondition(null, sensorDevices);
                }
            }

            const icon = scene.icon || 'fa-home';
            document.getElementById('sceneIcon').value = icon;
            document.querySelectorAll('.icon-option').forEach(opt => {
                opt.classList.remove('selected');
                if (opt.dataset.icon === icon) {
                    opt.classList.add('selected');
                }
            });

            const actionsList = document.getElementById('sceneActionsList');
            if (actionsList) {
                actionsList.innerHTML = '';
                const actions = scene.actions || [];

                if (actions.length === 0) {
                    addSceneAction();
                } else {
                    const devices = await fetchOnlineDevices();
                    actions.forEach(action => {
                        addSceneActionWithData(action, devices);
                    });
                }
            }

            const linkedList = document.getElementById('linkedScenesList');
            if (linkedList) {
                linkedList.innerHTML = '';
                const linkedScenes = scene.linkedScenes || [];
                linkedScenes.forEach(link => {
                    addLinkedScene(link, availableScenes);
                });
            }
        }
    } catch (error) {
        console.error('加载场景数据失败:', error);
        showNotification('加载场景数据失败', 'error');
    }
}

async function saveScene() {
    const sceneId = document.getElementById('sceneId').value;
    const sceneName = document.getElementById('sceneName').value;
    const sceneIcon = document.getElementById('sceneIcon').value;
    const sceneDescription = document.getElementById('sceneDescription').value;

    // 获取触发方式
    const selectedRadio = document.querySelector('input[name="triggerType"]:checked');
    const triggerType = selectedRadio ? selectedRadio.value : 'manual';
    let executeTime = '';
    let repeatDays = '';
    let conditions = [];
    let conditionLogic = 'and';

    if (triggerType === 'time') {
        executeTime = document.getElementById('sceneExecuteTime').value;
        const days = [];
        document.querySelectorAll('#timeTriggerSettings input[type="checkbox"]:checked').forEach(cb => {
            days.push(cb.value);
        });
        repeatDays = days.join(',');
    } else if (triggerType === 'condition') {
        const conditionItems = document.querySelectorAll('#conditionList .condition-item');
        conditionItems.forEach(item => {
            const deviceSelect = item.querySelector('.condition-device');
            const operator = item.querySelector('.condition-operator').value;
            const value = item.querySelector('.condition-value').value;
            const selectedOption = deviceSelect.options[deviceSelect.selectedIndex];

            if (deviceSelect.value && value) {
                conditions.push({
                    deviceId: deviceSelect.value,
                    deviceName: selectedOption.textContent,
                    deviceType: selectedOption.dataset.type,
                    operator: operator,
                    value: value
                });
            }
        });
        const logicSelect = document.getElementById('conditionLogic');
        if (logicSelect) conditionLogic = logicSelect.value;
    }

    // 获取操作列表
    const actions = [];
    const actionItems = document.querySelectorAll('#sceneActionsList .scene-action-item');

    for (const item of actionItems) {
        const deviceSelect = item.querySelector('.action-device-select');
        const actionType = item.querySelector('.action-type').value;
        const actionValue = item.querySelector('.action-value').value;
        const selectedOption = deviceSelect.options[deviceSelect.selectedIndex];

        if (!deviceSelect.value) {
            showNotification('请为每个操作选择设备', 'warning');
            return;
        }

        actions.push({
            deviceId: deviceSelect.value,
            deviceName: selectedOption.textContent,
            deviceType: selectedOption.dataset.type,
            action: actionType,
            value: actionValue || null
        });
    }

    if (actions.length === 0) {
        showNotification('请至少添加一个操作', 'warning');
        return;
    }

    // 获取联动场景
    const linkedScenes = [];
    const linkedItems = document.querySelectorAll('#linkedScenesList .linked-scene-item');
    for (const item of linkedItems) {
        const sceneSelect = item.querySelector('.linked-scene-select');
        const actionSelect = item.querySelector('.linked-scene-action');
        const selectedOption = sceneSelect.options[sceneSelect.selectedIndex];

        if (sceneSelect.value) {
            linkedScenes.push({
                sceneId: parseInt(sceneSelect.value),
                sceneName: selectedOption.textContent,
                action: actionSelect.value
            });
        }
    }

    const sceneData = {
        id: parseInt(sceneId) || 0,
        sceneName: sceneName,
        icon: sceneIcon,
        description: sceneDescription,
        triggerType: triggerType,
        executeTime: executeTime,
        repeatDays: repeatDays,
        conditions: conditions,
        conditionLogic: conditionLogic,
        actions: actions,
        linkedScenes: linkedScenes
    };

    try {
        const response = await fetch('/api/scenes/save', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(sceneData)
        });
        const data = await response.json();
        if (data.success) {
            showNotification('场景保存成功', 'success');
            document.getElementById('sceneModal').style.display = 'none';
            setTimeout(() => location.reload(), 1000);
        } else {
            showNotification('保存失败: ' + data.message, 'error');
        }
    } catch (error) {
        console.error('保存场景失败:', error);
        showNotification('保存场景失败', 'error');
    }
}

async function deleteScene(sceneId, sceneName) {
    if (confirm(`确定要删除场景 "${sceneName}" 吗？`)) {
        try {
            const response = await fetch(`/api/scenes/delete`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ id: parseInt(sceneId) })
            });
            const data = await response.json();
            if (data.success) {
                showNotification('场景删除成功', 'success');
                setTimeout(() => location.reload(), 500);
            } else {
                showNotification('删除失败: ' + data.message, 'error');
            }
        } catch (error) {
            console.error('删除场景失败:', error);
            showNotification('删除场景失败', 'error');
        }
    }
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
    if (typeof roomData === 'undefined') roomData = [];
    if (typeof deviceTypeData === 'undefined') deviceTypeData = [];

    getWeatherData();
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
    initSceneManagement();
    initSceneTriggers();
    initTriggerTypeSelector();
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