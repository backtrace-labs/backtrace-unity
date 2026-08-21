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
        /// Populates the snapshot from the current application context. The whole operation is
        /// fail-safe: a missing Unity activity (for example a Flutter host), an unavailable
        /// context, or any JNI failure returns an empty snapshot instead of throwing, so an
        /// application-metadata problem can never prevent the authoritative linker path from
        /// being used. splitNames is read only on API 26+, where the field exists.
        /// </summary>
        internal static AndroidApplicationInfoSnapshot Capture()
        {
            var snapshot = new AndroidApplicationInfoSnapshot();
            try
            {
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
                        {
                            if (context == null)
                            {
                                return snapshot;
                            }
                            using (var applicationInfo = context.Call<AndroidJavaObject>("getApplicationInfo"))
                            {
                                if (applicationInfo == null)
                                {
                                    return snapshot;
                                }
                                snapshot.SourceDir = TryGetString(applicationInfo, "sourceDir");
                                snapshot.PublicSourceDir = TryGetString(applicationInfo, "publicSourceDir");
                                snapshot.NativeLibraryDir = TryGetString(applicationInfo, "nativeLibraryDir");
                                snapshot.SplitSourceDirs = TryGetStringArray(applicationInfo, "splitSourceDirs");
                                snapshot.SplitPublicSourceDirs =
                                    TryGetStringArray(applicationInfo, "splitPublicSourceDirs");
                                // API-level guard instead of relying on the caught NoSuchFieldError:
                                // the field exists only from API 26.
                                if (GetSdkVersion() >= 26)
                                {
                                    snapshot.SplitNames = TryGetStringArray(applicationInfo, "splitNames");
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                return snapshot;
            }
            return snapshot;
        }

        private static int GetSdkVersion()
        {
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    return version.GetStatic<int>("SDK_INT");
                }
            }
            catch (System.Exception)
            {
                return 0;
            }
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
