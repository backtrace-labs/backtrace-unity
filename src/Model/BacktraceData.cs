using Backtrace.Unity.Model.JsonData;
using Backtrace.Newtonsoft;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Backtrace.Unity.Model
{
    /// <summary>
    /// Serializable Backtrace API data object
    /// </summary>
    public class BacktraceData
    {
        /// <summary>
        /// 16 bytes of randomness in human readable UUID format
        /// server will reject request if uuid is already found
        /// </summary>
        [JsonProperty(PropertyName = "uuid")]
        public Guid Uuid;

        /// <summary>
        /// UTC timestamp in seconds
        /// </summary>
        [JsonProperty(PropertyName = "timestamp")]
        public long Timestamp;

        /// <summary>
        /// Name of programming language/environment this error comes from.
        /// </summary>
        [JsonProperty(PropertyName = "lang")]
        public const string Lang = "csharp";

        /// <summary>
        /// Version of programming language/environment this error comes from.
        /// </summary>
        [JsonProperty(PropertyName = "langVersion")]
        public string LangVersion;

        /// <summary>
        /// Name of the client that is sending this error report.
        /// </summary>
        [JsonProperty(PropertyName = "agent")]
        public const string Agent = "backtrace-csharp";

        /// <summary>
        /// Version of the C# library
        /// </summary>
        [JsonProperty(PropertyName = "agentVersion")]
        public string AgentVersion;

        /// <summary>
        /// Get built-in attributes
        /// </summary>
        [JsonProperty(PropertyName = "attributes")]
        public Dictionary<string, object> Attributes;

        /// <summary>
        /// Get current host environment variables and application dependencies
        /// </summary>
        [JsonProperty(PropertyName = "annotations")]
        internal Annotations Annotations;

        /// <summary>
        /// Application thread details
        /// </summary>
        [JsonProperty(PropertyName = "threads")]
        internal Dictionary<string, ThreadInformation> ThreadInformations;

        /// <summary>
        /// Get a main thread name
        /// </summary>
        [JsonProperty(PropertyName = "mainThread")]
        public string MainThread;

        /// <summary>
        /// Get a report classifiers. If user send custom message, then variable should be null
        /// </summary>
        [JsonProperty(PropertyName = "classifiers", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Classifier;

        [JsonProperty(PropertyName = "sourceCode", NullValueHandling = NullValueHandling.Ignore)]
        internal Dictionary<string, SourceCodeData.SourceCode> SourceCode;

        /// <summary>
        /// Get a path to report attachments
        /// </summary>
        [JsonIgnore]
        public List<string> Attachments;

        /// <summary>
        /// Current BacktraceReport
        /// </summary>
        internal BacktraceReport Report { get; set; }

        /// <summary>
        /// Create instance of report data
        /// </summary>
        /// <param name="report">Current report</param>
        /// <param name="clientAttributes">BacktraceClient's attributes</param>
        public BacktraceData(BacktraceReport report, Dictionary<string, object> clientAttributes)
        {
            if (report == null)
            {
                return;
            }
            Report = report;
            SetReportInformation();
            SetAttributes(clientAttributes);
            SetThreadInformations();
            Attachments = Report.AttachmentPaths.Distinct().ToList();
        }

        private void SetThreadInformations()
        {
            var threadData = new ThreadData(Report.CallingAssembly, Report.DiagnosticStack);
            ThreadInformations = threadData.ThreadInformations;
            MainThread = threadData.MainThread;
            var sourceCodeData = new SourceCodeData(Report.DiagnosticStack);
            SourceCode = sourceCodeData.data.Any() ? sourceCodeData.data : null;
        }

        private void SetAttributes(Dictionary<string, object> clientAttributes)
        {
            var backtraceAttributes = new BacktraceAttributes(Report, clientAttributes);
            Attributes = backtraceAttributes.Attributes;
            Annotations = new Annotations(Report.CallingAssembly, backtraceAttributes.ComplexAttributes);
        }

        private void SetReportInformation()
        {
            AssemblyName CurrentAssembly = Assembly.GetExecutingAssembly().GetName();
            Uuid = Report.Uuid;
            Timestamp = Report.Timestamp;
            LangVersion = typeof(string).Assembly.ImageRuntimeVersion;
            AgentVersion = CurrentAssembly.Version.ToString();
            Classifier = Report.ExceptionTypeReport ? new[] { Report.Classifier } : null;
        }
    }
}

