// Import the file type detector
importScripts('file-types.js');

class OpenWebDMExtension {
    constructor() {
        this.nativePort = null;
        this.isEnabled = true;
        this.minFileSize = 1024 * 100; // 100KB minimum
        this.fileTypeDetector = new FileTypeDetector();
        this.interceptedRequests = new Map();
        this.stats = {
            totalIntercepted: 0,
            sessionStart: Date.now()
        };
        
        this.init();
    }

    init() {
        // Create context menu
        chrome.runtime.onInstalled.addListener(() => {
            this.createContextMenus();
        });

        // Handle download interception
        chrome.webRequest.onBeforeRequest.addListener(
            (details) => this.handleDownloadRequest(details),
            { urls: ['<all_urls>'] },
            ['requestBody']
        );

        // Handle messages from content script
        chrome.runtime.onMessage.addListener(
            (request, sender, sendResponse) => this.handleMessage(request, sender, sendResponse)
        );

        // Context menu click handler
        chrome.contextMenus.onClicked.addListener(
            (info, tab) => this.handleContextMenu(info, tab)
        );

        // Connect to native application
        this.connectToNativeApp();
    }

    createContextMenus() {
        chrome.contextMenus.create({
            id: 'downloadWithOpenWebDM',
            title: 'Download with OpenWeb DM',
            contexts: ['link', 'image', 'video', 'audio'],
            enabled: this.isEnabled
        });

        chrome.contextMenus.create({
            id: 'downloadAllLinks',
            title: 'Download all links with OpenWeb DM',
            contexts: ['page'],
            enabled: this.isEnabled
        });

        chrome.contextMenus.create({
            id: 'separator1',
            type: 'separator',
            contexts: ['link', 'image', 'video', 'audio', 'page']
        });

        chrome.contextMenus.create({
            id: 'openWebDMSettings',
            title: 'OpenWeb DM Settings',
            contexts: ['link', 'image', 'video', 'audio', 'page']
        });
    }

    connectToNativeApp() {
        try {
            this.nativePort = chrome.runtime.connectNative('com.openwebdm.native');
            
            this.nativePort.onMessage.addListener((message) => {
                console.log('Message from native app:', message);
            });

            this.nativePort.onDisconnect.addListener(() => {
                console.log('Native app disconnected:', chrome.runtime.lastError);
                this.nativePort = null;
                // Try to reconnect after 5 seconds
                setTimeout(() => this.connectToNativeApp(), 5000);
            });

            // Send ping to test connection
            this.sendToNativeApp({ type: 'ping' });
        } catch (error) {
            console.error('Failed to connect to native app:', error);
        }
    }

    handleDownloadRequest(details) {
        if (!this.isEnabled || !this.shouldIntercept(details)) {
            return {};
        }

        // Log the interception
        this.stats.totalIntercepted++;
        console.log(`[OpenWebDM] Intercepted download #${this.stats.totalIntercepted}: ${details.url}`);

        // Get file category for better organization
        let contentType = '';
        if (details.responseHeaders) {
            const ctHeader = details.responseHeaders.find(h => h.name.toLowerCase() === 'content-type');
            if (ctHeader) contentType = ctHeader.value;
        }

        const category = this.fileTypeDetector.getFileTypeCategory(details.url, contentType);

        // Cancel the original download and send to OpenWebDM
        this.sendDownloadToNativeApp(details.url, {
            referrer: details.initiator,
            userAgent: navigator.userAgent,
            headers: details.requestHeaders || [],
            category: category,
            contentType: contentType,
            timestamp: Date.now()
        });

        // Show notification for successful interception
        chrome.notifications?.create({
            type: 'basic',
            iconUrl: 'icons/icon48.png',
            title: 'OpenWeb DM',
            message: `Download intercepted and sent to OpenWeb Download Manager`,
            priority: 1
        });

        return { cancel: true };
    }

    shouldIntercept(details) {
        // Don't intercept if it's not a GET request
        if (details.method !== 'GET') return false;

        // Get headers for analysis
        let contentType = '';
        let contentDisposition = '';
        let contentLength = 0;

        if (details.responseHeaders) {
            details.responseHeaders.forEach(header => {
                const name = header.name.toLowerCase();
                if (name === 'content-type') {
                    contentType = header.value;
                } else if (name === 'content-disposition') {
                    contentDisposition = header.value;
                } else if (name === 'content-length') {
                    contentLength = parseInt(header.value) || 0;
                }
            });
        }

        // Use the enhanced file type detector
        return this.fileTypeDetector.shouldInterceptDownload(
            details.url,
            contentType,
            contentDisposition,
            contentLength
        );
    }

    handleMessage(request, sender, sendResponse) {
        switch (request.action) {
            case 'downloadUrl':
                this.sendDownloadToNativeApp(request.url, {
                    filename: request.filename,
                    referrer: sender.tab?.url,
                    userAgent: navigator.userAgent
                });
                sendResponse({ success: true });
                break;

            case 'getSettings':
                chrome.storage.sync.get(['isEnabled', 'minFileSize'], (result) => {
                    sendResponse({
                        isEnabled: result.isEnabled !== false,
                        minFileSize: result.minFileSize || this.minFileSize
                    });
                });
                return true;

            case 'updateSettings':
                chrome.storage.sync.set(request.settings, () => {
                    this.isEnabled = request.settings.isEnabled;
                    this.minFileSize = request.settings.minFileSize;
                    sendResponse({ success: true });
                });
                return true;
        }
    }

    handleContextMenu(info, tab) {
        switch (info.menuItemId) {
            case 'downloadWithOpenWebDM':
                const url = info.linkUrl || info.srcUrl;
                if (url) {
                    this.sendDownloadToNativeApp(url, {
                        referrer: tab.url,
                        userAgent: navigator.userAgent
                    });
                }
                break;

            case 'downloadAllLinks':
                chrome.tabs.sendMessage(tab.id, { action: 'getAllLinks' }, (response) => {
                    if (response && response.links) {
                        response.links.forEach(link => {
                            this.sendDownloadToNativeApp(link.url, {
                                filename: link.filename,
                                referrer: tab.url,
                                userAgent: navigator.userAgent
                            });
                        });
                    }
                });
                break;

            case 'openWebDMSettings':
                chrome.tabs.create({ url: chrome.runtime.getURL('popup.html') });
                break;
        }
    }

    sendDownloadToNativeApp(url, options = {}) {
        if (!this.nativePort) {
            console.error('Native app not connected');
            return;
        }

        const message = {
            type: 'download_request',
            data: {
                url: url,
                filename: options.filename || '',
                referrer: options.referrer || '',
                userAgent: options.userAgent || '',
                headers: options.headers || {}
            }
        };

        try {
            this.nativePort.postMessage(message);
            console.log('Sent download request to native app:', url);
        } catch (error) {
            console.error('Failed to send message to native app:', error);
        }
    }

    sendToNativeApp(message) {
        if (!this.nativePort) return;
        
        try {
            this.nativePort.postMessage(message);
        } catch (error) {
            console.error('Failed to send message to native app:', error);
        }
    }
}

// Initialize extension
const openWebDM = new OpenWebDMExtension();