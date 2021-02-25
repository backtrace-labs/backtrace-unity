using Backtrace.Unity.Types;
using System;
using UnityEngine;

namespace Backtrace.Unity.Model
{
    [Serializable]
    public class BacktraceClientConfiguration : ScriptableObject
    {
        public string ServerUrl;
        public int ReportPerMin;
        public bool HandleUnhandledExceptions = true;
        public bool IgnoreSslValidation = false;
        public bool DestroyOnLoad = true;
        public bool HandleANR = true;
        public bool OomReports = false;
        public int GameObjectDepth = 0;
        public MiniDumpType MinidumpType = MiniDumpType.None;

        public void UpdateServerUrl()
        {
            if (string.IsNullOrEmpty(ServerUrl))
            {
                return;
            }

            Uri tmp;
            var result = Uri.TryCreate(ServerUrl, UriKind.RelativeOrAbsolute, out tmp);
            if (result)
            {
                try
                {
                    //Parsed uri include http/https
                    ServerUrl = new UriBuilder(ServerUrl) { Scheme = Uri.UriSchemeHttps, Port = 6098 }.Uri.ToString();
                }
                catch (Exception)
                {
                    Debug.LogWarning("Invalid Backtrace URL");
                }
            }
        }

        public bool ValidateServerUrl()
        {
            if (!ServerUrl.Contains("backtrace.io") && !ServerUrl.Contains("submit.backtrace.io"))
            {
                return false;
            }

            Uri tmp;
            var result = Uri.TryCreate(ServerUrl, UriKind.RelativeOrAbsolute, out tmp);
            try
            {
                new UriBuilder(ServerUrl) { Scheme = Uri.UriSchemeHttps, Port = 6098 }.Uri.ToString();
            }
            catch (Exception)
            {
                return false;
            }
            return result;
        }

    }
}