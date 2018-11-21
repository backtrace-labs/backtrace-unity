using Backtrace.Newtonsoft;
using Backtrace.Newtonsoft.Linq;
using System.Collections.Generic;

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
        public Dictionary<string, string> EnvironmentVariables { get; set; }

        /// <summary>
        /// Get built-in complex attributes
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> ComplexAttributes = new Dictionary<string, object>();

        public Annotations()
        {

        }
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
            return annotations;
        }

        public static Annotations Deserialize(JToken token)
        {
            var annotations = new Annotations();
            //get all environment variables and complex attributes
            foreach (BacktraceJProperty annotation in token)
            {
                //parse all dictionaries of values
                var values = new Dictionary<string, string>();
                foreach (var annotationDictionary in annotation)
                {
                    foreach (BacktraceJProperty value in annotationDictionary)
                    {
                        values.Add(value.Name, value.Value.Value<string>());
                    }
                }
                if (annotation.Name == "Environment Variables")
                {
                    annotations.EnvironmentVariables = values;
                }
            }
            return annotations;
        }
    }
}
