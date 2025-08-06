// Injected script to intercept downloads at page level
(function() {
    'use strict';

    // Override XMLHttpRequest to detect file downloads
    const originalXHROpen = XMLHttpRequest.prototype.open;
    XMLHttpRequest.prototype.open = function(method, url, async, user, password) {
        this._url = url;
        this._method = method;
        return originalXHROpen.call(this, method, url, async, user, password);
    };

    const originalXHRSend = XMLHttpRequest.prototype.send;
    XMLHttpRequest.prototype.send = function(data) {
        const xhr = this;
        
        xhr.addEventListener('readystatechange', function() {
            if (xhr.readyState === XMLHttpRequest.HEADERS_RECEIVED) {
                const contentType = xhr.getResponseHeader('Content-Type');
                const contentDisposition = xhr.getResponseHeader('Content-Disposition');
                
                if (shouldInterceptDownload(contentType, contentDisposition, xhr._url)) {
                    // Abort the original request
                    xhr.abort();
                    
                    // Send to extension
                    window.postMessage({
                        type: 'OPENWEBDM_DOWNLOAD_REQUEST',
                        url: xhr._url,
                        headers: getAllResponseHeaders(xhr)
                    }, '*');
                }
            }
        });
        
        return originalXHRSend.call(this, data);
    };

    // Override fetch API
    const originalFetch = window.fetch;
    window.fetch = function(input, init) {
        const url = typeof input === 'string' ? input : input.url;
        
        return originalFetch.call(this, input, init).then(response => {
            const contentType = response.headers.get('Content-Type');
            const contentDisposition = response.headers.get('Content-Disposition');
            
            if (shouldInterceptDownload(contentType, contentDisposition, url)) {
                window.postMessage({
                    type: 'OPENWEBDM_DOWNLOAD_REQUEST',
                    url: url,
                    headers: Object.fromEntries(response.headers.entries())
                }, '*');
                
                // Return empty response to prevent normal download
                return new Response(new ArrayBuffer(0));
            }
            
            return response;
        });
    };

    // Listen for messages from content script
    window.addEventListener('message', function(event) {
        if (event.source !== window) return;
        
        if (event.data.type === 'OPENWEBDM_DOWNLOAD_REQUEST') {
            // Forward to content script
            const customEvent = new CustomEvent('openwebdm-download', {
                detail: event.data
            });
            document.dispatchEvent(customEvent);
        }
    });

    function shouldInterceptDownload(contentType, contentDisposition, url) {
        if (!contentType && !contentDisposition && !url) return false;
        
        // Check Content-Disposition header
        if (contentDisposition && contentDisposition.includes('attachment')) {
            return true;
        }
        
        // Check Content-Type
        if (contentType) {
            const downloadTypes = [
                'application/octet-stream',
                'application/zip',
                'application/x-rar-compressed',
                'application/pdf',
                'application/x-msdownload',
                'application/x-executable'
            ];
            
            if (downloadTypes.some(type => contentType.includes(type))) {
                return true;
            }
            
            if (contentType.startsWith('video/') || 
                contentType.startsWith('audio/') ||
                contentType.startsWith('application/')) {
                return true;
            }
        }
        
        // Check URL pattern
        if (url) {
            const downloadPatterns = [
                /\.(exe|msi|zip|rar|7z|tar|gz|pdf|mp4|avi|mkv|mp3|flac|iso|dmg|pkg)(\?|$)/i,
                /download/i,
                /attachment/i
            ];
            
            return downloadPatterns.some(pattern => pattern.test(url));
        }
        
        return false;
    }

    function getAllResponseHeaders(xhr) {
        const headers = {};
        const headerString = xhr.getAllResponseHeaders();
        
        if (headerString) {
            headerString.split('\r\n').forEach(line => {
                const parts = line.split(': ');
                if (parts.length === 2) {
                    headers[parts[0]] = parts[1];
                }
            });
        }
        
        return headers;
    }
})();