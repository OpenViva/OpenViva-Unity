using JetBrains.RiderFlow.Core.ReEditor.Notifications;

namespace JetBrains.RiderFlow.Since2019_4
{
    public class LogProgressManager : IProgressManager
    {
        public int CreateProgress(string name, string description = null)
        {
            //nope
            return -1;
        }

        public void ReportProgress(int id, float progressValue, string description)
        {
            // nop
        }

        public void FinishProgress(int id)
        {
            // nop
        }
    }
}