namespace EasyLog.Models
{
    public class StateModel
    {
        // Name of the current backup job
        public string Name { get; set; }

        // Timestamp of the last action
        public string LastActionTimestamp { get; set; }

        // Current status ("Active" during copy, "Inactive" when finished)
        public string State { get; set; }

        // Total number of files detected at start
        public int TotalFilesToCopy { get; set; }

        // Total cumulative size of all files (in bytes)
        public long TotalFilesSize { get; set; }

        // Number of files remaining to copy
        public int NbFilesLeftToDo { get; set; }

        // Progress percentage (from 0 to 100)
        public int Progression { get; set; }

        // Path of the file currently being read
        public string CurrentSourceFile { get; set; }

        // Path of the file currently being written
        public string CurrentTargetFile { get; set; }
    }
}