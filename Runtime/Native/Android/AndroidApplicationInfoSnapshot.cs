#if UNITY_ANDROID || UNITY_EDITOR
#if UNITY_ANDROID
using UnityEngine;
#endif

namespace Backtrace.Unity.Runtime.Native.Android
{
    /// <summary>
    /// Immutable view of the ApplicationInfo fields the native-library path resolver needs.
    /// A plain data class keeps the resolver pure and testable in the Editor; the JNI reads live only in <see cref="Capture"/>.
    /// </summary>
    internal sealed class AndroidApplicationInfoSnapshot
    {
        internal string SourceDir;
        internal string PublicSourceDir;
        internal string NativeLibraryDir;
        internal string[] SplitSourceDirs;
        internal string[] SplitPublicSourceDirs;
        internal string[] SplitNames;

#if UNITY_ANDROID
        /// <summary>
        /// Populates the snapshot from the current application context.
        /// Missing pieces (for example a host without a Unity activity, or splitNames below API 26) leave the corresponding fields null; the resolver treats every field as optional.
        /// </summary>
        internal static AndroidApplicationInfoSnapshot Capture()
        {
            var snapshot = new AndroidApplicationInfoSnapshot();
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                if (unityPlayer == null)
                {
                    return snapshot;
                }
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    if (activity == null)
                    {
                        return snapshot;
                    }
                    using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                    using (var applicationInfo = context.Call<AndroidJavaObject>("getApplicationInfo"))
                    {
                        snapshot.SourceDir = TryGetString(applicationInfo, "sourceDir");
                        snapshot.PublicSourceDir = TryGetString(applicationInfo, "publicSourceDir");
                        snapshot.NativeLibraryDir = TryGetString(applicationInfo, "nativeLibraryDir");
                        snapshot.SplitSourceDirs = TryGetStringArray(applicationInfo, "splitSourceDirs");
                        snapshot.SplitPublicSourceDirs = TryGetStringArray(applicationInfo, "splitPublicSourceDirs");
                        // splitNames exists only from API 26; on older platforms the field read throws.
                        snapshot.SplitNames = TryGetStringArray(applicationInfo, "splitNames");
                    }
                }
            }
            return snapshot;
        }

        private static string TryGetString(AndroidJavaObject applicationInfo, string fieldName)
        {
            try
            {
                return applicationInfo.Get<string>(fieldName);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        private static string[] TryGetStringArray(AndroidJavaObject applicationInfo, string fieldName)
        {
            try
            {
                return applicationInfo.Get<string[]>(fieldName);
            }
            catch (System.Exception)
            {
                return null;
            }
        }
#endif
    }
}
#endif
