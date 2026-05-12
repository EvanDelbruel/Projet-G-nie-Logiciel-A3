using System.Threading;

namespace EasySaveWPF.Services
{
    // Global synchronization manager for handling parallel backup constraints
    public static class SyncManager
    {
        // Semaphore allowing ONLY ONE large file transfer at a time globally
        public static readonly SemaphoreSlim LargeFileSemaphore = new SemaphoreSlim(1, 1);

        // Semaphore ensuring CryptoSoft is ONLY executed by one thread at a time (Mono-instance constraint)
        public static readonly SemaphoreSlim CryptoSemaphore = new SemaphoreSlim(1, 1);

        // Tracks the total number of priority files currently waiting or processing across ALL jobs
        private static int _priorityFilesCount = 0;
        private static readonly object _priorityLock = new object();

        // Safely increments the priority file counter
        public static void AddPriorityFile()
        {
            lock (_priorityLock) _priorityFilesCount++;
        }

        // Safely decrements the priority file counter
        public static void RemovePriorityFile()
        {
            lock (_priorityLock)
            {
                if (_priorityFilesCount > 0) _priorityFilesCount--;
            }
        }

        // Checks if any non-priority thread must yield and wait
        public static bool ArePriorityFilesWaiting()
        {
            lock (_priorityLock) return _priorityFilesCount > 0;
        }
    }
}