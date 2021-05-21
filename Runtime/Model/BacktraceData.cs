using Backtrace.Unity.Json;
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
        public Guid Uuid { get; private set; }

        /// <summary>
        /// String representation of Uuid Guid - for optimization purposes.
        /// </summary>
        private string _uuidString;

        /// <summary>
        /// internal Uuid in string format
        /// </summary>
        internal string UuidString
        {
            get
            {
                if (string.IsNullOrEmpty(_uuidString))
                {
                    _uuidString = Uuid.ToString();
                }
                return _uuidString;
            }
        }

        /// <summary>
        /// UTC timestamp in seconds
        /// </summary>
        public long Timestamp { get; private set; }

        /// <summary>
        /// Name of programming language/environment this error comes from.
        /// </summary>
        public const string Lang = "csharp";

        /// <summary>
        /// Version of programming language/environment this error comes from.
        /// </summary>
        public readonly string LangVersion =
#if ENABLE_IL2CPP
             "IL2CPP";
#else
             "Mono";
#endif

        /// <summary>
        /// Name of the client that is sending this error report.
        /// </summary>
        public const string Agent = "backtrace-unity";

        /// <summary>
        /// Version of the C# library
        /// </summary>
        public const string AgentVersion = BacktraceClient.VERSION;

        /// <summary>
        /// Application thread details
        /// </summary>
        public Dictionary<string, ThreadInformation> ThreadInformations;

        /// <summary>
        /// Get a main thread name
        /// </summary>
        public string MainThread;

        /// <summary>
        /// Get a report classifiers. If user send custom message, then variable should be null
        /// </summary>
        public string[] Classifier;

        /// <summary>
        /// Get a path to report attachments
        /// </summary>
        public ICollection<string> Attachments;

        /// <summary>
        /// Current BacktraceReport
        /// </summary>
        public BacktraceReport Report { get; set; }

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
        /// Create instance of report data
        /// </summary>
        /// <param name="report">Current report</param>
        /// <param name="clientAttributes">BacktraceClient's attributes</param>
        public BacktraceData(BacktraceReport report, Dictionary<string, string> clientAttributes = null, int gameObjectDepth = -1)
        {
            if (report == null)
            {
                return;
            }
            Report = report;
            Uuid = Report.Uuid;
            Timestamp = Report.Timestamp;
            Classifier = Report.ExceptionTypeReport ? new[] { Report.Classifier } : new string[0];

            SetAttributes(clientAttributes, gameObjectDepth);
            SetThreadInformations();
            Attachments = new HashSet<string>(Report.AttachmentPaths);
        }

        /// <summary>
        /// Convert Backtrace data to JSON
        /// </summary>
        /// <returns>Backtrace Data JSON string</returns>
        public string ToJson()
        {
            var jObject = new BacktraceJObject(new Dictionary<string, string>()
            {
                ["uuid"] = UuidString,
                ["lang"] = Lang,
                ["langVersion"] = LangVersion,
                ["agent"] = Agent,
                ["agentVersion"] = AgentVersion,
                ["mainThread"] = MainThread,
            });
            jObject.Add("timestamp", Timestamp);
            jObject.Add("classifiers", Classifier);

            jObject.Add("attributes", Attributes.ToJson());
            jObject.Add("annotations", Annotation.ToJson());
            jObject.Add("threads", ThreadData.ToJson());
            return jObject.ToJson();
        }

        /// <summary>
        /// Set thread information
        /// </summary>
        private void SetThreadInformations()
        {
            var faultingThread = !(Report.Exception is BacktraceUnhandledException
                && string.IsNullOrEmpty(Report.Exception.StackTrace));

            ThreadData = new ThreadData(Report.DiagnosticStack, faultingThread);
            ThreadInformations = ThreadData.ThreadInformations;
            MainThread = ThreadData.MainThread;
        }

        /// <summary>
        /// Set report attributes and annotations
        /// </summary>
        /// <param name="clientAttributes">Backtrace client attributes</param>
        private void SetAttributes(Dictionary<string, string> clientAttributes, int gameObjectDepth)
        {
            Attributes = new BacktraceAttributes(Report, clientAttributes);
            Annotation = new Annotations(Report.ExceptionTypeReport ? Report.Exception : null, gameObjectDepth);
        }
    }
}

