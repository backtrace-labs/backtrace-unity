using Backtrace.Unity.Json;
using System.Collections.Generic;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Model.Metrics
{
    public sealed class UniqueEvent : EventAggregationBase
    {
        internal const string UniqueEventName = "unique";
        internal IDictionary<string, string> Attributes;
        internal UniqueEvent(
            string name,
            long timestamp,
            IDictionary<string, string> attributes) : base(name, timestamp)
        {
            Attributes = attributes;
        }

        internal void UpdateTimestamp(long timestamp, IDictionary<string, string> attributes)
        {
            Timestamp = timestamp;
            if (attributes != null && attributes.TryGetValue(Name, out string value) && !string.IsNullOrEmpty(value))
            {
                Attributes = attributes;
            }
        }
        internal BacktraceJObject ToJson()
        {
            var jObject = ToBaseObject(Attributes);
            jObject.Add(UniqueEventName, new string[1] { Name });
            return jObject;
        }
    }
}
