using Backtrace.Unity.Model;
using System.Text;
using UnityEngine.Networking;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Extensions
{
    public static class UnityWebRequestExtensions
    {
        internal const string ContentTypeHeader = "Content-Type";
        internal static UnityWebRequest SetMultipartFormData(this UnityWebRequest source, byte[] boundaryId)
        {
            const string multipartContentTypePrefix = "multipart/form-data; boundary=";
            source.SetRequestHeader(ContentTypeHeader, string.Format("{0}{1}", multipartContentTypePrefix, Encoding.UTF8.GetString(boundaryId)));
            return source;
        }

        public static bool ReceivedNetworkError(this UnityWebRequest request)
        {
            return
#if UNITY_2020_2_OR_NEWER
                request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError;
#else
                request.isNetworkError || request.isHttpError;
#endif
        }

        internal static UnityWebRequest SetJsonContentType(this UnityWebRequest source)
        {
            const string contentTypeApplicationJson = "application/json";
            source.SetRequestHeader(ContentTypeHeader, contentTypeApplicationJson);
            return source;
        }

        internal static UnityWebRequest IgnoreSsl(this UnityWebRequest source, bool shouldIgnore)
        {

#if UNITY_2018_4_OR_NEWER

            if (shouldIgnore)
            {
                source.certificateHandler = new BacktraceSelfSSLCertificateHandler();
            }
#endif
            return source;
        }

    }
}
