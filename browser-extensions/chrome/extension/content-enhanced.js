// Enhanced content script for comprehensive file type detection
class EnhancedContentDownloadDetector {
    constructor() {
        // Load the file type detector
        this.loadFileTypeDetector().then(() => {
            this.fileTypeDetector = new FileTypeDetector();
            this.processedLinks = new Set();
            this.buttonCount = 0;
            this.observer = null;
            
            this.init();
        });
    }

    async loadFileTypeDetector() {
        // Inject the file-types.js script
        return new Promise((resolve) => {
            const script = document.createElement('script');
            script.src = chrome.runtime.getURL('file-types.js');
            script.onload = resolve;
            document.head.appendChild(script);
        });
    }

    init() {
        // Listen for messages from background script
        chrome.runtime.onMessage.addListener(
            (request, sender, sendResponse) => this.handleMessage(request, sender, sendResponse)
        );

        // Comprehensive page monitoring
        this.scanPageForDownloads();
        this.observePageChanges();
        this.addDownloadButtons();
        this.monitorXHRRequests();
        
        // Re-scan every 5 seconds for dynamic content
        setInterval(() => this.scanPageForDownloads(), 5000);
    }

    scanPageForDownloads() {
        // Scan all links on the page
        const links = document.querySelectorAll('a[href]');
        let newLinksFound = 0;

        links.forEach(link => {
            const href = link.href;
            if (!href || this.processedLinks.has(href)) return;

            if (this.fileTypeDetector.isDownloadableUrl(href)) {
                this.addDownloadButtonToLink(link);
                this.processedLinks.add(href);
                newLinksFound++;
            }
        });

        // Scan for video/media embeds
        this.scanForMediaContent();

        // Scan for file inputs and download forms
        this.scanForDownloadForms();

        if (newLinksFound > 0) {
            console.log(`[OpenWebDM] Found ${newLinksFound} new downloadable links`);
        }
    }

    scanForMediaContent() {
        // Look for video elements
        const videos = document.querySelectorAll('video[src], video source[src]');
        videos.forEach(video => {
            const src = video.src || video.getAttribute('src');
            if (src && !this.processedLinks.has(src)) {
                this.addMediaDownloadButton(video, src, 'video');
                this.processedLinks.add(src);
            }
        });

        // Look for audio elements
        const audios = document.querySelectorAll('audio[src], audio source[src]');
        audios.forEach(audio => {
            const src = audio.src || audio.getAttribute('src');
            if (src && !this.processedLinks.has(src)) {
                this.addMediaDownloadButton(audio, src, 'audio');
                this.processedLinks.add(src);
            }
        });

        // Look for embedded video players (YouTube, Vimeo, etc.)
        this.scanForEmbeddedVideos();
    }

    scanForEmbeddedVideos() {
        const currentUrl = window.location.href;
        
        if (this.fileTypeDetector.isVideoSite(currentUrl)) {
            this.addVideoDownloadInterface();
        }

        // Look for embedded iframes
        const iframes = document.querySelectorAll('iframe[src*="youtube"], iframe[src*="vimeo"], iframe[src*="dailymotion"]');
        iframes.forEach(iframe => {
            this.addIframeDownloadButton(iframe);
        });
    }

    scanForDownloadForms() {
        // Look for download forms
        const forms = document.querySelectorAll('form[action*="download"], form[action*="export"]');
        forms.forEach(form => {
            if (!form.hasAttribute('data-openwebdm-processed')) {
                this.addFormDownloadButton(form);
                form.setAttribute('data-openwebdm-processed', 'true');
            }
        });

        // Look for download buttons
        const downloadButtons = document.querySelectorAll('a[href*="download"], button[onclick*="download"], input[onclick*="download"]');
        downloadButtons.forEach(button => {
            if (!button.hasAttribute('data-openwebdm-processed')) {
                this.enhanceDownloadButton(button);
                button.setAttribute('data-openwebdm-processed', 'true');
            }
        });
    }

    addDownloadButtonToLink(link) {
        if (link.hasAttribute('data-openwebdm-processed')) return;

        const button = document.createElement('span');
        button.className = 'openwebdm-download-btn';
        button.innerHTML = '⬇️';
        button.title = 'Download with OpenWeb DM';
        button.style.cssText = `
            display: inline-block;
            margin-left: 6px;
            padding: 3px 6px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            border-radius: 4px;
            font-size: 11px;
            cursor: pointer;
            text-decoration: none;
            vertical-align: middle;
            box-shadow: 0 2px 4px rgba(0,0,0,0.2);
            transition: all 0.2s ease;
        `;

        button.addEventListener('mouseenter', () => {
            button.style.transform = 'scale(1.1)';
            button.style.boxShadow = '0 4px 8px rgba(0,0,0,0.3)';
        });

        button.addEventListener('mouseleave', () => {
            button.style.transform = 'scale(1)';
            button.style.boxShadow = '0 2px 4px rgba(0,0,0,0.2)';
        });

        button.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            
            this.sendDownloadRequest(link.href, link.textContent.trim() || null);
            
            // Visual feedback
            button.innerHTML = '✓';
            button.style.background = '#4CAF50';
            setTimeout(() => {
                button.innerHTML = '⬇️';
                button.style.background = 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)';
            }, 2000);
        });

        link.parentNode.insertBefore(button, link.nextSibling);
        link.setAttribute('data-openwebdm-processed', 'true');
        this.buttonCount++;
    }

    addMediaDownloadButton(element, src, type) {
        const container = element.parentElement;
        if (!container) return;

        const downloadBar = document.createElement('div');
        downloadBar.className = 'openwebdm-media-bar';
        downloadBar.style.cssText = `
            position: absolute;
            bottom: 10px;
            right: 10px;
            background: rgba(0,0,0,0.8);
            color: white;
            padding: 8px 12px;
            border-radius: 6px;
            font-size: 12px;
            z-index: 1000;
            cursor: pointer;
            transition: opacity 0.3s ease;
        `;
        downloadBar.innerHTML = `📥 Download ${type.toUpperCase()}`;

        downloadBar.addEventListener('click', () => {
            this.sendDownloadRequest(src, null, type);
        });

        // Position container relatively if needed
        if (getComputedStyle(container).position === 'static') {
            container.style.position = 'relative';
        }

        container.appendChild(downloadBar);
    }

    addVideoDownloadInterface() {
        if (document.querySelector('.openwebdm-video-interface')) return;

        const videoInterface = document.createElement('div');
        videoInterface.className = 'openwebdm-video-interface';
        videoInterface.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 15px;
            border-radius: 10px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.3);
            z-index: 10000;
            font-family: Arial, sans-serif;
            max-width: 300px;
        `;

        videoInterface.innerHTML = `
            <div style="display: flex; align-items: center; margin-bottom: 10px;">
                <span style="font-size: 16px; margin-right: 8px;">🎥</span>
                <strong>OpenWeb DM</strong>
                <button id="owdm-close" style="margin-left: auto; background: none; border: none; color: white; font-size: 18px; cursor: pointer;">×</button>
            </div>
            <p style="margin: 0 0 15px 0; font-size: 13px;">Video detected! Click to download with OpenWeb Download Manager.</p>
            <button id="owdm-download-video" style="width: 100%; padding: 8px; background: white; color: #667eea; border: none; border-radius: 5px; font-weight: bold; cursor: pointer;">
                Download Video
            </button>
        `;

        document.body.appendChild(videoInterface);

        // Event handlers
        document.getElementById('owdm-close').addEventListener('click', () => {
            videoInterface.remove();
        });

        document.getElementById('owdm-download-video').addEventListener('click', () => {
            this.sendDownloadRequest(window.location.href, null, 'video');
            videoInterface.remove();
        });

        // Auto-hide after 10 seconds
        setTimeout(() => {
            if (videoInterface.parentNode) {
                videoInterface.style.opacity = '0.7';
            }
        }, 10000);
    }

    addIframeDownloadButton(iframe) {
        if (iframe.hasAttribute('data-openwebdm-processed')) return;

        const button = document.createElement('button');
        button.className = 'openwebdm-iframe-btn';
        button.innerHTML = '📥 Download';
        button.style.cssText = `
            position: absolute;
            top: 5px;
            right: 5px;
            padding: 5px 10px;
            background: #667eea;
            color: white;
            border: none;
            border-radius: 4px;
            font-size: 11px;
            cursor: pointer;
            z-index: 1000;
        `;

        button.addEventListener('click', () => {
            const src = iframe.src;
            if (src) {
                this.sendDownloadRequest(src, null, 'video');
            }
        });

        // Position iframe container relatively
        const container = iframe.parentElement;
        if (container && getComputedStyle(container).position === 'static') {
            container.style.position = 'relative';
        }

        iframe.parentElement?.appendChild(button);
        iframe.setAttribute('data-openwebdm-processed', 'true');
    }

    monitorXHRRequests() {
        // Intercept XMLHttpRequest to detect file downloads
        const originalOpen = XMLHttpRequest.prototype.open;
        const self = this;

        XMLHttpRequest.prototype.open = function(method, url, async, user, password) {
            this._url = url;
            this._method = method;
            
            this.addEventListener('readystatechange', function() {
                if (this.readyState === XMLHttpRequest.HEADERS_RECEIVED) {
                    const contentType = this.getResponseHeader('Content-Type');
                    const contentDisposition = this.getResponseHeader('Content-Disposition');
                    
                    if (self.fileTypeDetector.shouldInterceptDownload(url, contentType, contentDisposition)) {
                        console.log('[OpenWebDM] Detected XHR download:', url);
                        self.sendDownloadRequest(url);
                    }
                }
            });
            
            return originalOpen.call(this, method, url, async, user, password);
        };
    }

    observePageChanges() {
        this.observer = new MutationObserver((mutations) => {
            let shouldRescan = false;
            
            mutations.forEach((mutation) => {
                if (mutation.type === 'childList') {
                    mutation.addedNodes.forEach((node) => {
                        if (node.nodeType === Node.ELEMENT_NODE) {
                            // Check if new links were added
                            if (node.tagName === 'A' || node.querySelector?.('a[href]')) {
                                shouldRescan = true;
                            }
                            // Check for new media elements
                            if (['VIDEO', 'AUDIO', 'SOURCE', 'IFRAME'].includes(node.tagName)) {
                                shouldRescan = true;
                            }
                        }
                    });
                }
            });

            if (shouldRescan) {
                setTimeout(() => this.scanPageForDownloads(), 500);
            }
        });

        this.observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    sendDownloadRequest(url, filename = null, type = null) {
        chrome.runtime.sendMessage({
            action: 'downloadUrl',
            url: url,
            filename: filename,
            type: type,
            referrer: window.location.href,
            timestamp: Date.now()
        }, (response) => {
            if (chrome.runtime.lastError) {
                console.error('[OpenWebDM] Error sending download request:', chrome.runtime.lastError);
            } else if (response?.success) {
                console.log('[OpenWebDM] Download request sent successfully');
            }
        });
    }

    handleMessage(request, sender, sendResponse) {
        switch (request.action) {
            case 'getAllLinks':
                const links = this.getAllDownloadableLinks();
                sendResponse({ links: links });
                break;
                
            case 'addDownloadButtons':
                this.scanPageForDownloads();
                sendResponse({ success: true, buttonsAdded: this.buttonCount });
                break;

            case 'getStats':
                sendResponse({
                    processedLinks: this.processedLinks.size,
                    buttonsAdded: this.buttonCount,
                    currentUrl: window.location.href
                });
                break;
        }
    }

    getAllDownloadableLinks() {
        const links = [];
        const anchors = document.querySelectorAll('a[href]');

        anchors.forEach((anchor) => {
            const href = anchor.href;
            if (this.fileTypeDetector.isDownloadableUrl(href)) {
                links.push({
                    url: href,
                    filename: this.getFilenameFromUrl(href) || anchor.textContent.trim(),
                    text: anchor.textContent.trim(),
                    category: this.fileTypeDetector.getFileTypeCategory(href)
                });
            }
        });

        return links;
    }

    getFilenameFromUrl(url) {
        try {
            const urlObj = new URL(url);
            const pathname = urlObj.pathname;
            return pathname.split('/').pop() || null;
        } catch {
            return null;
        }
    }
}

// Initialize enhanced content detector when page is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        new EnhancedContentDownloadDetector();
    });
} else {
    new EnhancedContentDownloadDetector();
}