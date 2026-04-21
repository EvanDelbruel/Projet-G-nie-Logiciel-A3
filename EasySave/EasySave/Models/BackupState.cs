using System;

namespace EasySave.Models
{
    // L'état de la sauvegarde (Actif ou Inactif)
    public enum StateStatus
    {
        Active,
        Inactive
    }

    public class BackupState
    {
        public string Name { get; set; }
        public string Timestamp { get; set; }
        public StateStatus State { get; set; }
        public int TotalFilesToCopy { get; set; }
        public long TotalFilesSize { get; set; }
        public int NbFilesLeftToDo { get; set; }
        public int Progression { get; set; }
        public long RemainingFilesSize { get; set; }
        public string CurrentSourceFilePath { get; set; }
        public string CurrentTargetFilePath { get; set; }

        public BackupState()
        {
            // Initialisation par défaut
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            State = StateStatus.Inactive;
            CurrentSourceFilePath = string.Empty;
            CurrentTargetFilePath = string.Empty;
            TotalFilesToCopy = 0;
            TotalFilesSize = 0;
            NbFilesLeftToDo = 0;
            RemainingFilesSize = 0;
            Progression = 0;
        }
    }
}