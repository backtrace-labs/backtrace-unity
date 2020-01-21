using Backtrace.Unity.Types;
using System.Linq;
using System.Collections.Generic;
using Backtrace.Unity.Model.JsonData;
using System.Text;
using System.Security.Cryptography;
using System;
using Backtrace.Newtonsoft.Linq;

namespace Backtrace.Unity.Model
{
    internal class DeduplicationModel
    {
        private readonly BacktraceData _backtraceData;
        private readonly DeduplicationStrategy _strategy;
        public DeduplicationModel(
            BacktraceData backtraceData,
            DeduplicationStrategy strategy)
        {
            _backtraceData = backtraceData;
            _strategy = strategy;
        }
        public string[] StackTrace
        {
            get
            {
                if (_strategy == DeduplicationStrategy.None)
                {
                    return new string[0];
                }
                if (_backtraceData.Report == null || _backtraceData.Report.DiagnosticStack == null)
                {
                    System.Diagnostics.Debug.WriteLine("Report or diagnostic stack is null");
                }
                var result = _backtraceData.Report.DiagnosticStack
                    .Select(n => n.FunctionName)
                    .OrderByDescending(n => n);

                return new HashSet<string>(result).ToArray();
            }
        }
        public string[] Classifier
        {
            get
            {
                if ((_strategy & DeduplicationStrategy.Classifier) == 0)
                {
                    return new string[0];
                }
                return _backtraceData.Classifier;
            }
        }
        public string ExceptionMessage
        {
            get
            {
                if ((_strategy & DeduplicationStrategy.Message) == 0)
                {
                    return string.Empty;
                }
                return _backtraceData.Report.Message;
            }
        }
        public string Application
        {
            get
            {
                if ((_strategy & DeduplicationStrategy.LibraryName) == 0)
                {
                    return string.Empty;
                }
                string key = BacktraceAttributes.APPLICATION_ATTRIBUTE_NAME;
                return _backtraceData.Attributes?.Attributes[key] as string;
            }
        }
        public string Factor
        {
            get
            {
                return _backtraceData.Report.Factor;
            }
        }

        public string GetSha()
        {
            if (!string.IsNullOrEmpty(_backtraceData.Report.Fingerprint))
            {
                return _backtraceData.Report.Fingerprint;
            }

            string json = new BacktraceJObject
            {
                ["Application"] = Application,
                ["ExceptionMessage"] = ExceptionMessage,
                ["Factor"] = Factor,
                ["Classifier"] = new JArray(Classifier),
                ["StackTrace"] = new JArray(StackTrace)
            }.ToString();

            using (var sha1 = new SHA1Managed())
            {
                var bytes = Encoding.ASCII.GetBytes(json);
                var hash = sha1.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            };
        }
    }
}