using Backtrace.Unity.Json;
using Backtrace.Unity.Model;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
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
                "\r\n" +
                "}\r\n";
            Assert.AreEqual(expectedResult, json);
            yield return null;
        }
        [UnityTest]
        public IEnumerator TestDataSerialization_BasicVariableUsage_DataSerializeCorrectly()
        {
            var jObject = new BacktraceJObject()
            {
                ["agentName"] = "Backtrace-unity",
                ["number"] = 1,
                ["enabled"] = true
            };
            var json = jObject.ToJson();

            foreach (var keyValuePair in jObject.Source)
            {
                Assert.IsTrue(json.Contains(keyValuePair.Key));
                var prettyValue = keyValuePair.Value.GetType() == typeof(bool)
                    ? keyValuePair.Value.ToString().ToLower()
                    : keyValuePair.Value.ToString();

                Assert.IsTrue(json.Contains(prettyValue));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_ShouldEscapeInvalidStringValues_DataSerializeCorrectly()
        {
            var jObject = new BacktraceJObject();

            var invalidValue = string.Format("\"{0}\"", "foo");

            var primitiveValues = new Dictionary<string, object>
            {
                ["agentName"] = invalidValue,
                [invalidValue] = "foo"
            };
            foreach (var keyValuePair in primitiveValues)
            {
                jObject[keyValuePair.Key] = keyValuePair.Value;
            }
            var json = jObject.ToJson();
            foreach (var keyValuePair in primitiveValues)
            {
                Assert.IsTrue(json.Contains(keyValuePair.Key));
                Assert.IsTrue(json.Contains(Regex.Escape(keyValuePair.Value.ToString())));
            }

            yield return null;
        }


        [UnityTest]
        public IEnumerator TestDataSerialization_ShouldSerializeArray_DataSerializeCorrectly()
        {
            var classifiers = new string[] { "foo", "bar" };
            var numberClassifiers = new int[] { 1, 2, 3, 4 };
            var jObject = new BacktraceJObject();
            jObject["classifier"] = classifiers;
            jObject["numberClassifier"] = numberClassifiers;

            var json = jObject.ToJson();

            foreach (var classifier in classifiers)
            {
                Assert.IsTrue(json.Contains(classifier));
            }

            foreach (var classifier in numberClassifiers)
            {
                Assert.IsTrue(json.Contains(classifier.ToString()));
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_ShouldSerializeList_DataSerializeCorrectly()
        {
            var classifiers = new List<string>() { "foo", "bar" };
            var numberClassifiers = new List<int>() { 1, 2, 3, 4 };
            var jObject = new BacktraceJObject();
            jObject["classifier"] = classifiers;
            jObject["numberClassifier"] = numberClassifiers;

            var json = jObject.ToJson();

            foreach (var classifier in classifiers)
            {
                Assert.IsTrue(json.Contains(classifier));
            }

            foreach (var classifier in numberClassifiers)
            {
                Assert.IsTrue(json.Contains(classifier.ToString()));
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
               "\"foo\":null,\r\n" +
               "\"bar\":\"\"\r\n" +
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
            var classifiers = new string[] { "foo", "bar" };
            var numberClassifiers = new int[] { 1, 2, 3, 4 };
            var jObject = new BacktraceJObject();
            jObject["classifier"] = classifiers;
            jObject["numberClassifier"] = numberClassifiers;
            var inner = new BacktraceJObject();
            inner["foo"] = "bar";
            jObject["inner"] = inner;

            var json = jObject.ToJson();
            Assert.IsNotEmpty(json);
            yield return null;
        }
    }
}
