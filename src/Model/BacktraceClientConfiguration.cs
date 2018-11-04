using System;
using UnityEngine;

namespace Backtrace.Unity.Model
{
    [Serializable]
    public class BacktraceClientConfiguration : ScriptableObject
    {
        public string ServerUrl;
        public string Token;
        public int ReportPerMin;

        public void UpdateServerUrl()
        {
            if (string.IsNullOrEmpty(ServerUrl))
            {
                return;
            }
            if (!ServerUrl.Contains(".sp.backtrace.io"))
            {
                ServerUrl += ".sp.backtrace.io";
                Debug.Log("After change server URL: " + ServerUrl);
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
                    Debug.Log("Invalid uri provided");
                }
            }
        }

        public bool ValidateServerUrl()
        {
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
            return ValidateServerUrl() && ValidateToken();
        }
        public bool ValidateToken()
        {
            return !(string.IsNullOrEmpty(Token) || Token.Length != 64);
        }

        public BacktraceCredentials ToCredentials()
        {
            return new BacktraceCredentials(ServerUrl, Token);
        }
    }
}