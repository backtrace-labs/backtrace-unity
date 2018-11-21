using Backtrace.Newtonsoft.Linq;
using Backtrace.Newtonsoft.Utilities;
using System;
using System.Collections.Generic;

namespace Backtrace.Newtonsoft.Converters
{
    public class HashSetConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            //throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var shouldReplace = serializer.ObjectCreationHandling == ObjectCreationHandling.Replace;

            //Return the existing value (or null if it doesn't exist)
            if (reader.TokenType == JsonToken.Null)
            {
                return shouldReplace ? null : existingValue;
            }

            //Dynamically create the HashSet
            var result = !shouldReplace && existingValue != null
                ? existingValue
                : Activator.CreateInstance(objectType);

            var genericType = objectType.GetGenericArguments()[0];
            var addMethod = objectType.GetMethod("Add");

            var jo = JArray.Load(reader);

            for (var i = 0; i < jo.Count; i++)
            {
                var itemValue = serializer.Deserialize(jo[i].CreateReader(), genericType);
                addMethod.Invoke(result, new[] { itemValue });
            }

            return result;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType() && objectType.GetGenericTypeDefinition() == typeof(HashSet<>);
        }

        public override bool CanWrite
        {
            get { return false; }
        }


    }
}
