using Backtrace.Newtonsoft;
using Backtrace.Newtonsoft.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Backtrace.Unity.Model.JsonData
{
    /// <summary>
    /// Get report annotations - environment variables
    /// </summary>
    public class Annotations
    {

        /// <summary>
        /// Get system environment variables
        /// </summary>
        [JsonProperty(PropertyName = "Environment Variables")]
        public Dictionary<string, string> EnvironmentVariables;
        
        /// <summary>
        /// Get built-in complex attributes
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> ComplexAttributes = new Dictionary<string, object>();
        
        /// <summary>
        /// Create new instance of Annotations class
        /// </summary>
        /// <param name="complexAttributes">Built-in complex attributes</param>
        public Annotations(Dictionary<string, object> complexAttributes)
        {
            var environment = new EnvironmentVariables();
            ComplexAttributes = complexAttributes;
            EnvironmentVariables = environment.Variables;
        }

        internal BacktraceJObject ToJson()
        {
            var annotations = new BacktraceJObject();
            var envVariables = new BacktraceJObject();

            foreach (var envVariable in EnvironmentVariables)
            {
                envVariables[envVariable.Key] = envVariable.Value?.ToString() ?? string.Empty;
            }
            annotations["Environment Variables"] = envVariables;

            //todo: complex attributes

            return annotations;
        }
    }
}
