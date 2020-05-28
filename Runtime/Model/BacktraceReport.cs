using Backtrace.Newtonsoft;
using Backtrace.Newtonsoft.Linq;
using Backtrace.Unity.Common;
using Backtrace.Unity.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Backtrace.Unity.Model
{
    /// <summary>
    /// Capture application report
    /// </summary>
    public class BacktraceReport
    {
        /// <summary>
        /// Fingerprint
        /// </summary>
        public string Fingerprint { get; set; }

        /// <summary>
        /// Factor
        /// </summary>
        public string Factor { get; set; }

        /// <summary>
        /// 16 bytes of randomness in human readable UUID format
        /// server will reject request if uuid is already found
        /// </summary>s
        public Guid Uuid = Guid.NewGuid();

        /// <summary>
        /// UTC timestamp in seconds
        /// </summary>
        public long Timestamp = new DateTime().Timestamp();

        /// <summary>
        /// Get information aboout report type. If value is true the BacktraceReport has an error information
        /// </summary>
        public bool ExceptionTypeReport = false;

        /// <summary>
        /// Get a report classification 
        /// </summary>
        public string Classifier = string.Empty;

        /// <summary>
        /// Get an report attributes
        /// </summary>
        [JsonProperty(PropertyName = "attributes")]
        public Dictionary<string, object> Attributes { get; private set; }

        /// <summary>
        /// Get a custom client message
        /// </summary>
        [JsonProperty(PropertyName = "message")]
        public string Message { get; private set; }

        /// <summary>
        /// Get a report exception
        /// </summary>
        [JsonIgnore]
        public Exception Exception { get; private set; }

        /// <summary>
        /// Get all paths to attachments
        /// </summary>
        [JsonProperty(PropertyName = "attachmentPaths")]
        public List<string> AttachmentPaths { get; set; }

        /// <summary>
        /// Current report exception stack
        /// </summary>
        [JsonProperty(PropertyName = "diagnosticStack")]
        public List<BacktraceStackFrame> DiagnosticStack { get; set; }

        /// <summary>
        /// Get or set minidump attachment path
        /// </summary>
        [JsonProperty(PropertyName = "minidumpFile")]
        internal string MinidumpFile { get; private set; }

        /// <summary>
        /// Source code
        /// </summary>
        public BacktraceSourceCode SourceCode = null;

        /// <summary>
        /// Convert Backtrace report to JSON
        /// </summary>
        /// <returns>Backtrace report JSON representation</returns>
        public string ToJson()
        {
            var reportStackTrace = new JArray();
            foreach (var diagnosticFrame in DiagnosticStack)
            {
                reportStackTrace.Add(diagnosticFrame.ToJson());
            }

            var attributes = new BacktraceJObject();
            foreach (var value in Attributes)
            {
                attributes[value.Key] = value.Value.ToString();
            }

            var report = new BacktraceJObject()
            {
                {"Fingerprint", Fingerprint},
                {"Factor", Factor},
                {"Uuid", Uuid.ToString()},
                {"Timestamp", Timestamp},
                {"ExceptionTypeReport", ExceptionTypeReport},
                {"Classifier", Classifier},
                {"message", Message},
                {"minidumpFile", MinidumpFile},
                {"attachmentPaths", new JArray(AttachmentPaths)},
                {"diagnosticStack", reportStackTrace},
                {"attributes", attributes}
            };
            return report.ToString();
        }

        /// <summary>
        /// Convert JSON to Backtrace Report
        /// </summary>
        /// <param name="json">Backtrace Report JSON</param>
        /// <returns>Backtrace report instance</returns>
        public static BacktraceReport Deserialize(string json)
        {
            var @object = BacktraceJObject.Parse(json);
            var attributesObject = @object["attributes"];
            var attributes = new Dictionary<string, object>();
            foreach (BacktraceJProperty keys in attributesObject)
            {
                attributes.Add(keys.Name, keys.Value.Value<string>());
            }

            var exceptionStack = @object["diagnosticStack"];
            var resultStack = new List<BacktraceStackFrame>();
            foreach (var stack in exceptionStack)
            {
                var deserializedStack = BacktraceStackFrame.FromJson(stack.ToString());
                resultStack.Add(deserializedStack);
            }
            var attachmentJson = @object["attachmentPaths"];
            var attachments = attachmentJson.Select(n => n.Value<string>()).ToList();


            return new BacktraceReport(string.Empty)
            {
                Fingerprint = @object.Value<string>("Fingerprint"),
                Factor = @object.Value<string>("Factor"),
                Uuid = new Guid(@object.Value<string>("Uuid")),
                Timestamp = @object.Value<long>("Timestamp"),
                ExceptionTypeReport = @object.Value<bool>("ExceptionTypeReport"),
                Classifier = @object.Value<string>("Classifier"),
                Message = @object.Value<string>("message"),
                MinidumpFile = @object.Value<string>("minidumpFile"),
                Attributes = attributes,
                DiagnosticStack = resultStack,
                AttachmentPaths = attachments
            };
        }

        /// <summary>
        /// Create new instance of Backtrace report to sending a report with custom client message
        /// </summary>
        /// <param name="message">Custom client message</param>
        /// <param name="attributes">Additional information about application state</param>
        /// <param name="attachmentPaths">Path to all report attachments</param>
        [JsonConstructor]
        public BacktraceReport(
            string message,
            Dictionary<string, object> attributes = null,
            List<string> attachmentPaths = null)
            : this(null as Exception, attributes, attachmentPaths)
        {
            Message = message;
            // analyse stack trace information in both constructor 
            // to include error message in both source code properties.
            SetStacktraceInformation();
        }

        /// <summary>
        /// Create new instance of Backtrace report to sending a report with application exception
        /// </summary>
        /// <param name="exception">Current exception</param>
        /// <param name="attributes">Additional information about application state</param>
        /// <param name="attachmentPaths">Path to all report attachments</param>
        public BacktraceReport(
            Exception exception,
            Dictionary<string, object> attributes = null,
            List<string> attachmentPaths = null)
        {
            Attributes = attributes ?? new Dictionary<string, object>();
            AttachmentPaths = attachmentPaths ?? new List<string>();
            Exception = exception;
            ExceptionTypeReport = exception != null;
            if (ExceptionTypeReport)
            {
                Message = exception.Message;
                SetReportFingerPrintForEmptyStackTrace();
                SetStacktraceInformation();
            }
            Classifier = ExceptionTypeReport ? exception.GetType().Name : string.Empty;
        }

        /// <summary>
        /// Set a path to report minidump
        /// </summary>
        /// <param name="minidumpPath">Path to generated minidump file</param>
        internal void SetMinidumpPath(string minidumpPath)
        {
            if (string.IsNullOrEmpty(minidumpPath))
            {
                return;
            }
            MinidumpFile = minidumpPath;
            AttachmentPaths.Add(minidumpPath);
        }

        /// <summary>
        /// Override default fingerprint for reports without faulting stack trace.
        /// </summary>
        internal void SetReportFingerPrintForEmptyStackTrace()
        {
            if (string.IsNullOrEmpty(Exception.StackTrace))
            {
                // set attributes instead of fingerprint to still allow our user to define customer
                // fingerprints for reports without stack trace and apply deduplication rules in report flow.
                Attributes["_mod_fingerprint"] = Exception.Message.OnlyLetters().GetSha();
            }

        }

        internal BacktraceData ToBacktraceData(Dictionary<string, object> clientAttributes)
        {
            SetStacktraceInformation();
            return new BacktraceData(this, clientAttributes);
        }

        /// <summary>
        /// Concat two attributes dictionary 
        /// </summary>
        /// <param name="report">Current report</param>
        /// <param name="attributes">Attributes to concatenate</param>
        /// <returns></returns>
        internal static Dictionary<string, object> ConcatAttributes(
            BacktraceReport report, Dictionary<string, object> attributes)
        {
            var reportAttributes = report.Attributes;
            if (attributes == null)
            {
                return reportAttributes;
            };
            return reportAttributes.Merge(attributes);
        }

        internal void SetStacktraceInformation()
        {
            var stacktrace = new BacktraceStackTrace(Message, Exception);
            DiagnosticStack = stacktrace.StackFrames;
            SourceCode = stacktrace.SourceCode;
        }
        /// <summary>
        /// create a copy of BacktraceReport for inner exception object inside exception
        /// </summary>
        /// <returns>BacktraceReport for InnerExceptionObject</returns>
        internal BacktraceReport CreateInnerReport()
        {
            // there is no additional exception inside current exception
            // or exception does not exists
            if (!ExceptionTypeReport || Exception.InnerException == null)
            {
                return null;
            }
            var copy = (BacktraceReport)MemberwiseClone();
            copy.Exception = Exception.InnerException;
            copy.SetStacktraceInformation();
            copy.Classifier = copy.Exception.GetType().Name;
            return copy;
        }
    }
}
