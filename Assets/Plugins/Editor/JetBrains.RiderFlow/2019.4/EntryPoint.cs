using JetBrains.RiderFlow.Core.Launchers;
using JetBrains.RiderFlow.Core.ReEditor.Notifications;
using JetBrains.RiderFlow.Core.UI.SearchEverywhere;
using JetBrains.RiderFlow.Core.Utils;
using UnityEditor;

namespace JetBrains.RiderFlow.Since2019_4
{
    [InitializeOnLoad]
    public class DelayedEntryPoint
    {

        static DelayedEntryPoint()
        {
            SearchEverywhereWindow.Settings = new SearchWindowSettings();
            ProgressManagerOwner.ProgressManager = new LogProgressManager();
            UnityEditorUtils.ExecuteOnceOnUpdateCall(OnEnable);
        }
        
        protected static void OnEnable()
        {
            if (!IsPrimaryUnityProcess())
                return;

            Launcher.Run();
        }
        
        private static bool IsPrimaryUnityProcess()
        {
            return true;
        }
    }    
}
