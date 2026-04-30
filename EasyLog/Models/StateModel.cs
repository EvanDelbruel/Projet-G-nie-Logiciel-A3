namespace EasyLog.Models
{
    public class StateModel
    {
        // Name of the associated backup job
        public string Name { get; set; }

        // Timestamp indicating when the last action was performed
        public string LastActionTimestamp { get; set; }

        // Current execution status (e.g., "Active" while processing, "Inactive" when idle)
        public string State { get; set; }

        // Total number of files eligible for backup in this job
        public int TotalFilesToCopy { get; set; }

        // Total cumulative size of the files to be copied, in bytes
        public long TotalFilesSize { get; set; }

        // Number of files still waiting to be processed
        public int NbFilesLeftToDo { get; set; }

        // Overall completion progress percentage (from 0 to 100)
        public int Progression { get; set; }

        // Absolute path of the source file currently being read
        public string CurrentSourceFile { get; set; }

        // Absolute path of the target destination file currently being written
        public string CurrentTargetFile { get; set; }
    }
}