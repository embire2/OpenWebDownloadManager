document.addEventListener('DOMContentLoaded', function() {
    const enableToggle = document.getElementById('enableToggle');
    const minFileSizeInput = document.getElementById('minFileSize');
    const extensionStatusSpan = document.getElementById('extensionStatus');
    const nativeStatusSpan = document.getElementById('nativeStatus');
    const extensionIndicator = document.getElementById('extensionIndicator');
    const nativeIndicator = document.getElementById('nativeIndicator');
    const downloadsCountSpan = document.getElementById('downloadsCount');
    const openAppBtn = document.getElementById('openApp');
    const settingsBtn = document.getElementById('settings');

    // Load current settings
    loadSettings();

    // Event listeners
    enableToggle.addEventListener('change', saveSettings);
    minFileSizeInput.addEventListener('change', saveSettings);
    openAppBtn.addEventListener('click', openNativeApp);
    settingsBtn.addEventListener('click', openSettings);

    // Check connection status
    checkConnectionStatus();

    function loadSettings() {
        chrome.runtime.sendMessage({ action: 'getSettings' }, (response) => {
            if (response) {
                enableToggle.checked = response.isEnabled !== false;
                minFileSizeInput.value = response.minFileSize || 100;
                
                updateExtensionStatus(response.isEnabled !== false);
            }
        });

        // Load download count
        chrome.storage.local.get(['downloadsToday', 'lastCountDate'], (result) => {
            const today = new Date().toDateString();
            if (result.lastCountDate === today) {
                downloadsCountSpan.textContent = result.downloadsToday || 0;
            } else {
                downloadsCountSpan.textContent = 0;
            }
        });
    }

    function saveSettings() {
        const settings = {
            isEnabled: enableToggle.checked,
            minFileSize: parseInt(minFileSizeInput.value) || 100
        };

        chrome.runtime.sendMessage({ 
            action: 'updateSettings', 
            settings: settings 
        }, (response) => {
            if (response && response.success) {
                updateExtensionStatus(settings.isEnabled);
                showToast('Settings saved successfully');
            }
        });
    }

    function updateExtensionStatus(isEnabled) {
        if (isEnabled) {
            extensionStatusSpan.textContent = 'Active';
            extensionIndicator.className = 'indicator connected';
        } else {
            extensionStatusSpan.textContent = 'Disabled';
            extensionIndicator.className = 'indicator disconnected';
        }
    }

    function checkConnectionStatus() {
        // Test native app connection
        chrome.runtime.sendMessage({ action: 'testNativeConnection' }, (response) => {
            if (chrome.runtime.lastError || !response || !response.connected) {
                nativeStatusSpan.textContent = 'Disconnected';
                nativeIndicator.className = 'indicator disconnected';
            } else {
                nativeStatusSpan.textContent = 'Connected';
                nativeIndicator.className = 'indicator connected';
            }
        });
    }

    function openNativeApp() {
        // Try to open the native application
        chrome.runtime.sendMessage({ action: 'openNativeApp' }, (response) => {
            if (chrome.runtime.lastError) {
                showToast('Failed to open OpenWeb DM. Please make sure it\'s installed.');
            } else {
                showToast('Opening OpenWeb Download Manager...');
                window.close();
            }
        });
    }

    function openSettings() {
        chrome.tabs.create({ 
            url: chrome.runtime.getURL('options.html') 
        });
        window.close();
    }

    function showToast(message) {
        // Create and show a simple toast notification
        const toast = document.createElement('div');
        toast.textContent = message;
        toast.style.cssText = `
            position: fixed;
            bottom: 20px;
            left: 50%;
            transform: translateX(-50%);
            background: rgba(0, 0, 0, 0.8);
            color: white;
            padding: 10px 20px;
            border-radius: 6px;
            font-size: 12px;
            z-index: 9999;
            opacity: 0;
            transition: opacity 0.3s ease;
        `;

        document.body.appendChild(toast);

        // Show toast
        setTimeout(() => {
            toast.style.opacity = '1';
        }, 100);

        // Hide and remove toast
        setTimeout(() => {
            toast.style.opacity = '0';
            setTimeout(() => {
                document.body.removeChild(toast);
            }, 300);
        }, 3000);
    }

    // Update download count when downloads occur
    chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
        if (request.action === 'downloadStarted') {
            const today = new Date().toDateString();
            chrome.storage.local.get(['downloadsToday', 'lastCountDate'], (result) => {
                let count = 0;
                if (result.lastCountDate === today) {
                    count = (result.downloadsToday || 0) + 1;
                } else {
                    count = 1;
                }

                chrome.storage.local.set({
                    downloadsToday: count,
                    lastCountDate: today
                });

                downloadsCountSpan.textContent = count;
            });
        }
    });
});