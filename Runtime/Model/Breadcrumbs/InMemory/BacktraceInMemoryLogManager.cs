using Backtrace.Unity.Common;
using System.Collections.Generic;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Model.Breadcrumbs.InMemory
{
    internal sealed class BacktraceInMemoryLogManager : IBacktraceLogManager
    {
        /// <summary>
        /// Default maximum number of in memory breadcrumbs
        /// </summary>
        public const int DefaultMaximumNumberOfInMemoryBreadcrumbs = 100;

        /// <summary>
        /// Maximum number of in memory breadcrumbs
        /// </summary>
        public int MaximumNumberOfBreadcrumbs { get; set; } = DefaultMaximumNumberOfInMemoryBreadcrumbs;

        /// <summary>
        /// Lock object
        /// </summary>
        private object _lockObject = new object();

        /// <summary>
        /// Breadcrumbs
        /// </summary>
        internal readonly Queue<InMemoryBreadcrumb> Breadcrumbs = new Queue<InMemoryBreadcrumb>(DefaultMaximumNumberOfInMemoryBreadcrumbs);

        private double _breadcrumbId = DateTimeHelper.TimestampMs();

        /// <summary>
        /// Returns path to breadcrumb file - which is string.Empty for in memory breadcrumb manager
        /// </summary>
        public string BreadcrumbsFilePath
        {
            get
            {
                return string.Empty;
            }
        }

        public bool Add(string message, BreadcrumbLevel type, UnityEngineLogLevel level, IDictionary<string, string> attributes)
        {
            lock (_lockObject)
            {
                if (Breadcrumbs.Count + 1 > MaximumNumberOfBreadcrumbs)
                {
                    while (Breadcrumbs.Count + 1 > MaximumNumberOfBreadcrumbs)
                    {
                        Breadcrumbs.Dequeue();
                    }
                }
            }

            Breadcrumbs.Enqueue(new InMemoryBreadcrumb()
            {
                Message = message,
                Timestamp = DateTimeHelper.TimestampMs(),
                Level = level,
                Type = type,
                Attributes = attributes
            });
            _breadcrumbId++;

            return true;
        }

        public bool Clear()
        {
            Breadcrumbs.Clear();
            return true;
        }

        public bool Enable()
        {
            return true;
        }

        public int Length()
        {
            return Breadcrumbs.Count;
        }

        public double BreadcrumbId()
        {
            return _breadcrumbId;
        }
    }
}
