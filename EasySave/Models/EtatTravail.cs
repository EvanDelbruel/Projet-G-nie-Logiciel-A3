using System;

namespace EasySave.Models
{
    // classe pour le fichier state.json
    public class EtatTravail
    {
        public string Name { get; set; }
        public string SourceFilePath { get; set; }
        public string TargetFilePath { get; set; }
        public string State { get; set; } // active ou end
        public int TotalFilesToCopy { get; set; }
        public long TotalFilesSize { get; set; }
        public int NbFilesLeftToDo { get; set; }
        public int Progression { get; set; }
        public long SizeFilesLeftToDo { get; set; } // taille restante
        public string LastActionTime { get; set; } // horodatage
    }
}