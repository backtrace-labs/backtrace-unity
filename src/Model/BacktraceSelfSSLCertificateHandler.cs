using UnityEngine.Networking;

namespace Backtrace.Unity.Model
{
    public class BacktraceSelfSSLCertificateHandler : CertificateHandler
    {
        private static readonly string PUB_KEY = string.Empty;

        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
}