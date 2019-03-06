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

        public void UpdateServerUrl()
        {
            if (string.IsNullOrEmpty(ServerUrl))
            {
                return;
            }

            Uri serverUri;
            var result = Uri.TryCreate(ServerUrl, UriKind.RelativeOrAbsolute, out serverUri);
            if (result)
            {
                try
                {
                    //Parsed uri include http/https
                    ServerUrl = new UriBuilder(ServerUrl) { Scheme = Uri.UriSchemeHttps, Port = 6098 }.Uri.ToString();
                }
                catch (Exception)
                {
                    Debug.LogWarning("Invalid uri provided");
                }
            }
        }

        public bool ValidateServerUrl()
        {
            if (!ServerUrl.Contains(".sp.backtrace.io") && !ServerUrl.Contains("submit.backtrace.io"))
            {
                return false;
            }

            Uri serverUri;
            var result = Uri.TryCreate(ServerUrl, UriKind.RelativeOrAbsolute, out serverUri);
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
        
        public bool IsValid()
        {
            return ValidateServerUrl();
        }
      
        public BacktraceCredentials ToCredentials()
        {
            return new BacktraceCredentials(ServerUrl);
        }
    }
}