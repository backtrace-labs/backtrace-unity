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
        /// JSON object source
        /// </summary>
        public readonly Dictionary<string, object> Source = new Dictionary<string, object>();

        public BacktraceJObject() : this(null) { }
        public BacktraceJObject(Dictionary<string, object> source)
        {
            if (source == null)
            {
                return;
            }
            Source = source;
        }

        public object this[string key]
        {
            get
            {
                return Source[key];
            }
            set
            {
                Source[key] = value;
            }
        }

        /// <summary>
        /// Convert BacktraceJObject to JSON
        /// </summary>
        /// <returns>BacktraceJObject JSON representation</returns>
        public string ToJson()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("{");

            foreach (var entry in Source)
            {
                // add to each property (expect first one) 
                // ,\n for fomarring reasons. 
                // without adding this condition at the beginning of the string builder,
                // we will generate invalid json with `,` character in the end of the json properties.
                if (stringBuilder.Length != 3)
                {
                    stringBuilder.Append(",");
                    stringBuilder.AppendLine();
                }
                // add key
                stringBuilder.AppendFormat("\"{0}\":", EscapeString(entry.Key));
                // add value to key
                stringBuilder.Append(ConvertValue(entry.Value));
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("}");

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Escape special characters in string 
        /// </summary>
        /// <param name="value">string to escape</param>
        /// <returns>escaped string</returns>
        private string EscapeString(string value)
        {
            var output = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                        output.AppendFormat("{0}{0}", '\\');
                        break;

                    case '"':
                        output.AppendFormat("{0}{1}", '\\', '"');
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

            return output.ToString();
        }


        /// <summary>
        /// Convert object to json value
        /// </summary>
        /// <param name="value">object value</param>
        /// <returns>json value</returns>
        private string ConvertValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            var analysedType = value.GetType();

            if (analysedType == typeof(bool))
            {
                return ((bool)value).ToString().ToLower();
            }
            else if (analysedType == typeof(double))
            {
                return Convert.ToDouble(value, CultureInfo.CurrentCulture).ToString(CultureInfo.InvariantCulture);
            }
            else if (analysedType == typeof(float))
            {
                return Convert.ToDouble(value, CultureInfo.CurrentCulture).ToString(CultureInfo.InvariantCulture);
            }
            else if (analysedType == typeof(int))
            {
                return Convert.ToInt32(value, CultureInfo.CurrentCulture).ToString();
            }
            else if (analysedType == typeof(long))
            {
                return Convert.ToInt64(value, CultureInfo.CurrentCulture).ToString();
            }
            else if (analysedType == typeof(string))
            {
                return string.Format("\"{0}\"", EscapeString(value as string));
            }
            else if (value is IEnumerable && !(value is IDictionary))
            {
                var collection = (value as IEnumerable);
                var builder = new StringBuilder();
                builder.Append('[');
                int index = 0;
                foreach (var item in collection)
                {
                    if (index != 0)
                    {
                        builder.Append(',');
                    }
                    builder.Append(ConvertValue(item));
                    index++;
                }
                builder.Append(']');
                return builder.ToString();
            }
            else if (Guid.TryParse(value.ToString(), out Guid guidResult))
            {
                return string.Format("\"{0}\"", guidResult.ToString());
            }
            else
            {
                //check if this is json inner object
                var backtraceJObjectValue = value as BacktraceJObject;
                if (backtraceJObjectValue != null)
                {
                    return backtraceJObjectValue.ToJson();
                }

                return "null";
            }

        }
    }
}
