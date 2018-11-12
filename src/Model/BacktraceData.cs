using Backtrace.Newtonsoft;
using Backtrace.Newtonsoft.Linq;
using Backtrace.Unity.Model.JsonData;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public const string Agent = "backtrace-unity";

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
        /// Get current host environment variables
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

        private BacktraceAttributes _attributes = null;
        private Annotations _annotations = null;
        private ThreadData _threadData = null;


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

        public string ToJson()
        {
            using (var outputFile = new System.IO.StreamWriter(System.IO.Path.Combine(@"C:\Users\konra\source\BacktraceDatabase", "backtraceresult-data.txt"), true))
            {
                outputFile.WriteLine("Creating json file");
                var json = new BacktraceJObject
                {
                    ["uuid"] = Uuid,
                    ["timestamp"] = Timestamp,
                    ["lang"] = "csharp",
                    ["langVersion"] = "Mono/Il2CPP",
                    ["agent"] = "backtrace-unity",
                    ["agentVersion"] = "1.0.0",
                    ["mainThread"] = MainThread
                };
                outputFile.WriteLine("Converting attributes");
                json["attributes"] = _attributes.ToJson();
                outputFile.WriteLine("Converting annotations");
                json["annotations"] = _annotations.ToJson();
                outputFile.WriteLine("Serializing classifier");
                JArray jArray = new JArray(Classifier);
                json["classifiers"] = jArray;
                outputFile.WriteLine("Converting threads");
                json["threads"] = _threadData.ToJson();
                outputFile.WriteLine("Converting all json attributes together");
                try
                {
                    var jsonString = json.ToString();
                    return jsonString;
                }
                catch(Exception e)
                {
                    outputFile.WriteLine("EXCEPTION");
                    outputFile.WriteLine(e.ToString());
                    return string.Empty;
                }

            }
        }

        private void SetThreadInformations()
        {
            _threadData = new ThreadData(Report.DiagnosticStack);
            ThreadInformations = _threadData.ThreadInformations;
            MainThread = _threadData.MainThread;
            var sourceCodeData = new SourceCodeData(Report.DiagnosticStack);
            SourceCode = sourceCodeData.data.Any() ? sourceCodeData.data : null;
        }

        private void SetAttributes(Dictionary<string, object> clientAttributes)
        {
            _attributes = new BacktraceAttributes(Report, clientAttributes);
            Attributes = _attributes.Attributes;
            _annotations = new Annotations(_attributes.ComplexAttributes);
        }

        private void SetReportInformation()
        {
            Uuid = Report.Uuid;
            Timestamp = Report.Timestamp;
            LangVersion = "Mono/IL2CPP";
            AgentVersion = "1.0.0";
            Classifier = Report.ExceptionTypeReport ? new[] { Report.Classifier } : null;
        }
    }
}

