using Backtrace.Newtonsoft;
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
        /// System environment variables
        /// </summary>
        private readonly EnvironmentVariables environment;

        /// <summary>
        /// Create new instance of Annotations class
        /// </summary>
        /// <param name="complexAttributes">Built-in complex attributes</param>
        public Annotations(Dictionary<string, object> complexAttributes)
        {
            environment = new EnvironmentVariables();
            ComplexAttributes = complexAttributes;
            EnvironmentVariables = environment.Variables;
        }
    }
}
