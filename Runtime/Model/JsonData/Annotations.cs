using Backtrace.Unity.Extensions;
using Backtrace.Unity.Types;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backtrace.Unity.Model
{
    public class DeduplicationModel
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
        public string StackTrace
        {
            get
            {
                if (_strategy == DeduplicationStrategy.None)
                {
                    return "";
                }
                if (_backtraceData.Report == null || _backtraceData.Report.DiagnosticStack == null)
                {
                    return "";
                }
                var result = _backtraceData.Report.DiagnosticStack
                    .Select(n => n.FunctionName)
                    .OrderByDescending(n => n);

                var stackTrace = new HashSet<string>(result).ToArray();
                return string.Join(",", stackTrace);
            }
        }
        public string Classifier
        {
            get
            {
                if ((_strategy & DeduplicationStrategy.Classifier) == 0)
                {
                    return "";
                }
                var classifier = _backtraceData.Classifier ?? System.Array.Empty<string>();
                return string.Join(",", classifier);
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
                if (_backtraceData.Report == null || string.IsNullOrEmpty(_backtraceData.Report.Message))
                {
                    return string.Empty;
                }
                return _backtraceData.Report.Message.OnlyLetters();
            }
        }

        public string Factor
        {
            get
            {
                if (_backtraceData.Report == null)
                {
                    return string.Empty;
                }
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
            stringBuilder.Append(ExceptionMessage);
            stringBuilder.Append(Classifier);
            stringBuilder.Append(StackTrace);

            return stringBuilder.GetSha();
        }
    }
}