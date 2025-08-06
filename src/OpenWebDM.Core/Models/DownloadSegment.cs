namespace OpenWebDM.Core.Models
{
    public class DownloadSegment
    {
        public int Id { get; set; }
        public long StartByte { get; set; }
        public long EndByte { get; set; }
        public long CurrentByte { get; set; }
        public SegmentStatus Status { get; set; }
        public string TempFilePath { get; set; } = string.Empty;
        public CancellationTokenSource? CancellationTokenSource { get; set; }
        public long DownloadedBytes => CurrentByte - StartByte;
        public double Progress => EndByte > StartByte ? (double)(CurrentByte - StartByte) / (EndByte - StartByte) * 100 : 0;
        public bool IsCompleted => CurrentByte >= EndByte;
    }

    public enum SegmentStatus
    {
        Pending,
        Downloading,
        Completed,
        Failed,
        Paused
    }
}