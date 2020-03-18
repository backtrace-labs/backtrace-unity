using UnityEngine.Networking;

namespace Backtrace.Unity.Model
{
#if UNITY_2018_4_OR_NEWER
    public class BacktraceSelfSSLCertificateHandler : CertificateHandler
    {
        private static readonly string PUB_KEY = string.Empty;

        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
#endif
}