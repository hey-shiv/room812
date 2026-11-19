using System.Runtime.InteropServices;
using UnityEngine;

namespace Room812.Core
{
    /// <summary>
    /// iOS Core Haptics via a tiny native plugin; Android uses Handheld.Vibrate() instead.
    /// </summary>
    public static class NativeHaptics
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void Room812_PulseHaptic(int style);
#endif

        public static void Pulse(int style = 1)
        {
#if UNITY_IOS && !UNITY_EDITOR
            Room812_PulseHaptic(style);
#elif UNITY_ANDROID || UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
