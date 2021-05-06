using Backtrace.Unity.Common;
using Backtrace.Unity.Json;
using System.Collections.Generic;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Model.Session
{
    public sealed class UniqueEvent : EventAggregationBase
    {
        internal const string UniqueEventName = "unique";
        internal readonly IDictionary<string, string> Attributes;
        internal UniqueEvent(string name) : this(name, DateTimeHelper.Timestamp(), null) { }
        internal UniqueEvent(
            string name,
            long timestamp,
            IDictionary<string, string> attributes) : base(name, timestamp)
        {
            Attributes = attributes;
        }

        internal void UpdateTimestamp()
        {
            Timestamp = DateTimeHelper.Timestamp();
        }
        internal BacktraceJObject ToJson()
        {
            var jObject = ToBaseObject(Attributes);
            jObject.Add(UniqueEventName, new string[1] { Name });
            return jObject;
        }
    }
}
