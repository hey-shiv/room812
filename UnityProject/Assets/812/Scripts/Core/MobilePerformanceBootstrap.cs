using UnityEngine;

namespace Room812.Core
{
    /// <summary>
    /// Sets mobile-first runtime defaults: 60 FPS target, no VSync stall, Mobile quality tier
    /// on handhelds, screen awake while playing.
    /// </summary>
    public class MobilePerformanceBootstrap : MonoBehaviour
    {
        public int targetFps = 60;
        public int mobileQualityLevel = 0;

        void Awake()
        {
            Application.targetFrameRate = targetFps;
            QualitySettings.vSyncCount = 0;
            if (Application.isMobilePlatform)
                QualitySettings.SetQualityLevel(mobileQualityLevel, true);
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
