using System;
using System.Collections.Generic;
namespace Backtrace.Unity.Model.Breadcrumbs.InMemory
{
    [Serializable]
    public class InMemoryBreadcrumb
    {
        public string Message
        {
            get { return message; }
            set
            {
                message = value;
            }
        }
        public string message;
        public string Timestamp
        {
            get { return timestamp; }
            set { timestamp = value; }
        }
        public string timestamp;

        public BreadcrumbLevel Type
        {
            get
            {
                return (BreadcrumbLevel)Enum.Parse(typeof(BreadcrumbLevel), type, true);
            }
            set
            {
                type = Enum.GetName(typeof(BreadcrumbLevel), value).ToLower();
            }
        }
        public string type;

        public UnityEngineLogLevel Level
        {
            get
            {
                return (UnityEngineLogLevel)Enum.Parse(typeof(UnityEngineLogLevel), level, true);
            }
            set
            {
                level = Enum.GetName(typeof(UnityEngineLogLevel), value).ToLower();
            }
        }
        public string level;
        [NonSerialized]
        public IDictionary<string, string> Attributes;
    }
}
