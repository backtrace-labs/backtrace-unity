using Backtrace.Unity.Json;
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
            var expectedResult = "{}";

            Assert.IsNotEmpty(json);
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
                DoubleNumber = 555.432d,
                LongNumber = 999
            };

            var jObject = new BacktraceJObject();
            jObject.Add("AgentName", sampleObject.AgentName);
            jObject.Add("Active", sampleObject.Active);
            jObject.Add("IntNumber", sampleObject.IntNumber);
            jObject.Add("FloatNumber", sampleObject.FloatNumber);
            jObject.Add("DoubleNumber", sampleObject.DoubleNumber);
            jObject.Add("LongNumber", sampleObject.LongNumber);


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
        public IEnumerator TestDataSerialization_BasicVariableUsageWithPreinitializedVariables_DataSerializeCorrectly()
        {
            var sampleObject = new SampleObject()
            {
                Active = true,
                AgentName = "Backtrace-unity",
                IntNumber = 1,
                FloatNumber = 12.123f,
                DoubleNumber = 555.432d,
                LongNumber = 999
            };

            var jObject = new BacktraceJObject(new Dictionary<string, string>()
                {
                    { "AgentName", sampleObject.AgentName }
                });

            jObject.Add("Active", sampleObject.Active);
            jObject.Add("IntNumber", sampleObject.IntNumber);
            jObject.Add("FloatNumber", sampleObject.FloatNumber);
            jObject.Add("DoubleNumber", sampleObject.DoubleNumber);
            jObject.Add("LongNumber", sampleObject.LongNumber);


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
        public IEnumerator TestDataSerialization_WithComplexValues_DataSerializeCorrectly()
        {
            var sampleObject = new SampleObject()
            {
                Active = true,
                AgentName = "Backtrace-unity",
                LongNumber = 999,
                NumberArray = new int[] { 1, 2, 3, 4 },
                StringArray = new string[] { string.Empty, null, "foo", "bar" },
                StringList = new List<string> { string.Empty, null, "foo", "bar" }
            };

            var jObject = new BacktraceJObject();
            jObject.Add("AgentName", sampleObject.AgentName);
            jObject.Add("Active", sampleObject.Active);
            jObject.Add("LongNumber", sampleObject.LongNumber);
            jObject.Add("NumberArray", sampleObject.NumberArray);
            jObject.Add("StringArray", sampleObject.StringArray);
            jObject.Add("StringList", sampleObject.StringList);


            var json = jObject.ToJson();

            var jsonObject = JsonUtility.FromJson<SampleObject>(json);

            Assert.AreEqual(sampleObject.AgentName, jsonObject.AgentName);
            // validate number array
            for (int i = 0; i < sampleObject.NumberArray.Length; i++)
            {
                Assert.AreEqual(jsonObject.NumberArray[i], sampleObject.NumberArray[i]);
            }
            // validate string array
            for (int i = 0; i < sampleObject.StringArray.Length; i++)
            {
                // handle empty strings
                var expectedValue = string.IsNullOrEmpty(sampleObject.StringArray[i]) ? string.Empty : sampleObject.StringArray[i];
                Assert.AreEqual(jsonObject.StringArray[i], expectedValue);
            }

            // validate string list
            for (int i = 0; i < sampleObject.StringList.Count; i++)
            {
                var expectedValue = string.IsNullOrEmpty(sampleObject.StringList[i]) ? string.Empty : sampleObject.StringList[i];
                Assert.AreEqual(jsonObject.StringList[i], expectedValue);
            }

            yield return null;
        }


        [UnityTest]
        public IEnumerator TestDataSerialization_WithInnerJObjectAndComplexArray_DataSerializeCorrectly()
        {
            // this test should validate if we can start analysing new data type without previous data types
            var sampleObject = new BaseJObject()
            {
                InnerObject = new SampleObject()
                {
                    NumberArray = new int[] { 1, 2, 3, 4 }
                }
            };

            var sampleJObject = new BacktraceJObject();
            var innerJObject = new BacktraceJObject();
            innerJObject.Add("NumberArray", sampleObject.InnerObject.NumberArray);
            sampleJObject.Add("InnerObject", innerJObject);


            var json = sampleJObject.ToJson();
            var jsonObject = JsonUtility.FromJson<BaseJObject>(json);

            Assert.IsNotNull(jsonObject);
            Assert.IsNotNull(jsonObject.InnerObject);
            // validate number array
            for (int i = 0; i < jsonObject.InnerObject.NumberArray.Length; i++)
            {
                Assert.AreEqual(jsonObject.InnerObject.NumberArray[i], jsonObject.InnerObject.NumberArray[i]);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_ShouldEscapeInvalidStringKey_DataSerializeCorrectly()
        {
            var invalidValue = string.Format("\"{0}\"", "foo");
            var jObject = new BacktraceJObject();
            jObject.Add(invalidValue, "foo");

            var json = jObject.ToJson();
            foreach (var keyValuePair in jObject.PrimitiveValues)
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

            var jObject = new BacktraceJObject();
            jObject.Add("AgentName", invalidValue);
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
            jObject.Add("classifier", classifiers);

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
            jObject.Add("StringArray", sampleObject.StringArray);
            jObject.Add("NumberArray", sampleObject.NumberArray);

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
            jObject.Add("StringList", sampleObject.StringList);

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
            jObject.Add("bar", string.Empty);
            jObject.Add("foo", null as string);

            var json = jObject.ToJson();

            var expectedResult = "{" +
                   "\"bar\":\"\"," +
                   "\"foo\":\"\"" +
                   "}";
            Assert.AreEqual(expectedResult, json);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_ShouldEscapeCorrectlyAllKeys_DataSerializeCorrectly()
        {
            var jObject = new BacktraceJObject();
            jObject.Add(@"foo""", string.Empty);
            jObject.Add("\\bar".ToString(), null as string);
            jObject.Add("b\naz".ToString(), null as string);

            var json = jObject.ToJson();

            var expectedResult = "{" +
                    "\"foo\\\"\":\"\"," +
                    "\"\\\\bar\":\"\"," +
                    "\"b\\naz\":\"\"" +
                    "}";
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

            jObject.Add("doubleQuote", expected.doubleQuote);
            jObject.Add("slash", expected.slash);
            jObject.Add("newLine", expected.newLine);
            jObject.Add("tab", expected.tab);
            jObject.Add("carriageReturn", expected.carriageReturn);


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
            jObject.Add("StringArray", sampleObject.StringArray);
            jObject.Add("NumberArray", sampleObject.NumberArray);

            var inner = new BacktraceJObject();
            inner.Add("foo", "bar");
            jObject.Add("inner", inner);

            var json = jObject.ToJson();
            Assert.IsNotEmpty(json);
            yield return null;
        }


        [UnityTest]
        public IEnumerator TestDataSerialization_WithOnlyComplexValues_DataSerializeCorrectly()
        {
            // this test should validate if we can start analysing new data type without previous data types
            var sampleObject = new SampleObject()
            {
                NumberArray = new int[] { 1, 2, 3, 4 }
            };

            var jObject = new BacktraceJObject();
            jObject.Add("NumberArray", sampleObject.NumberArray);


            var json = jObject.ToJson();
            var jsonObject = JsonUtility.FromJson<SampleObject>(json);
            // validate number array
            for (int i = 0; i < sampleObject.NumberArray.Length; i++)
            {
                Assert.AreEqual(jsonObject.NumberArray[i], sampleObject.NumberArray[i]);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_WithOnlyJObject_DataSerializeCorrectly()
        {
            // this test should validate if we can start analysing new data type without previous data types
            var sampleObject = new BaseJObject()
            {
                InnerObject = new SampleObject()
                {
                    Active = true
                }
            };

            var sampleJObject = new BacktraceJObject();
            var innerJObject = new BacktraceJObject();
            innerJObject.Add("Active", sampleObject.InnerObject.Active);
            sampleJObject.Add("InnerObject", innerJObject);


            var json = sampleJObject.ToJson();
            var jsonObject = JsonUtility.FromJson<BaseJObject>(json);

            Assert.IsNotNull(jsonObject);
            Assert.IsNotNull(jsonObject.InnerObject);
            Assert.IsTrue(jsonObject.InnerObject.Active);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_WithUserPredefinedValues_DataSerializeCorrectly()
        {
            // this test should validate if we can start analysing new data type without previous data types

            var sampleObject = new SampleObject()
            {
                AgentName = "Backtrace-unity",
                TestString = "Test string"
            };
            var jObject = new BacktraceJObject(new Dictionary<string, string>()
            {
                {"AgentName", sampleObject.AgentName },
                {"TestString", sampleObject.TestString }
            });


            var json = jObject.ToJson();
            var jsonObject = JsonUtility.FromJson<SampleObject>(json);
            Assert.IsNotNull(jsonObject);
            Assert.AreEqual(sampleObject.AgentName, jsonObject.AgentName);
            Assert.AreEqual(sampleObject.TestString, jsonObject.TestString);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestDataSerialization_WithAllTypeOfPossibleJsonTypes_DataSerializeCorrectly()
        {
            var sampleObject = new BaseJObject()
            {
                InnerObject = new SampleObject()
                {
                    Active = true,
                    AgentName = "Backtrace-unity",
                    IntNumber = 1,
                    FloatNumber = 12.123f,
                    DoubleNumber = 555.432d,
                    LongNumber = 999,
                    NumberArray = new int[] { 1, 2, 3, 4 },
                    StringArray = new string[] { string.Empty, null, "foo", "bar" },
                    StringList = new List<string> { string.Empty, null, "foo", "bar" }
                }
            };

            var sampleJObject = new BacktraceJObject();
            var innerJObject = new BacktraceJObject();
            innerJObject.Add("AgentName", sampleObject.InnerObject.AgentName);
            innerJObject.Add("Active", sampleObject.InnerObject.Active);
            innerJObject.Add("IntNumber", sampleObject.InnerObject.IntNumber);
            innerJObject.Add("FloatNumber", sampleObject.InnerObject.FloatNumber);
            innerJObject.Add("DoubleNumber", sampleObject.InnerObject.DoubleNumber);
            innerJObject.Add("LongNumber", sampleObject.InnerObject.LongNumber);
            innerJObject.Add("NumberArray", sampleObject.InnerObject.NumberArray);
            innerJObject.Add("StringArray", sampleObject.InnerObject.StringArray);
            innerJObject.Add("StringList", sampleObject.InnerObject.StringList);

            sampleJObject.Add("InnerObject", innerJObject);


            var json = sampleJObject.ToJson();
            var jsonObject = JsonUtility.FromJson<BaseJObject>(json);

            // validate number array
            var jsonInnerObject = jsonObject.InnerObject;
            for (int i = 0; i < sampleObject.InnerObject.NumberArray.Length; i++)
            {
                Assert.AreEqual(sampleObject.InnerObject.NumberArray[i], jsonInnerObject.NumberArray[i]);
            }
            // validate string array
            for (int i = 0; i < sampleObject.InnerObject.StringArray.Length; i++)
            {
                // handle empty strings
                var expectedValue = string.IsNullOrEmpty(sampleObject.InnerObject.StringArray[i]) ? string.Empty : sampleObject.InnerObject.StringArray[i];
                Assert.AreEqual(expectedValue, jsonInnerObject.StringArray[i]);
            }

            // validate string list
            for (int i = 0; i < sampleObject.InnerObject.StringList.Count; i++)
            {
                // handle empty strings
                var expectedValue = string.IsNullOrEmpty(sampleObject.InnerObject.StringList[i]) ? string.Empty : sampleObject.InnerObject.StringList[i];
                Assert.AreEqual(expectedValue, jsonInnerObject.StringList[i]);
            }


            Assert.AreEqual(sampleObject.InnerObject.AgentName, jsonObject.InnerObject.AgentName);
            Assert.AreEqual(sampleObject.InnerObject.Active, jsonObject.InnerObject.Active);
            Assert.AreEqual(sampleObject.InnerObject.IntNumber, jsonObject.InnerObject.IntNumber);
            Assert.AreEqual(sampleObject.InnerObject.FloatNumber, jsonObject.InnerObject.FloatNumber);
            Assert.AreEqual(sampleObject.InnerObject.DoubleNumber, jsonObject.InnerObject.DoubleNumber);
            Assert.AreEqual(sampleObject.InnerObject.LongNumber, jsonObject.InnerObject.LongNumber);
            yield return null;
        }


        [UnityTest]
        public IEnumerator TestStringEscapingInSerialization_WithUserPredefinedValues_DataSerializeCorrectly()
        {
            // this test should validate if we can escape correctly strings from user predefined values

            var sampleObject = new SampleObject()
            {
                AgentName = "\"\\!@#$%^&*()+_=-{}{[]:\\\n\r\f\r ",
                TestString = "\b\t\\\n\"''"
            };
            var jObject = new BacktraceJObject(new Dictionary<string, string>()
            {
                {"AgentName", sampleObject.AgentName },
                {"TestString", sampleObject.TestString }
            });


            var json = jObject.ToJson();
            var jsonObject = JsonUtility.FromJson<SampleObject>(json);
            Assert.IsNotNull(jsonObject);
            Assert.AreEqual(sampleObject.AgentName, jsonObject.AgentName);
            Assert.AreEqual(sampleObject.TestString, jsonObject.TestString);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestStringEscapingInSerialization_WithPrimitiveValues_DataSerializeCorrectly()
        {
            // this test should validate if we can escape correctly strings from user predefined values

            var sampleObject = new SampleObject()
            {
                AgentName = "\"\\!@#$%^&*()+_=-{}{[]:\\\n\r\f\r ",
                TestString = "\b\t\\\n\"''"
            };
            var jObject = new BacktraceJObject();
            jObject.Add("AgentName", sampleObject.AgentName);
            jObject.Add("TestString", sampleObject.TestString);


            var json = jObject.ToJson();
            var jsonObject = JsonUtility.FromJson<SampleObject>(json);
            Assert.IsNotNull(jsonObject);
            Assert.AreEqual(sampleObject.AgentName, jsonObject.AgentName);
            Assert.AreEqual(sampleObject.TestString, jsonObject.TestString);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestStringEscapingInSerialization_WithJObject_DataSerializeCorrectly()
        {
            // this test should validate if we can escape correctly strings from user predefined values

            var sampleObject = new BaseJObject()
            {
                InnerObject = new SampleObject()
                {
                    AgentName = "\"\\!@#$%^&*()+_=-{}{[]:\\\n\r\f\r ",
                    TestString = "\b\t\\\n\"''"
                }
            };
            var jObject = new BacktraceJObject();
            var innerJObject = new BacktraceJObject();
            innerJObject.Add("AgentName", sampleObject.InnerObject.AgentName);
            innerJObject.Add("TestString", sampleObject.InnerObject.TestString);
            jObject.Add("InnerObject", innerJObject);


            var json = jObject.ToJson();
            var jsonObject = JsonUtility.FromJson<BaseJObject>(json);
            Assert.IsNotNull(jsonObject);
            Assert.AreEqual(sampleObject.InnerObject.AgentName, jsonObject.InnerObject.AgentName);
            Assert.AreEqual(sampleObject.InnerObject.TestString, jsonObject.InnerObject.TestString);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestStringEscapingInSerialization_WithComplexValues_DataSerializeCorrectly()
        {
            // this test should validate if we can escape correctly strings from user predefined values

            var sampleObject = new SampleObject()
            {
                StringArray = new string[] { "\"\\!@#$%^&*()+_=-{}{[]:\\\n\r\f\r ", "\b\t\\\n\"''" },
                StringList = new List<string>() { "\"\\!@#$%^&*()+_=-{}{[]:\\\n\r\f\r ", "\b\t\\\n\"''" },
                NumberArray = null
            };

            var jObject = new BacktraceJObject();
            jObject.Add("StringArray", sampleObject.StringArray);
            jObject.Add("StringList", sampleObject.StringList);
            jObject.Add("NumberArray", sampleObject.NumberArray);


            var json = jObject.ToJson();
            var jsonObject = JsonUtility.FromJson<SampleObject>(json);
            Assert.IsNotNull(jsonObject);
            Assert.IsEmpty(jsonObject.NumberArray);

            for (int i = 0; i < sampleObject.StringArray.Length; i++)
            {
                // handle empty strings
                Assert.AreEqual(jsonObject.StringArray[i], sampleObject.StringArray[i]);
            }

            // validate string list
            for (int i = 0; i < sampleObject.StringList.Count; i++)
            {

                Assert.AreEqual(jsonObject.StringList[i], sampleObject.StringList[i]);
            }

            yield return null;
        }
    }
}
