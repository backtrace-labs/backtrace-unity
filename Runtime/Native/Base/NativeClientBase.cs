#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_WIN
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Extensions;
using System.Threading;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Runtime.Native.Base
{
    internal abstract class NativeClientBase
    {
        internal const string AnrMessage = "ANRException: Blocked thread detected.";
        protected const string HangType = "Hang";
        protected const string CrashType = "Crash";
        protected const string ErrorTypeAttribute = "error.type";

        protected int AnrWatchdogTimeout = BacktraceConfiguration.DefaultAnrWatchdogTimeout;
        /// <summary>
        /// Determine if ANR occurred and NativeClient should report ANR in breadcrumbs
        /// </summary>
        protected volatile bool LogAnr = false;

        // Last Backtrace client update time 
        protected volatile internal float LastUpdateTime;

        /// <summary>
        /// Determine if the ANR background thread should be disabled or not 
        /// for some period of time.
        /// This option will be used by the native client implementation
        /// once application goes to background/foreground
        /// </summary>
        internal volatile bool PreventAnr = false;

        /// <summary>
        /// Determine if ANR thread should exit
        /// </summary>
        internal volatile bool StopAnr = false;

        internal Thread AnrThread;

        /// <summary>
        /// Determine if Native client should capture native crashes
        /// </summary>
        protected bool CaptureNativeCrashes = false;

        /// <summary>
        /// Determine if Native client should handle ANRs
        /// </summary>
        protected bool HandlerANR = false;

        protected readonly BacktraceConfiguration _configuration;
        protected readonly BacktraceBreadcrumbs _breadcrumbs;

        private readonly bool _shouldLogAnrsInBreadcrumbs;

        private object _lockObject = new object();
        internal NativeClientBase(BacktraceConfiguration configuration, BacktraceBreadcrumbs breadcrumbs)
        {
            _configuration = configuration;
            _breadcrumbs = breadcrumbs;
            _shouldLogAnrsInBreadcrumbs = ShouldStoreAnrBreadcrumbs();
            AnrWatchdogTimeout = configuration.AnrWatchdogTimeout > 1000
                ? configuration.AnrWatchdogTimeout
                : BacktraceConfiguration.DefaultAnrWatchdogTimeout;
        }

        /// <summary>
        /// Update native client state
        /// </summary>
        /// <param name="time">Current game time</param>
        public void Update(float time)
        {
            LastUpdateTime = time;
            if (_shouldLogAnrsInBreadcrumbs && LogAnr)
            {
                if (Monitor.TryEnter(_lockObject))
                {
                    try
                    {
                        if (_shouldLogAnrsInBreadcrumbs && LogAnr)
                        {
                            _breadcrumbs.AddBreadcrumbs(AnrMessage, BreadcrumbLevel.System, UnityEngineLogLevel.Warning);
                            LogAnr = false;
                        }
                    }
                    finally
                    {
                        Monitor.Exit(_lockObject);
                    }

                }
            }
        }

        /// <summary>
        /// Invoke code when ANR was detected
        /// </summary>
        internal void OnAnrDetection()
        {
            LogAnr = _shouldLogAnrsInBreadcrumbs;
        }
        /// <summary>
        /// Pause ANR detection
        /// </summary>
        /// <param name="stopAnr">True - if native client should pause ANR detection"</param>
        public void PauseAnrThread(bool stopAnr)
        {
            PreventAnr = stopAnr;
        }

        public virtual void Disable()
        {
            if (AnrThread != null)
            {
                StopAnr = true;
            }
        }

        /// <summary>
        /// Determine if native client should store ANR breadcrumbs
        /// </summary>
        /// <returns>True, if client should store ANR breadcrumbs. Otherwise false.</returns>
        private bool ShouldStoreAnrBreadcrumbs()
        {
            if (_breadcrumbs == null)
            {
                return false;
            }
            return _breadcrumbs.BreadcrumbsLevel.HasFlag(BacktraceBreadcrumbType.System) && _breadcrumbs.UnityLogLevel.HasFlag(UnityEngineLogLevel.Warning);
        }
    }
}
#endif