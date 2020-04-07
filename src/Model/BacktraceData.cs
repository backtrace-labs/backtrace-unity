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
        public Guid Uuid { get; set; }

        /// <summary>
        /// UTC timestamp in seconds
        /// </summary>
        [JsonProperty(PropertyName = "timestamp")]
        public long Timestamp { get; set; }

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
        /// Get built-in attributes
        /// </summary>
        public BacktraceAttributes Attributes = null;
        public Annotations Annotation = null;
        public ThreadData ThreadData = null;


        /// <summary>
        /// Number of deduplications
        /// </summary>
        public int Deduplication = 0;

        /// <summary>
        /// Empty constructor for serialization purpose
        /// </summary>
        public BacktraceData()
        { }
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

        /// <summary>
        /// Convert Backtrace data to JSON
        /// </summary>
        /// <returns>Backtrace Data JSON string</returns>
        public string ToJson()
        {
            var json = new BacktraceJObject
            {
                {"uuid", Uuid},
                {"timestamp", Timestamp},
                {"lang", Lang},
                {"langVersion", LangVersion},
                {"agent", Agent},
                {"agentVersion", AgentVersion},
                {"mainThread", MainThread},
                {"classifiers", new JArray(Classifier)},
                {"attributes", Attributes.ToJson()},
                {"annotations", Annotation.ToJson()},
                {"threads", ThreadData == null ? null : ThreadData.ToJson()}
            };
            return json.ToString();
        }

        /// <summary>
        /// Convert JSON to Backtrace Data
        /// </summary>
        /// <param name="json">Backtrace Data JSON</param>
        /// <returns>Backtrace Data instance</returns>
        public static BacktraceData Deserialize(string json)
        {
            var @object = BacktraceJObject.Parse(json);

            var classfiers = @object == null ? null : @object["classifiers"]
                                 .Select(n => n.Value<string>()).ToArray() ?? null;

            return new BacktraceData()
            {
                Uuid = new Guid(@object.Value<string>("uuid")),
                Timestamp = @object.Value<long>("timestamp"),
                MainThread = @object.Value<string>("mainThread"),
                Classifier = classfiers,
                Annotation = Annotations.Deserialize(@object["annotations"]),
                Attributes = BacktraceAttributes.Deserialize(@object["attributes"]),
                ThreadData = ThreadData.DeserializeThreadInformation(@object["threads"])
            };
        }

        /// <summary>
        /// Set thread information
        /// </summary>
        private void SetThreadInformations()
        {
            ThreadData = new ThreadData(Report.DiagnosticStack);
            ThreadInformations = ThreadData.ThreadInformations;
            MainThread = ThreadData.MainThread;
        }

        /// <summary>
        /// Set report attributes and annotations
        /// </summary>
        /// <param name="clientAttributes">Backtrace client attributes</param>
        private void SetAttributes(Dictionary<string, object> clientAttributes)
        {
            Attributes = new BacktraceAttributes(Report, clientAttributes);
            Annotation = new Annotations(Attributes.ComplexAttributes);
        }

        /// <summary>
        /// Set default exception/agent information
        /// </summary>
        private void SetReportInformation()
        {
            Uuid = Report.Uuid;
            Timestamp = Report.Timestamp;
#if ENABLE_IL2CPP
            LangVersion = "IL2CPP";
#else
            LangVersion = "Mono";
#endif

            AgentVersion = "2.0.5";
            Classifier = Report.ExceptionTypeReport ? new[] { Report.Classifier } : null;
        }
    }
}

