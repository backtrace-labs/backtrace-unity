using Backtrace.Newtonsoft.Linq;

namespace Backtrace.Unity.Model.Database
{
    /// <summary>
    /// Counter data model
    /// </summary>
    internal class CounterData
    {
        private static int TOTAL_DEFAULT = 1;

        /// <summary>
        /// Determine how many records are the same in database
        /// </summary>
        public int Total { get; set; } = TOTAL_DEFAULT;


        /// <summary>
        /// Get default counter data json file
        /// </summary>
        /// <returns>Default counter data json file</returns>
        public static string DefaultJson()
        {
            return new BacktraceJObject
            {
                ["Total"] = TOTAL_DEFAULT
            }.ToString();
        }


        /// <summary>
        /// Convert counter data to JSON string
        /// </summary>
        /// <returns>Counter data JSON file</returns>
        public string ToJson()
        {
            return new BacktraceJObject
            {
                ["Total"] = Total
            }.ToString();
        }

        /// <summary>
        /// Deserialize counter data json file to counter data instance
        /// </summary>
        /// <param name="jToken">Counter data json string</param>
        /// <returns>Counter data instance</returns>
        public static CounterData Deserialize(string json)
        {
            var jObject = BacktraceJObject.Parse(json);
            return Deserialize(jObject);
        }

        /// <summary>
        /// Deserialize counter data json file to counter data instance
        /// </summary>
        /// <param name="jToken">Counter data json string</param>
        /// <returns>Counter data instance</returns>
        public static CounterData Deserialize(JToken jToken)
        {
            return new CounterData {
                Total = jToken["Total"].Value<int>()
            };

        }

    }
}