namespace EasyLog.Models
{
    public class LogModel
    {
        // Name of the backup job
        public string Name { get; set; }

        // Exact original file path (UNC format)
        public string SourceFilePath { get; set; }

        // Exact copied file path (UNC format)
        public string TargetFilePath { get; set; }

        // Size of the copied file (in bytes)
        public long FileSize { get; set; }

        // Transfer time (in milliseconds)
        public double FileTransferTime { get; set; }

        // Timestamp of the transfer
        public string Time { get; set; }
    }
}