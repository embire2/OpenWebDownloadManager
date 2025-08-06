// Content script for detecting downloadable links and media
class ContentDownloadDetector {
    constructor() {
        this.downloadableExtensions = [
            'exe', 'msi', 'zip', 'rar', '7z', 'tar', 'gz', 'bz2',
            'pdf', 'doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx',
            'mp4', 'avi', 'mkv', 'mov', 'wmv', 'flv', 'webm',
            'mp3', 'wav', 'flac', 'aac', 'ogg', 'wma',
            'jpg', 'jpeg', 'png', 'gif', 'bmp', 'tiff', 'webp',
            'iso', 'img', 'dmg', 'pkg', 'deb', 'rpm',
            'apk', 'ipa', 'cab', 'msu'
        ];
        
        this.init();
    }

    init() {
        // Listen for messages from background script
        chrome.runtime.onMessage.addListener(
            (request, sender, sendResponse) => this.handleMessage(request, sender, sendResponse)
        );

        // Inject download button script
        this.injectDownloadScript();

        // Monitor for new links added dynamically
        this.observePageChanges();

        // Add download buttons to existing links
        this.addDownloadButtons();
    }

    handleMessage(request, sender, sendResponse) {
        switch (request.action) {
            case 'getAllLinks':
                const links = this.getAllDownloadableLinks();
                sendResponse({ links: links });
                break;
                
            case 'addDownloadButtons':
                this.addDownloadButtons();
                sendResponse({ success: true });
                break;
        }
    }

    injectDownloadScript() {
        const script = document.createElement('script');
        script.src = chrome.runtime.getURL('inject.js');
        (document.head || document.documentElement).appendChild(script);
    }

    observePageChanges() {
        const observer = new MutationObserver((mutations) => {
            let shouldUpdate = false;
            
            mutations.forEach((mutation) => {
                if (mutation.type === 'childList') {
                    mutation.addedNodes.forEach((node) => {
                        if (node.nodeType === Node.ELEMENT_NODE) {
                            const element = node;
                            if (element.tagName === 'A' || element.querySelector('a')) {
                                shouldUpdate = true;
                            }
                        }
                    });
                }
            });

            if (shouldUpdate) {
                setTimeout(() => this.addDownloadButtons(), 500);
            }
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    getAllDownloadableLinks() {
        const links = [];
        const anchors = document.querySelectorAll('a[href]');

        anchors.forEach((anchor) => {
            const href = anchor.href;
            if (this.isDownloadableUrl(href)) {
                links.push({
                    url: href,
                    filename: this.getFilenameFromUrl(href) || anchor.textContent.trim(),
                    text: anchor.textContent.trim()
                });
            }
        });

        return links;
    }

    addDownloadButtons() {
        const links = document.querySelectorAll('a[href]:not([data-openwebdm-processed])');
        
        links.forEach((link) => {
            if (this.isDownloadableUrl(link.href)) {
                this.addDownloadButtonToLink(link);
                link.setAttribute('data-openwebdm-processed', 'true');
            }
        });
    }

    addDownloadButtonToLink(link) {
        // Create download button
        const button = document.createElement('span');
        button.className = 'openwebdm-download-btn';
        button.innerHTML = '⬇';
        button.title = 'Download with OpenWeb DM';
        button.style.cssText = `
            display: inline-block;
            margin-left: 5px;
            padding: 2px 6px;
            background: #007bff;
            color: white;
            border-radius: 3px;
            font-size: 12px;
            cursor: pointer;
            text-decoration: none;
            vertical-align: middle;
        `;

        button.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            
            chrome.runtime.sendMessage({
                action: 'downloadUrl',
                url: link.href,
                filename: this.getFilenameFromUrl(link.href)
            });
        });

        // Insert button after the link
        link.parentNode.insertBefore(button, link.nextSibling);
    }

    isDownloadableUrl(url) {
        if (!url) return false;
        
        try {
            const urlObj = new URL(url);
            const pathname = urlObj.pathname.toLowerCase();
            const extension = pathname.split('.').pop();
            
            return this.downloadableExtensions.includes(extension) || 
                   pathname.includes('download') ||
                   urlObj.searchParams.has('download');
        } catch {
            return false;
        }
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

// Initialize content detector
const contentDetector = new ContentDownloadDetector();