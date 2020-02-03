using Backtrace.Unity.Model.JsonData;
using Backtrace.Unity.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

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
                    Debug.Log("Report or diagnostic stack is null");
                    return new string[0];
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

            var stringBuilder = new StringBuilder();
            stringBuilder.Append(Application);
            stringBuilder.Append(ExceptionMessage);
            stringBuilder.Append(Classifier);
            stringBuilder.Append(StackTrace);

            using (var sha256Hash = SHA256.Create())
            {
                var bytes = sha256Hash.ComputeHash(Encoding.ASCII.GetBytes(stringBuilder.ToString()));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}