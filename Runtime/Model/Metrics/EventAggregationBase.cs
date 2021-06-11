using Backtrace.Unity.Json;
using System.Collections.Generic;

namespace Backtrace.Unity.Model.Metrics
{
    public abstract class EventAggregationBase
    {
        private const string TimestampName = "timestamp";
        private const string AttributesName = "attributes";
        public long Timestamp { get; set; }
        public string Name { get; private set; }
        public EventAggregationBase(string name, long timestamp)
        {
            Name = name;
            Timestamp = timestamp;
        }

        internal BacktraceJObject ToBaseObject(IDictionary<string, string> attributes)
        {
            var jObject = new BacktraceJObject();
            jObject.Add(TimestampName, Timestamp);
            jObject.Add(AttributesName, new BacktraceJObject(attributes));
            return jObject;
        }
    }
}
