using System;

namespace Backtrace.Unity.Model
{
    /// <summary>
    /// Bactrace credentials informations
    /// </summary>
    public class BacktraceCredentials
    {

        /// <summary>
        /// Get a Uri to Backtrace servcie
        /// </summary>
        public Uri BacktraceHostUri { get; private set; }

        /// <summary>
        /// Create submission url to Backtrace API
        /// </summary>
        public Uri GetSubmissionUrl()
        {

            var uriBuilder = new UriBuilder(BacktraceHostUri);
            if (!uriBuilder.Scheme.StartsWith("http"))
            {
                uriBuilder.Scheme = string.Format("https://{0}", uriBuilder.Scheme);
            }
            return uriBuilder.Uri;
        }

        public Uri GetPlCrashReporterSubmissionUrl()
        {
            var url = GetSubmissionUrl().ToString();
            var plCrashReporterUrl = url.IndexOf("submit.backtrace.io") != -1
                ? url.Replace("/json", "/plcrash")
                : url.Replace("format=json", "format=plcrash");
            var uriBuilder = new UriBuilder(plCrashReporterUrl);
            return uriBuilder.Uri;
        }

        /// <summary>
        /// Create minidump submission url to Backtrace API
        /// </summary>
        public Uri GetMinidumpSubmissionUrl()
        {
            var url = GetSubmissionUrl().ToString();
            var minidumpUrl = url.IndexOf("submit.backtrace.io") != -1
                ? url.Replace("/json", "/minidump")
                : url.Replace("format=json", "format=minidump");
            var uriBuilder = new UriBuilder(minidumpUrl);
            return uriBuilder.Uri;
        }

        /// <summary>
        /// Create symbols submission url to Backtrace
        /// </summary>
        /// <returns></returns>
        public Uri GetSymbolsSubmissionUrl(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("Empty symbols submission token");
            }
            var url = GetSubmissionUrl().ToString();

            if (url.IndexOf("submit.backtrace.io") != -1)
            {
                url = url.Replace("/json", "/symbols");
                var endIndex = url.LastIndexOf("/");
                var startIndex = endIndex - 64;
                var submissionToken = url.Substring(startIndex, 64);
                url = url.Replace(submissionToken, token);

            }
            else
            {
                url = url.Replace("format=json", "format=symbols");
                const string tokenPrefix = "token=";
                var tokenIndex = url.IndexOf(tokenPrefix);
                if (tokenIndex == -1)
                {
                    throw new ArgumentException("Missing token in Backtrace url");
                }
                var submissionToken = url.Substring(tokenIndex + tokenPrefix.Length, 64);
                url = url.Replace(submissionToken, token);
            }

            var uriBuilder = new UriBuilder(url);
            return uriBuilder.Uri;
        }
        /// <summary>
        /// Initialize Backtrace credentials with Backtrace submit url. 
        /// If you pass backtraceSubmitUrl you have to make sure url to API is valid and contains token
        /// </summary>
        /// <param name="backtraceSubmitUrl">Backtrace submit url</param>
        public BacktraceCredentials(string backtraceSubmitUrl)
            : this(new Uri(backtraceSubmitUrl))
        { }

        /// <summary>
        /// Initialize Backtrace credentials with Backtrace submit url. 
        /// If you pass backtraceSubmitUrl you have to make sure url to API is valid and contains token
        /// </summary>
        /// <param name="backtraceSubmitUrl">Backtrace submit url</param>
        public BacktraceCredentials(Uri backtraceSubmitUrl)
        {
            BacktraceHostUri = backtraceSubmitUrl;
        }
        /// <summary>
        /// Check if model passed to constructor is valid
        /// </summary>
        /// <param name="uri">Backtrace service uri</param>
        /// <param name="token">Access token to Backtrace services</param>
        /// <returns>validation result</returns>
        internal bool IsValid(Uri uri, byte[] token)
        {
            return token != null && token.Length > 0 && uri.IsWellFormedOriginalString();
        }
    }
}