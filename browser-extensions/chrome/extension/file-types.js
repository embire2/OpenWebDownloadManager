// Comprehensive file type detection for OpenWeb Download Manager
class FileTypeDetector {
    constructor() {
        this.downloadableExtensions = new Set([
            // Archives
            'zip', 'rar', '7z', 'tar', 'gz', 'bz2', 'xz', 'cab', 'iso', 'img', 'dmg', 'pkg',
            
            // Documents
            'pdf', 'doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx', 'odt', 'ods', 'odp',
            'rtf', 'txt', 'csv', 'xml', 'json', 'epub', 'mobi',
            
            // Images
            'jpg', 'jpeg', 'png', 'gif', 'bmp', 'tiff', 'tga', 'psd', 'svg', 'webp',
            'ico', 'raw', 'cr2', 'nef', 'arw', 'dng', 'heic', 'heif',
            
            // Videos
            'mp4', 'avi', 'mkv', 'mov', 'wmv', 'flv', 'webm', 'm4v', '3gp', 'ogv',
            'mpg', 'mpeg', 'ts', 'vob', 'asf', 'rm', 'rmvb', 'f4v',
            
            // Audio
            'mp3', 'wav', 'flac', 'aac', 'ogg', 'wma', 'm4a', 'opus', 'aiff',
            'ape', 'ac3', 'dts', 'mp2', 'amr', 'au', 'ra',
            
            // Software/Programs
            'exe', 'msi', 'dmg', 'pkg', 'deb', 'rpm', 'apk', 'ipa', 'app',
            'run', 'bin', 'appimage', 'snap', 'flatpak',
            
            // Development
            'zip', 'tar.gz', 'tgz', 'jar', 'war', 'ear', 'nupkg', 'gem',
            'whl', 'egg', 'vsix', 'xpi', 'crx',
            
            // Fonts
            'ttf', 'otf', 'woff', 'woff2', 'eot',
            
            // Data/Database
            'sql', 'db', 'sqlite', 'mdb', 'accdb', 'bak',
            
            // 3D/CAD
            'obj', 'fbx', 'dae', '3ds', 'blend', 'max', 'dwg', 'step', 'stl',
            
            // Game Files
            'rom', 'iso', 'cue', 'bin', 'nds', 'gba', 'n64', 'smc',
            
            // Virtual Machine
            'ova', 'ovf', 'vmx', 'vdi', 'vhd', 'vhdx', 'qcow2'
        ]);

        this.downloadableMimeTypes = new Set([
            // Archives
            'application/zip',
            'application/x-rar-compressed',
            'application/x-7z-compressed',
            'application/x-tar',
            'application/gzip',
            'application/x-bzip2',
            'application/x-xz',
            'application/vnd.ms-cab-compressed',
            'application/x-iso9660-image',
            
            // Documents
            'application/pdf',
            'application/msword',
            'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
            'application/vnd.ms-excel',
            'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
            'application/vnd.ms-powerpoint',
            'application/vnd.openxmlformats-officedocument.presentationml.presentation',
            'application/vnd.oasis.opendocument.text',
            'application/vnd.oasis.opendocument.spreadsheet',
            'application/vnd.oasis.opendocument.presentation',
            'application/rtf',
            'text/csv',
            'application/xml',
            'application/json',
            'application/epub+zip',
            
            // Images
            'image/jpeg',
            'image/png',
            'image/gif',
            'image/bmp',
            'image/tiff',
            'image/webp',
            'image/svg+xml',
            'image/x-icon',
            'image/heic',
            'image/heif',
            
            // Videos
            'video/mp4',
            'video/x-msvideo',
            'video/x-matroska',
            'video/quicktime',
            'video/x-ms-wmv',
            'video/x-flv',
            'video/webm',
            'video/3gpp',
            'video/ogg',
            'video/mpeg',
            'video/x-ms-asf',
            
            // Audio
            'audio/mpeg',
            'audio/wav',
            'audio/flac',
            'audio/aac',
            'audio/ogg',
            'audio/x-ms-wma',
            'audio/mp4',
            'audio/opus',
            'audio/aiff',
            
            // Software
            'application/x-msdownload',
            'application/vnd.microsoft.portable-executable',
            'application/x-msi',
            'application/x-apple-diskimage',
            'application/vnd.debian.binary-package',
            'application/x-rpm',
            'application/vnd.android.package-archive',
            'application/octet-stream',
            
            // Development
            'application/java-archive',
            'application/x-python-wheel',
            'application/x-vsix',
            
            // Others
            'application/x-executable',
            'application/x-sharedlib',
            'application/x-object'
        ]);

        this.downloadablePatterns = [
            /\/download\//i,
            /\/dl\//i,
            /\/files\//i,
            /\/attachments\//i,
            /\?download/i,
            /\&download/i,
            /download=true/i,
            /attachment=true/i,
            /export=true/i,
            /\.zip$/i,
            /\.rar$/i,
            /\.pdf$/i,
            /\.exe$/i,
            /\.mp4$/i,
            /\.mp3$/i,
            /release.*download/i,
            /github\.com.*releases.*download/i,
            /sourceforge\.net.*download/i,
            /mediafire\.com/i,
            /mega\.nz/i,
            /drive\.google\.com.*download/i,
            /dropbox\.com.*dl=1/i,
            /onedrive\.live\.com.*download/i
        ];

        this.videoSitePatterns = [
            /youtube\.com\/watch/i,
            /youtu\.be\//i,
            /vimeo\.com\/\d+/i,
            /dailymotion\.com\/video/i,
            /twitch\.tv\/videos/i,
            /tiktok\.com\/.*\/video/i,
            /instagram\.com\/(p|tv|reel)\//i,
            /facebook\.com\/.*\/videos/i,
            /twitter\.com\/.*\/status/i,
            /soundcloud\.com\//i,
            /bandcamp\.com/i,
            /spotify\.com\/track/i
        ];
    }

    isDownloadableUrl(url) {
        if (!url) return false;

        try {
            const urlObj = new URL(url);
            const pathname = urlObj.pathname.toLowerCase();
            const search = urlObj.search.toLowerCase();
            const fullUrl = url.toLowerCase();

            // Check file extension
            const extension = this.getFileExtension(pathname);
            if (extension && this.downloadableExtensions.has(extension)) {
                return true;
            }

            // Check URL patterns
            if (this.downloadablePatterns.some(pattern => pattern.test(fullUrl))) {
                return true;
            }

            // Check video sites
            if (this.videoSitePatterns.some(pattern => pattern.test(fullUrl))) {
                return true;
            }

            return false;
        } catch {
            return false;
        }
    }

    isDownloadableMimeType(mimeType) {
        if (!mimeType) return false;
        
        const cleanMimeType = mimeType.toLowerCase().split(';')[0].trim();
        return this.downloadableMimeTypes.has(cleanMimeType) || 
               cleanMimeType.startsWith('application/') ||
               cleanMimeType.startsWith('video/') ||
               cleanMimeType.startsWith('audio/');
    }

    shouldInterceptDownload(url, mimeType, contentDisposition, size) {
        // Size threshold (100KB minimum by default)
        const minSize = 100 * 1024;
        
        // Always intercept if Content-Disposition suggests download
        if (contentDisposition && contentDisposition.toLowerCase().includes('attachment')) {
            return true;
        }

        // Check MIME type
        if (this.isDownloadableMimeType(mimeType)) {
            return true;
        }

        // Check URL patterns
        if (this.isDownloadableUrl(url)) {
            return true;
        }

        // Check file size (large files are likely downloads)
        if (size && size > minSize * 10) { // 1MB+
            return true;
        }

        return false;
    }

    getFileExtension(pathname) {
        if (!pathname) return null;
        
        const parts = pathname.split('.');
        if (parts.length < 2) return null;
        
        return parts[parts.length - 1].toLowerCase();
    }

    getFileTypeCategory(url, mimeType) {
        const extension = this.getFileExtension(new URL(url).pathname);
        
        if (!extension && !mimeType) return 'General';

        // Video files
        const videoExts = ['mp4', 'avi', 'mkv', 'mov', 'wmv', 'flv', 'webm', 'm4v'];
        if (videoExts.includes(extension) || (mimeType && mimeType.startsWith('video/'))) {
            return 'Videos';
        }

        // Audio files
        const audioExts = ['mp3', 'wav', 'flac', 'aac', 'ogg', 'wma', 'm4a'];
        if (audioExts.includes(extension) || (mimeType && mimeType.startsWith('audio/'))) {
            return 'Music';
        }

        // Documents
        const docExts = ['pdf', 'doc', 'docx', 'xls', 'xlsx', 'ppt', 'pptx', 'txt', 'rtf'];
        if (docExts.includes(extension) || 
            (mimeType && (mimeType.includes('pdf') || mimeType.includes('document') || mimeType.includes('text')))) {
            return 'Documents';
        }

        // Programs
        const progExts = ['exe', 'msi', 'dmg', 'pkg', 'deb', 'rpm', 'apk'];
        if (progExts.includes(extension) || (mimeType && mimeType.includes('executable'))) {
            return 'Programs';
        }

        // Archives
        const archiveExts = ['zip', 'rar', '7z', 'tar', 'gz'];
        if (archiveExts.includes(extension) || 
            (mimeType && (mimeType.includes('zip') || mimeType.includes('compressed')))) {
            return 'Archives';
        }

        // Images
        const imageExts = ['jpg', 'jpeg', 'png', 'gif', 'bmp', 'tiff', 'webp'];
        if (imageExts.includes(extension) || (mimeType && mimeType.startsWith('image/'))) {
            return 'Images';
        }

        return 'General';
    }

    isVideoSite(url) {
        return this.videoSitePatterns.some(pattern => pattern.test(url));
    }
}

// Export for use in other scripts
if (typeof module !== 'undefined' && module.exports) {
    module.exports = FileTypeDetector;
} else if (typeof window !== 'undefined') {
    window.FileTypeDetector = FileTypeDetector;
}