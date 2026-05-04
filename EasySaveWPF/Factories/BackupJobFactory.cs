using EasySaveWPF.Models;

namespace EasySaveWPF.Factories
{
    // Design Pattern Factory
    public static class BackupJobFactory
    {
        public static BackupJob CreateJob(string name, string source, string target, string type)
        {

            return new BackupJob(name, source, target, type);
        }
    }
}