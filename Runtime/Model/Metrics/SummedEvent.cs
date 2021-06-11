using Backtrace.Unity.Common;
using Backtrace.Unity.Json;
using System.Collections.Generic;

namespace Backtrace.Unity.Model.Metrics
{
    internal sealed class SummedEvent : EventAggregationBase
    {
        internal const string MetricGroupName = "metric_group";
        internal readonly IDictionary<string, string> Attributes;
        internal SummedEvent(string name) : this(name, DateTimeHelper.Timestamp(), new Dictionary<string, string>())
        { }

        internal SummedEvent(string name, long timestamp, IDictionary<string, string> attributes) : base(name, timestamp)
        {
            Attributes = attributes ?? new Dictionary<string, string>();
        }

        internal BacktraceJObject ToJson(IDictionary<string, string> scopedAttributes)
        {
            if (scopedAttributes != null)
            {
                foreach (var attribute in scopedAttributes)
                {
                    Attributes[attribute.Key] = attribute.Value;
                }
            }

            var jObject = ToBaseObject(Attributes);
            jObject.Add(MetricGroupName, Name);

            return jObject;
        }
    }
}
