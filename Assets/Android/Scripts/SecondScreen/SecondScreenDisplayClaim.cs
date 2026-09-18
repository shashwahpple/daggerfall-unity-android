using UnityEngine;

namespace DaggerfallWorkshop
{
    /// <summary>
    /// Claims the AYN Thor's Display 2 in Android's window model so tapping it no longer hands
    /// window/controller focus to Android's SECONDARY_HOME resolution - see
    /// SecondScreenOwnerActivity.java and UnityPresentationFocusPatcher.java (Assets/Plugins/Android)
    /// for the native half of the fix. Fixes
    /// https://github.com/shashwahpple/daggerfall-unity-android/issues/1.
    /// </summary>
    public static class SecondScreenDisplayClaim
    {
        public static void Claim()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var displayClaim = new AndroidJavaClass("com.dfworkshop.daggerfallunityandroid.SecondScreenDisplayClaim"))
            {
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                displayClaim.CallStatic("claim", currentActivity);
            }
#endif
        }
    }
}
