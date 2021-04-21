using Backtrace.Unity.Json;
using System.Collections.Generic;

namespace Backtrace.Unity.Model.Session
{
    internal sealed class SessionEvent : EventAggregationBase
    {
        internal const string MetricGroupName = "metric_group";
        internal readonly IDictionary<string, string> Attributes;
        internal SessionEvent(string name, long timestamp, IDictionary<string, string> attributes) : base(name, timestamp)
        {
            Attributes = attributes;
        }

        internal BacktraceJObject ToJson(IDictionary<string, string> attributes)
        {
            if (Attributes != null)
            {
                foreach (var attribute in Attributes)
                {
                    attributes[attribute.Key] = attribute.Value;
                }
            }
            var jObject = ToBaseObject(attributes);
            jObject.Add(MetricGroupName, Name);

            return jObject;
        }
    }
}
