using Backtrace.Unity.Json;
using Backtrace.Unity.Model;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime
{
    public class BacktraceJObjectTests
    {

        [UnityTest]
        public IEnumerator TestDataSerialization_EmptyJson_ShouldCreateEmptyJson()
        {
            var jObject = new BacktraceJObject();
            var json = jObject.ToJson();
            Assert.IsNotEmpty(json);

            var expectedResult = "{\r\n" +
                "}\r\n";
            Assert.AreEqual(expectedResult, json);
            yield return null;
        }
        [UnityTest]
        public IEnumerator TestDataSerialization_BasicVariableUsage_DataSerializeCorrectly()
        {
            var sampleObject = new SampleObject()
            {
                Active = true,
                AgentName = "Backtrace-unity",
                IntNumber = 1,
                FloatNumber = 12.123f,
                DoubleNumber = 123.123d,
                LongNumber = 123
            };

            var jObject = new BacktraceJObject()
            {
                ["AgentName"] = sampleObject.AgentName,
                ["Active"] = sampleObject.Active,
                ["IntNumber"] = sampleObject.IntNumber,
                ["FloatNumber"] = sampleObject.FloatNumber,
                ["DoubleNumber"] = sampleObject.DoubleNumber,
                ["LongNumber"] = sampleObject.LongNumber,
            };

            var json = jObject.ToJson();

            var jsonObject = JsonUtility.FromJson<SampleObject>(json);

            Assert.AreEqual(sampleObject.AgentName, jsonObject.AgentName);
            Assert.AreEqual(sampleObject.Active, jsonObject.Active);
            Assert.AreEqual(sampleObject.IntNumber, jsonObject.IntNumber);
            Assert.AreEqual(sampleObject.FloatNumber, jsonObject.FloatNumber);
            Assert.AreEqual(sampleObject.DoubleNumber, jsonObject.DoubleNumber);
            Assert.AreEqual(sampleObject.LongNumber, jsonObject.LongNumber);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_ShouldEscapeInvalidStringKey_DataSerializeCorrectly()
        {
            var invalidValue = string.Format("\"{0}\"", "foo");
            var jObject = new BacktraceJObject()
            {
                [invalidValue] = "foo"
            };

            var json = jObject.ToJson();
            foreach (var keyValuePair in jObject.Source)
            {
                Assert.IsTrue(json.Contains(keyValuePair.Key));
                Assert.IsTrue(json.Contains(Regex.Escape(keyValuePair.Value.ToString())));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_ShouldEscapeInvalidStringValues_DataSerializeCorrectly()
        {
            var invalidValue = string.Format("\"{0}\"", "foo");
            var sampleObject = new SampleObject()
            {
                AgentName = invalidValue
            };

            var jObject = new BacktraceJObject()
            {
                ["AgentName"] = invalidValue
            };
            var json = jObject.ToJson();
            var deserializedObject = JsonUtility.FromJson<SampleObject>(json);
            Assert.AreEqual(sampleObject.AgentName, deserializedObject.AgentName);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_ShouldSerializeDictionary_ShouldntSerializeDictionary()
        {
            var classifiers = new Dictionary<string, string> { { "foo", "bar" } };
            var jObject = new BacktraceJObject();
            jObject["classifier"] = classifiers;

            var json = jObject.ToJson();
            Assert.IsNotEmpty(json);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_ShouldSerializeArray_DataSerializeCorrectly()
        {
            var sampleObject = new SampleObject()
            {
                StringArray = new string[] { "foo", "bar" },
                NumberArray = new int[] { 1, 2, 3, 4 }
            };

            var jObject = new BacktraceJObject();
            jObject["StringArray"] = sampleObject.StringArray;
            jObject["NumberArray"] = sampleObject.NumberArray;

            var json = jObject.ToJson();
            var deserializedObject = JsonUtility.FromJson<SampleObject>(json);

            for (int i = 0; i < sampleObject.StringArray.Length; i++)
            {
                Assert.AreEqual(sampleObject.StringArray[i], deserializedObject.StringArray[i]);
            }

            for (int i = 0; i < sampleObject.NumberArray.Length; i++)
            {
                Assert.AreEqual(sampleObject.NumberArray[i], deserializedObject.NumberArray[i]);
            }


            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_ShouldSerializeList_DataSerializeCorrectly()
        {
            var sampleObject = new SampleObject()
            {
                StringList = new List<string>() { "foo", "bar" }
            };

            var jObject = new BacktraceJObject();
            jObject["StringList"] = sampleObject.StringList;

            var json = jObject.ToJson();
            var deserializedObject = JsonUtility.FromJson<SampleObject>(json);

            for (int i = 0; i < sampleObject.StringList.Count; i++)
            {
                Assert.AreEqual(sampleObject.StringList.ElementAt(i), deserializedObject.StringList.ElementAt(i));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_ShouldSerializeEmptyOrNullableValues_DataSerializeCorrectly()
        {
            var jObject = new BacktraceJObject();
            jObject["foo"] = null;
            jObject["bar"] = string.Empty;

            var json = jObject.ToJson();
            var expectedResult = "{\r\n" +
               "\"foo\": null," +
               "\"bar\": \"\"" +
               "}\r\n";
            Assert.AreEqual(expectedResult, json);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_JsonWithCharactersToEscape_ShouldEscapeCorrectly()
        {
            var expected = new EmptyCharacters()
            {
                doubleQuote = "\"",
                slash = "\\",
                newLine = "\n",
                tab = "\t",
                carriageReturn = "\r"
            };

            var jObject = new BacktraceJObject();
            jObject["doubleQuote"] = expected.doubleQuote;
            jObject["slash"] = expected.slash;
            jObject["newLine"] = expected.newLine;
            jObject["tab"] = expected.tab;
            jObject["carriageReturn"] = expected.carriageReturn;


            var json = jObject.ToJson();
            var emptyChars = JsonUtility.FromJson<EmptyCharacters>(json);
            Assert.AreEqual(expected.doubleQuote, emptyChars.doubleQuote);
            Assert.AreEqual(expected.slash, emptyChars.slash);
            Assert.AreEqual(expected.newLine, emptyChars.newLine);
            Assert.AreEqual(expected.tab, emptyChars.tab);
            Assert.AreEqual(expected.carriageReturn, emptyChars.carriageReturn);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_ShouldSerializeInnerJObject_DataSerializeCorrectly()
        {
            var sampleObject = new SampleObject()
            {
                StringArray = new string[] { "foo", "bar" },
                NumberArray = new int[] { 1, 2, 3, 4 }
            };

            var jObject = new BacktraceJObject();
            jObject["StringArray"] = sampleObject.StringArray;
            jObject["NumberArray"] = sampleObject.NumberArray;

            var inner = new BacktraceJObject();
            inner["foo"] = "bar";
            jObject["inner"] = inner;

            var json = jObject.ToJson();
            Assert.IsNotEmpty(json);
            yield return null;
        }
    }
}
