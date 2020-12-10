using System;
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
        public readonly Dictionary<string, string> PrimitiveValues = new Dictionary<string, string>();

        /// <summary>
        /// JSON object source - primitive values
        /// </summary>
        public readonly Dictionary<string, string> UserPrimitives;

        /// <summary>
        /// Inner objects
        /// </summary>
        public readonly Dictionary<string, BacktraceJObject> InnerObjects = new Dictionary<string, BacktraceJObject>();

        /// <summary>
        /// Complex objects - array of JObjects/strings
        /// </summary>
        public readonly Dictionary<string, object> ComplexObjects = new Dictionary<string, object>();


        public BacktraceJObject() : this(null) { }

        public BacktraceJObject(Dictionary<string, string> source)
        {
            UserPrimitives = source == null ? new Dictionary<string, string>() : source;
        }

        public object this[string key]
        {
            set
            {
                if (value == null)
                {
                    PrimitiveValues.Add(key, "null");
                    return;
                }

                var analysedType = value.GetType();
                if (analysedType == typeof(string))
                {
                    StringBuilder builder = new StringBuilder();
                    builder.Append("\"");
                    EscapeString(value as string, builder);
                    builder.Append("\"");

                    PrimitiveValues.Add(key, builder.ToString());
                }
                else if (analysedType == typeof(double))
                {
                    PrimitiveValues.Add(key, Convert.ToDouble(value, CultureInfo.CurrentCulture).ToString("G", CultureInfo.InvariantCulture));
                }
                else if (analysedType == typeof(float))
                {
                    PrimitiveValues.Add(key, Convert.ToDouble(value, CultureInfo.CurrentCulture).ToString("G", CultureInfo.InvariantCulture));
                }
                else if (analysedType == typeof(int))
                {
                    PrimitiveValues.Add(key, Convert.ToInt32(value, CultureInfo.CurrentCulture).ToString());
                }
                else if (analysedType == typeof(long))
                {
                    PrimitiveValues.Add(key, Convert.ToInt64(value, CultureInfo.CurrentCulture).ToString());
                }
                else if (analysedType == typeof(bool))
                {
                    PrimitiveValues.Add(key, ((bool)value).ToString().ToLower());
                }
                else if (value is IEnumerable && !(value is IDictionary))
                {
                    ComplexObjects.Add(key, value);
                }
                else if (Guid.TryParse(value.ToString(), out Guid guidResult))
                {
                    PrimitiveValues.Add(key, string.Format("\"{0}\"", guidResult.ToString()));
                }
                else
                {
                    //check if this is json inner object
                    var backtraceJObjectValue = value as BacktraceJObject;
                    if (backtraceJObjectValue != null)
                    {
                        InnerObjects.Add(key, backtraceJObjectValue);
                    }
                    else
                    {
                        ComplexObjects.Add(key, null);
                    }
                }
            }
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
            var enumerator = UserPrimitives.GetEnumerator();
            if (stringBuilder[stringBuilder.Length - 1] != ',' && stringBuilder[stringBuilder.Length - 1] != '{')
            {
                stringBuilder.Append(',');
            }
            while (enumerator.MoveNext())
            {
                propertyIndex++;
                var entry = enumerator.Current;
                AppendKey(entry.Key, stringBuilder);
                if (string.IsNullOrEmpty(entry.Value))
                {
                    stringBuilder.Append("null");
                }
                else
                {
                    EscapeString(entry.Value, stringBuilder);
                }
                if (propertyIndex != UserPrimitives.Count)
                {
                    stringBuilder.Append(",");
                }
            }
        }

        private void AppendPrimitives(StringBuilder stringBuilder)
        {
            int propertyIndex = 0;
            var enumerator = PrimitiveValues.GetEnumerator();
            while (enumerator.MoveNext())
            {

                propertyIndex++;
                var entry = enumerator.Current;
                AppendKey(entry.Key, stringBuilder);
                stringBuilder.Append(string.IsNullOrEmpty(entry.Value) ? "null" : entry.Value);

                if (propertyIndex != PrimitiveValues.Count)
                {
                    stringBuilder.Append(",");
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
            var enumerator = InnerObjects.GetEnumerator();
            if (stringBuilder[stringBuilder.Length - 1] != ',' && stringBuilder[stringBuilder.Length - 1] != '{')
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

        private void AppendComplexValues(StringBuilder stringBuilder)
        {
            if (ComplexObjects.Count == 0)
            {
                return;
            }
            int propertyIndex = 0;
            var enumerator = ComplexObjects.GetEnumerator();
            if (stringBuilder[stringBuilder.Length - 1] != ',' && stringBuilder[stringBuilder.Length - 1] != '{')
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
                        if (item is BacktraceJObject)
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


        private void AppendKey(string value, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(value);
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
