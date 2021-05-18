using Backtrace.Unity.Common;
using Backtrace.Unity.Json;
using Backtrace.Unity.Model.JsonData;
using System.Collections.Generic;

namespace Backtrace.Unity.Model.Metrics
{
    internal sealed class UniqueEventsSubmissionQueue : MetricsSubmissionQueue<UniqueEvent>
    {
        private const string Name = "unique-events";
        private readonly AttributeProvider _attributeProvider;
        public UniqueEventsSubmissionQueue(
            string universeName,
            string token,
            IBacktraceHttpClient httpClient,
            AttributeProvider attributeProvider)
            : base(Name, universeName, token, httpClient)
        {
            _attributeProvider = attributeProvider;
        }

        public override void StartWithEvent(string eventName)
        {
            var uniqueEventAttributes = _attributeProvider.GenerateAttributes();
            if (uniqueEventAttributes.TryGetValue(eventName, out string value) && !string.IsNullOrEmpty(value))
            {
                Events.AddLast(new UniqueEvent(eventName, DateTimeHelper.Timestamp(), _attributeProvider.GenerateAttributes()));
            }
            Send();
        }

        internal override IEnumerable<BacktraceJObject> GetEventsPayload(ICollection<UniqueEvent> events)
        {
            var uniqueEventsJson = new List<BacktraceJObject>();
            foreach (var uniqueEvent in events)
            {
                uniqueEventsJson.Add(uniqueEvent.ToJson());
                uniqueEvent.UpdateTimestamp(DateTimeHelper.Timestamp(), _attributeProvider.GenerateAttributes());
            }

            return uniqueEventsJson;
        }
    }
}
