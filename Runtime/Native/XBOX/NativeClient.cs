#if UNITY_GAMECORE_XBOXSERIES
using Backtrace.Unity.Interfaces;
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Runtime.Native.Base;
using Backtrace.Unity.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Runtime.Native.XBOX
{

    internal class NativeClient : NativeClientBase, INativeClient
    {
        [DllImport("backtrace_native_xbox_mt.dll")]
        private static extern bool BacktraceCrash();

        [DllImport("backtrace_native_xbox_mt.dll")]
        private static extern bool BacktraceAddAttribute(
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            [MarshalAs(UnmanagedType.LPWStr)] string value
        );

        [DllImport("backtrace_native_xbox_mt.dll")]
        private static extern bool BacktraceAddFile(
            [MarshalAs(UnmanagedType.LPWStr)] string name,
            [MarshalAs(UnmanagedType.LPWStr)] string path
        );

        [DllImport("backtrace_native_xbox_mt.dll")]
        private static extern bool BacktraceSetUrl(
            [MarshalAs(UnmanagedType.LPWStr)] string url
        );

        [DllImport("backtrace_native_xbox_mt.dll")]
        private static extern bool BacktraceNativeInitialize();

        /// <summary>
        /// Determine if the XBOX integration should be enabled
        /// </summary>
        private bool _enabled =
#if UNITY_GAMECORE_XBOXSERIES && !UNITY_EDITOR
            true;
#else
            false;
#endif

        public NativeClient(BacktraceConfiguration configuration, BacktraceBreadcrumbs breadcrumbs, IDictionary<string, string> clientAttributes, IEnumerable<string> attachments) : base(configuration, breadcrumbs)
        {
            if (!_enabled)
            {
                return;
            }
            AddScopedAttributes(clientAttributes);
            HandleNativeCrashes(clientAttributes, attachments);
        }

        public void GetAttributes(IDictionary<string, string> attributes)
        {
        }

        public void HandleAnr()
        {
        }

        public bool OnOOM()
        {
            return false;
        }

        public void SetAttribute(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }
            // avoid null reference in crashpad source code
            if (value == null)
            {
                value = string.Empty;
            }
            BacktraceAddAttribute(key, value);
        }

        private void AddScopedAttributes(IDictionary<string, string> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (string.IsNullOrEmpty(attribute.Key) || attribute.Value == null)
                {
                    continue;
                }

                BacktraceAddAttribute(attribute.Key, attribute.Value);
            }
        }

        private void HandleNativeCrashes(IDictionary<string, string> clientAttributes, IEnumerable<string> attachments)
        {
            var integrationDisabled = !_configuration.CaptureNativeCrashes || !_configuration.Enabled;
            if (integrationDisabled)
            {
                return;
            }

            var minidumpUrl = new BacktraceCredentials(_configuration.GetValidServerUrl()).GetMinidumpSubmissionUrl().ToString();
            BacktraceSetUrl(minidumpUrl);

            foreach (var attachment in attachments)
            {
                var name = Path.GetFileName(attachment);
                BacktraceAddFile(name, attachment);
            }

            CaptureNativeCrashes = BacktraceNativeInitialize();

            if (!CaptureNativeCrashes)
            {
                Debug.LogWarning("Backtrace native integration status: Cannot initialize the Native Crash Reporting client");
                return;
            }
        }
    }
}
#endif