using Backtrace.Unity.Json;
using Backtrace.Unity.Model.JsonData;
using System.Collections.Generic;

namespace Backtrace.Unity.Model.Metrics
{
    internal sealed class SummedEventsSubmissionQueue : MetricsSubmissionQueue<SummedEvent>
    {
        private const string Name = "summed_events";
        private readonly AttributeProvider _attributeProvider;
        public SummedEventsSubmissionQueue(
          string submissionUrl,
          AttributeProvider attributeProvider)
          : base(Name, submissionUrl)
        {
            _attributeProvider = attributeProvider;
        }

        public override void StartWithEvent(string eventName)
        {
            Events.AddLast(new SummedEvent(eventName));
            Send();
        }

        internal override IEnumerable<BacktraceJObject> GetEventsPayload(ICollection<SummedEvent> events)
        {
            var summedEventJson = new List<BacktraceJObject>();
            var attributes = _attributeProvider.GenerateAttributes(false);
            foreach (var summedEvent in events)
            {
                summedEventJson.Add(summedEvent.ToJson(attributes));
            }
            Events.Clear();
            return summedEventJson;
        }

        internal override void OnMaximumAttemptsReached(ICollection<SummedEvent> events)
        {
            if (Count + events.Count < MaximumEvents)
            {
                foreach (var summedEvent in events)
                {
                    Events.AddFirst(summedEvent);
                }
            }
        }
    }
}
