using Assets.Plugins.src.Backtrace.Unity.Common;
using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace Backtrace.Unity.Model.JsonData
{
    /// <summary>
    /// System environment variables
    /// </summary>
    public class EnvironmentVariables
    {
        /// <summary>
        /// System environment values dictionary
        /// </summary>
        public StringDictionary Variables = new StringDictionary();

        /// <summary>
        /// Create instance of EnvironmnetVariables class to get system environment variables
        /// </summary>
        public EnvironmentVariables()
        {
            ReadEnvironmentVariables();
        }

        /// <summary>
        /// Read all environment variables from system
        /// </summary>
        private void ReadEnvironmentVariables()
        {
            foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
            {
                Variables.Add(variable.Key.ToString(), Regex.Escape(variable.Value?.ToString() ?? "NULL"));
            }
        }
    }
}
