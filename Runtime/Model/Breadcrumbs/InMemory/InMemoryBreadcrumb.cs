using System.Collections.Generic;
namespace Backtrace.Unity.Model.Breadcrumbs.InMemory
{
    public class InMemoryBreadcrumb
    {
        public string Message { get; set; }
        public BreadcrumbLevel Level { get; set; }
        public UnityEngineLogLevel Type { get; set; }
        public IDictionary<string, string> Attributes { get; set; }
    }
}
