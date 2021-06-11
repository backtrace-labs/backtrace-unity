using Backtrace.Unity.Common;
using Backtrace.Unity.Json;
using Backtrace.Unity.Model.JsonData;
using System.Collections.Generic;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Model.Metrics
{
    internal sealed class UniqueEventsSubmissionQueue : MetricsSubmissionQueue<UniqueEvent>
    {
        private const string Name = "unique_events";
        private readonly AttributeProvider _attributeProvider;
        public UniqueEventsSubmissionQueue(
            string submissionUrl,
            AttributeProvider attributeProvider)
            : base(Name, submissionUrl)
        {
            _attributeProvider = attributeProvider;
        }

        public override void StartWithEvent(string eventName)
        {
            var uniqueEventAttributes = GetUniqueEventAttributes();
            if (uniqueEventAttributes.TryGetValue(eventName, out string value) && !string.IsNullOrEmpty(value))
            {
                Events.AddLast(new UniqueEvent(eventName, DateTimeHelper.Timestamp(), uniqueEventAttributes));
            }
            Send();
        }

        internal override IEnumerable<BacktraceJObject> GetEventsPayload(ICollection<UniqueEvent> events)
        {
            var uniqueEventsJson = new List<BacktraceJObject>();
            foreach (var uniqueEvent in events)
            {
                uniqueEventsJson.Add(uniqueEvent.ToJson());
                uniqueEvent.UpdateTimestamp(DateTimeHelper.Timestamp(), GetUniqueEventAttributes());
            }

            return uniqueEventsJson;
        }
        private IDictionary<string, string> GetUniqueEventAttributes()
        {
            return _attributeProvider.GenerateAttributes(false);
        }
    }
}
