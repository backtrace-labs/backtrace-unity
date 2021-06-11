using System;
using System.Collections.Generic;
using System.Globalization;

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
        public double Timestamp
        {
            get { return Convert.ToDouble(timestamp); }
            set { timestamp = value.ToString("F0", CultureInfo.InvariantCulture); }
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
