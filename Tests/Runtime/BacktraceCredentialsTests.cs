using Backtrace.Unity.Model;
using NUnit.Framework;
using System;

namespace Backtrace.Unity.Tests.Runtime
{
    internal class BacktraceCredentialsTests
    {
        [TestCase("https://www.submit.backtrace.io")]
        [TestCase("http://www.submit.backtrace.io")]
        [TestCase("https://submit.backtrace.io")]
        [TestCase("https://submit.backtrace.io/12312/312312/")]
        [TestCase("https://submit.backtrace.io/uri/")]
        [TestCase("https://submit.backtrace.io/uri?sumbissionToken=123123134&value=123123/")]
        [TestCase("http://submit.backtrace.io")]
        [TestCase("http://submit.backtrace.io/")]
        public void GenerateBacktraceSubmitUrl_FromSubmitUrl_ValidSubmissionUrl(string host)
        {
            var credentials = new BacktraceCredentials(host);
            if (!host.StartsWith("https://") && !host.StartsWith("http://"))
            {
                host = string.Format("https://{0}", host);
            }

            if (!host.EndsWith("/"))
            {
                host += '/';
            }
            Assert.AreEqual(host, credentials.GetSubmissionUrl().ToString());
        }

        [TestCase("")]
        [TestCase("not url")]
        [TestCase("123123...")]
        [Test(Author = "Konrad Dysput", Description = "Test invalid api url")]
        public void ThrowInvalidUrlException_FromInvalidUrl_ThrowException(string host)
        {
            Assert.Throws<UriFormatException>(() => new BacktraceCredentials(host));
        }

    }
}
