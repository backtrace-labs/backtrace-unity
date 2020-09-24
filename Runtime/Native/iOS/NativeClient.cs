using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Backtrace.Unity.Model;

namespace Backtrace.Unity.Runtime.Native.iOS
{
    /// <summary>
    /// iOS native client 
    /// </summary>
    public class NativeClient : INativeClient
    {

        [DllImport("__Internal", EntryPoint = "GetAttributes")]
        private static extern string GetiOSAttributes(float[] memoryUsed);

        [DllImport("__Internal", EntryPoint = "StartBacktraceIntegration")]
        private static extern void Start(string plCrashReporterUrl);


        [DllImport("__Internal", EntryPoint = "SuperCrashInWrapper")]
        public static extern string Crash();

        /// <summary>
        /// Determine if ios integration should be enabled
        /// </summary>
        private readonly bool _enabled =
#if UNITY_IOS && !UNITY_EDITOR
            true;
#else
            false;
#endif
        public NativeClient(string gameObjectName, BacktraceConfiguration configuration)
        {

            var plcrashreporterUrl = new BacktraceCredentials(configuration.GetValidServerUrl()).GetPlCrashReporterSubmissionUrl().ToString();
            Start(plcrashreporterUrl);
        }

        /// <summary>
        /// Retrieve Backtrace Attributes from the Android native code.
        /// </summary>
        /// <returns>Backtrace Attributes from the Android build</returns>
        public Dictionary<string, string> GetAttributes()
        {
            var result = new Dictionary<string, string>();            
            if (!_enabled)
            {
                return result;
            }
            float[] attributes = new float[] { 0,0,0,0,0,0};
            GetiOSAttributes(attributes);

            result.Add("mem.used", attributes[0].ToString());
            result.Add("mem.free", attributes[1].ToString());
            result.Add("mem.total", attributes[2].ToString());

            result.Add("cpu.count.active", attributes[3].ToString());
            result.Add("resident.size", attributes[4].ToString());
            result.Add("virtual size", attributes[5].ToString());
            return result;
            
        }

        /// <summary>
        /// Setup Android ANR support and set callback function when ANR happened.
        /// </summary>
        /// <param name="gameObjectName">Backtrace game object name</param>
        /// <param name="callbackName">Callback function name</param>
        public void HandleAnr(string gameObjectName, string callbackName)
        {
            throw new NotImplementedException();
        }

        public void SetAttribute(string key, string value)
        {
            throw new NotImplementedException();
        }
    }
}