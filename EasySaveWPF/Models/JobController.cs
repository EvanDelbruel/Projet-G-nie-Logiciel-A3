using System.Threading;

namespace EasySaveWPF.Models
{
    // Technical object (not saved in JSON) acting as a remote control for an active thread
    public class JobController
    {
        // Manages permanent cancellation (STOP button)
        public CancellationTokenSource CancelTokenSource { get; set; } = new CancellationTokenSource();

        // Manages pausing (PLAY / PAUSE buttons). Initialized to "true" (the gate is open, execution proceeds)
        public ManualResetEvent PauseEvent { get; set; } = new ManualResetEvent(true);
    }
}