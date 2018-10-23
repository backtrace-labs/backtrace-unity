using Backtrace.Unity.Common;
using System;
using System.Collections.Generic;
using System.Reflection;

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
        public Guid Uuid { get; private set; } = Guid.NewGuid();

        /// <summary>
        /// UTC timestamp in seconds
        /// </summary>
        public long Timestamp { get; private set; } = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

        /// <summary>
        /// Get information aboout report type. If value is true the BacktraceReport has an error information
        /// </summary>
        public bool ExceptionTypeReport { get; private set; } = false;

        /// <summary>
        /// Get a report classification 
        /// </summary>
        public string Classifier { get; set; } = string.Empty;

        /// <summary>
        /// Get an report attributes
        /// </summary>
        public Dictionary<string, object> Attributes { get; private set; }

        /// <summary>
        /// Get a custom client message
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Get a report exception
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Get all paths to attachments
        /// </summary>
        public List<string> AttachmentPaths { get; set; }

        /// <summary>
        /// Current report exception stack
        /// </summary>
        public List<BacktraceStackFrame> DiagnosticStack { get; set; }

        /// <summary>
        /// Get or set minidump attachment path
        /// </summary>
        internal string MinidumpFile { get; private set; }

        /// <summary>
        /// Get an assembly where report was created (or should be created)
        /// </summary>
        internal Assembly CallingAssembly { get; set; }

        internal readonly bool _reflectionMethodName;

        /// <summary>
        /// Create new instance of Backtrace report to sending a report with custom client message
        /// </summary>
        /// <param name="message">Custom client message</param>
        /// <param name="attributes">Additional information about application state</param>
        /// <param name="attachmentPaths">Path to all report attachments</param>
        public BacktraceReport(
            string message,
            Dictionary<string, object> attributes = null,
            List<string> attachmentPaths = null,
            bool reflectionMethodName = true)
            : this(null as Exception, attributes, attachmentPaths, reflectionMethodName)
        {
            Message = message;
        }

        /// <summary>
        /// Create new instance of Backtrace report to sending a report with application exception
        /// </summary>
        /// <param name="exception">Current exception</param>
        /// <param name="callingAssembly">Calling assembly</param>
        /// <param name="attributes">Additional information about application state</param>
        /// <param name="attachmentPaths">Path to all report attachments</param>
        public BacktraceReport(
            Exception exception,
            Dictionary<string, object> attributes = null,
            List<string> attachmentPaths = null,
            bool reflectionMethodName = true)
        {
            Attributes = attributes ?? new Dictionary<string, object>();
            AttachmentPaths = attachmentPaths ?? new List<string>();
            Exception = exception;
            ExceptionTypeReport = exception != null;
            Classifier = ExceptionTypeReport ? exception.GetType().Name : string.Empty;
            _reflectionMethodName = reflectionMethodName;
            SetCallingAssemblyInformation();
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

        internal BacktraceData ToBacktraceData(Dictionary<string, object> clientAttributes)
        {
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

        internal void SetCallingAssemblyInformation()
        {
            var stacktrace = new BacktraceStackTrace(Exception, _reflectionMethodName);
            DiagnosticStack = stacktrace.StackFrames;
            CallingAssembly = stacktrace.CallingAssembly;
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
            copy.SetCallingAssemblyInformation();
            copy.Classifier = copy.Exception.GetType().Name;
            return copy;
        }
    }
}
