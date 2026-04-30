namespace EasyLog.Models
{
    public class LogModel
    {
        // Name of the executed backup job
        public string Name { get; set; }

        // Absolute path of the source file (UNC format)
        public string SourceFilePath { get; set; }

        // Absolute path of the target destination file (UNC format)
        public string TargetFilePath { get; set; }

        // Size of the processed file in bytes
        public long FileSize { get; set; }

        // Time taken to transfer the file in milliseconds
        public double FileTransferTime { get; set; }

        // Time taken to encrypt the file in milliseconds (0 if unencrypted, -1 if encryption failed)
        public double FileEncryptTime { get; set; }

        // Timestamp indicating when the operation occurred
        public string Time { get; set; }
    }
}