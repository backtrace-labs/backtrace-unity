using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Backtrace.Unity.Model.Breadcrumbs
{
    internal sealed class BacktraceBreadcrumbsEventHandler
    {
        public bool HasRegisteredEvents { get; set; } = false;
        private readonly BacktraceBreadcrumbs _breadcrumbs;
        private BacktraceBreadcrumbType _registeredLevel;
        private Thread _thread;
        public BacktraceBreadcrumbsEventHandler(BacktraceBreadcrumbs breadcrumbs)
        {
            _thread = Thread.CurrentThread;
            _breadcrumbs = breadcrumbs;
        }
        /// <summary>
        /// Register unity events that will generate logs in the breadcrumbs file
        /// </summary>
        /// <param name="level">Breadcrumbs level</param>
        public void Register(BacktraceBreadcrumbType level)
        {
            _registeredLevel = level;
            HasRegisteredEvents = level.HasFlag(BacktraceBreadcrumbType.System);
            if (!HasRegisteredEvents)
            {

                return;
            }
            SceneManager.activeSceneChanged += HandleSceneChanged;
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
            Application.lowMemory += HandleLowMemory;
            Application.quitting += HandleApplicationQuitting;
            Application.focusChanged += Application_focusChanged;
            Application.logMessageReceived += HandleMessage;
            Application.logMessageReceivedThreaded += HandleBackgroundMessage;
        }

        /// <summary>
        /// Unregister Unity breadcrumbs events
        /// </summary>
        public void Unregister()
        {
            if (HasRegisteredEvents)
            {
                return;
            }
            SceneManager.activeSceneChanged -= HandleSceneChanged;
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
            SceneManager.sceneUnloaded -= SceneManager_sceneUnloaded;
            Application.lowMemory -= HandleLowMemory;
            Application.quitting -= HandleApplicationQuitting;
            Application.logMessageReceived -= HandleMessage;
            Application.logMessageReceivedThreaded -= HandleBackgroundMessage;
            Application.focusChanged -= Application_focusChanged;
        }

        private void SceneManager_sceneUnloaded(Scene scene)
        {
            var message = string.Format("SceneManager:scene {0} unloaded", scene.name);
            Log(message, LogType.Assert);
        }

        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            var message = string.Format("SceneManager:scene {0} loaded", scene.name);
            Log(message, LogType.Assert, new Dictionary<string, string>() { { "LoadSceneMode", loadSceneMode.ToString() } });
        }

        private void HandleSceneChanged(Scene sceneFrom, Scene sceneTo)
        {
            var message = string.Format("SceneManager:scene changed from {0} to {1}", sceneFrom.name, sceneTo.name);
            Log(message, LogType.Assert);
        }

        private void HandleLowMemory()
        {
            Log("Application:low memory", LogType.Warning);
        }

        private void HandleApplicationQuitting()
        {
            Log("Application:quitting", LogType.Log);
        }

        private void HandleBackgroundMessage(string condition, string stackTrace, LogType type)
        {
            // validate if a message is from main thread
            // and skip messages from main thread
            if (Thread.CurrentThread == _thread)
            {
                return;
            }
            HandleMessage(condition, stackTrace, type);
        }

        private void HandleMessage(string condition, string stackTrace, LogType type)
        {
            Log(condition, type, new Dictionary<string, string> { { "stackTrace", stackTrace } });
        }

        private void Application_focusChanged(bool hasFocus)
        {
            Log("Application:focus changed.", LogType.Assert, new Dictionary<string, string> { { "hasFocus", hasFocus.ToString() } });
        }

        private void Log(string message, LogType level, IDictionary<string, string> attributes = null)
        {
            var type = _breadcrumbs.ConvertLogTypeToLogLevel(level);
            if (!_breadcrumbs.ShouldLog(type))
            {
                return;
            }
            _breadcrumbs.AddBreadcrumbs(message, BreadcrumbLevel.System, type, attributes);
        }
    }
}
