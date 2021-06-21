using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Backtrace.Unity.Json
{
    /// <summary>
    /// Backtrace JSON object representation
    /// </summary>
    public class BacktraceJObject
    {
        /// <summary>
        /// JSON object source - primitive values
        /// </summary>
        internal readonly Dictionary<string, string> PrimitiveValues = new Dictionary<string, string>();

        /// <summary>
        /// JSON object source - primitive values
        /// </summary>
        internal readonly IDictionary<string, string> UserPrimitives;

        /// <summary>
        /// Inner objects
        /// </summary>
        internal readonly Dictionary<string, BacktraceJObject> InnerObjects = new Dictionary<string, BacktraceJObject>();

        /// <summary>
        /// Complex objects - array of JObjects/strings
        /// </summary>
        internal readonly Dictionary<string, object> ComplexObjects = new Dictionary<string, object>();


        public BacktraceJObject() : this(null) { }

        public BacktraceJObject(IDictionary<string, string> source)
        {
            UserPrimitives = source == null ? new Dictionary<string, string>() : source;
        }


        /// <summary>
        /// Add boolean key-value pair to JSON object
        /// </summary>
        /// <param name="key">JSON key</param>
        /// <param name="value">value</param>
        public void Add(string key, bool value)
        {
            PrimitiveValues.Add(key, value.ToString(CultureInfo.InvariantCulture).ToLower());
        }



        /// <summary>
        /// Add key-value pair to JSON object
        /// </summary>
        /// <param name="key">JSON key</param>
        /// <param name="value">value</param>
        public void Add(string key, float value, string format = "G")
        {
            PrimitiveValues.Add(key, value.ToString(format, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Add key-value pair to JSON object
        /// </summary>
        /// <param name="key">JSON key</param>
        /// <param name="value">value</param>
        public void Add(string key, double value, string format = "G")
        {
            PrimitiveValues.Add(key, value.ToString(format, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Add string key-value pair to JSON object
        /// </summary>
        /// <param name="key">JSON key</param>
        /// <param name="value">value</param>
        public void Add(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                // be sure that we avoid using null here
                // to avoid null conficts.
                value = string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("\"");
            EscapeString(value, builder);
            builder.Append("\"");

            PrimitiveValues.Add(key, builder.ToString());
        }

        /// <summary>
        /// Add long key-value pair to JSON object
        /// </summary>
        /// <param name="key">JSON key</param>
        /// <param name="value">value</param>
        public void Add(string key, long value)
        {
            PrimitiveValues.Add(key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Add BacktraceJObject key-value pair to JSON object
        /// </summary>
        /// <param name="key">JSON key</param>
        /// <param name="value">value</param>
        public void Add(string key, BacktraceJObject value)
        {
            if (value != null)
            {
                InnerObjects.Add(key, value);
            }
            else
            {
                ComplexObjects.Add(key, null);

            }
        }
        /// <summary>
        /// Add ienumerable object key-value pair to JSON object
        /// </summary>
        /// <param name="key">JSON key</param>
        /// <param name="value">value</param>
        public void Add(string key, IEnumerable value)
        {
            ComplexObjects.Add(key, value);
        }

        /// <summary>
        /// Convert BacktraceJObject to JSON
        /// </summary>
        /// <returns>BacktraceJObject JSON representation</returns>
        public string ToJson()
        {
            var stringBuilder = new StringBuilder();
            ToJson(stringBuilder);
            return stringBuilder.ToString();
        }

        internal void ToJson(StringBuilder stringBuilder)
        {
            stringBuilder.Append("{");
            AppendPrimitives(stringBuilder);
            AddUserPrimitives(stringBuilder);
            AppendJObjects(stringBuilder);
            AppendComplexValues(stringBuilder);
            stringBuilder.Append("}");
        }

        private void AddUserPrimitives(StringBuilder stringBuilder)
        {
            if (UserPrimitives.Count == 0)
            {
                return;
            }
            int propertyIndex = 0;
            if (ShouldContinueAddingJSONProperties(stringBuilder))
            {
                stringBuilder.Append(',');
            }
            using (var enumerator = UserPrimitives.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    propertyIndex++;
                    var entry = enumerator.Current;
                    AppendKey(entry.Key, stringBuilder);
                    if (string.IsNullOrEmpty(entry.Value))
                    {
                        stringBuilder.Append("\"\"");
                    }
                    else
                    {
                        stringBuilder.Append("\"");
                        EscapeString(entry.Value, stringBuilder);
                        stringBuilder.Append("\"");
                    }
                    if (propertyIndex != UserPrimitives.Count)
                    {
                        stringBuilder.Append(",");
                    }
                }
            }
        }

        private void AppendPrimitives(StringBuilder stringBuilder)
        {
            int propertyIndex = 0;
            using (var enumerator = PrimitiveValues.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {

                    propertyIndex++;
                    var entry = enumerator.Current;
                    AppendKey(entry.Key, stringBuilder);
                    stringBuilder.Append(string.IsNullOrEmpty(entry.Value) ? "\"\"" : entry.Value);

                    if (propertyIndex != PrimitiveValues.Count)
                    {
                        stringBuilder.Append(",");
                    }
                }
            }
        }

        private void AppendJObjects(StringBuilder stringBuilder)
        {
            if (InnerObjects.Count == 0)
            {
                return;
            }
            int propertyIndex = 0;
            using (var enumerator = InnerObjects.GetEnumerator())
            {
                if (ShouldContinueAddingJSONProperties(stringBuilder))
                {
                    stringBuilder.Append(',');
                }
                while (enumerator.MoveNext())
                {
                    propertyIndex++;
                    var entry = enumerator.Current;
                    AppendKey(entry.Key, stringBuilder);
                    entry.Value.ToJson(stringBuilder);
                    if (propertyIndex != InnerObjects.Count)
                    {
                        stringBuilder.Append(",");
                    }
                }
            }
        }

        private void AppendComplexValues(StringBuilder stringBuilder)
        {
            if (ComplexObjects.Count == 0)
            {
                return;
            }
            int propertyIndex = 0;
            using (var enumerator = ComplexObjects.GetEnumerator())
            {
                if (ShouldContinueAddingJSONProperties(stringBuilder))
                {
                    stringBuilder.Append(',');
                }
                while (enumerator.MoveNext())
                {
                    propertyIndex++;
                    var entry = enumerator.Current;
                    AppendKey(entry.Key, stringBuilder);
                    if (entry.Value == null)
                    {
                        stringBuilder.Append("null");
                    }
                    else if (entry.Value is IEnumerable && !(entry.Value is IDictionary))
                    {
                        stringBuilder.Append('[');
                        int index = 0;
                        foreach (var item in (entry.Value as IEnumerable))
                        {
                            if (index != 0)
                            {
                                stringBuilder.Append(',');
                            }
                            if (item == null)
                            {
                                stringBuilder.Append("\"\"");
                            }
                            else if (item is BacktraceJObject)
                            {
                                (item as BacktraceJObject).ToJson(stringBuilder);
                            }
                            else
                            {
                                stringBuilder.Append("\"");
                                EscapeString(item.ToString(), stringBuilder);
                                stringBuilder.Append("\"");
                            }
                            index++;
                        }
                        stringBuilder.Append(']');
                    }

                    if (propertyIndex != ComplexObjects.Count)
                    {
                        stringBuilder.Append(",");
                    }
                }
            }
        }

        private bool ShouldContinueAddingJSONProperties(StringBuilder stringBuilder)
        {
            return stringBuilder[stringBuilder.Length - 1] != ',' && stringBuilder[stringBuilder.Length - 1] != '{';
        }


        private void AppendKey(string value, StringBuilder builder)
        {
            builder.Append("\"");
            if (string.IsNullOrEmpty(value))
            {
                builder.Append("\"\"");
            }
            else
            {
                EscapeString(value, builder);
            }
            builder.Append("\":");
        }


        /// <summary>
        /// Escape special characters in string 
        /// </summary>
        /// <param name="value">string to escape</param>
        /// <returns>escaped string</returns>
        private void EscapeString(string value, StringBuilder output)
        {
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                        output.AppendFormat("\\\\");
                        break;
                    case '"':
                        output.AppendFormat("\\\"");
                        break;
                    case '\b':
                        output.Append("\\b");
                        break;
                    case '\t':
                        output.Append("\\t");
                        break;
                    case '\n':
                        output.Append("\\n");
                        break;
                    case '\f':
                        output.Append("\\f");
                        break;
                    case '\r':
                        output.Append("\\r");
                        break;
                    default:
                        output.Append(c);
                        break;
                }
            }
        }
    }
}
