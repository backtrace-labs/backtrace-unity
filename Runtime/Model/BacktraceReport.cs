using Backtrace.Unity.Common;
using Backtrace.Unity.Extensions;
using System;
using System.Collections.Generic;

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
        public Dictionary<string, string> Attributes { get; private set; }

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
        /// Source code
        /// </summary>
        public BacktraceSourceCode SourceCode = null;

        /// <summary>
        /// Create new instance of Backtrace report to sending a report with custom client message
        /// </summary>
        /// <param name="message">Custom client message</param>
        /// <param name="attributes">Additional information about application state</param>
        /// <param name="attachmentPaths">Path to all report attachments</param>
        public BacktraceReport(
            string message,
            Dictionary<string, string> attributes = null,
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
            Dictionary<string, string> attributes = null,
            List<string> attachmentPaths = null)
        {
            Attributes = attributes ?? new Dictionary<string, string>();
            AttachmentPaths = attachmentPaths ?? new List<string>();
            Exception = exception;
            ExceptionTypeReport = exception != null;
            if (ExceptionTypeReport)
            {
                Message = exception.Message;
                SetClassifier();
                SetStacktraceInformation();
            }
        }

        /// <summary>
        /// Set report classifier
        /// </summary>
        private void SetClassifier()
        {
            // ignore classifier for message reports
            if (!ExceptionTypeReport)
            {
                Classifier = string.Empty;
            }

            Classifier = Exception is BacktraceUnhandledException
                ? (Exception as BacktraceUnhandledException).Classifier
                : Exception.GetType().Name;
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

        internal BacktraceData ToBacktraceData(Dictionary<string, string> clientAttributes, int gameObjectDepth)
        {
            return new BacktraceData(this, clientAttributes, gameObjectDepth);
        }

        /// <summary>
        /// Concat two attributes dictionary 
        /// </summary>
        /// <param name="report">Current report</param>
        /// <param name="attributes">Attributes to concatenate</param>
        /// <returns></returns>
        internal static Dictionary<string, string> ConcatAttributes(
            BacktraceReport report, Dictionary<string, string> attributes)
        {
            var reportAttributes = report.Attributes;
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
