using Backtrace.Unity.Json;
using System.Collections.Generic;

namespace Backtrace.Unity.Model.JsonData
{
    /// <summary>
    /// Class instance to get a built-in attributes from current application
    /// </summary>
    public class BacktraceAttributes
    {
        /// <summary>
        /// Get built-in primitive attributes
        /// </summary>
        public readonly Dictionary<string, string> Attributes;

        /// <summary>
        /// Create instance of Backtrace Attribute
        /// </summary>
        /// <param name="report">Received report</param>
        /// <param name="clientAttributes">Client's attributes (report and client)</param>
        public BacktraceAttributes(BacktraceReport report, Dictionary<string, string> clientAttributes)
        {
            Attributes = clientAttributes;

            if (report != null && report.Attributes != null)
            {
                if (Attributes == null)
                {
                    Attributes = report.Attributes;
                }
                else
                {
                    foreach (var attribute in report.Attributes)
                    {
                        Attributes[attribute.Key] = attribute.Value;
                    }
                }
            }
            if (Attributes == null)
            {
                Attributes = new Dictionary<string, string>();
            }
        }

        public BacktraceJObject ToJson()
        {
            return new BacktraceJObject(Attributes);
        }

    }
}
