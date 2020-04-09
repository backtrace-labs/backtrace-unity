using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.TestTools;

namespace Tests
{
    public class BacktraceReportTests
    {
        private readonly Exception exception = new DivideByZeroException("fake exception message");
        private readonly Dictionary<string, string> reportAttributes = new Dictionary<string, string>()
            {
                { "test_attribute", "test_attribute_value" },
                { "temporary_attribute", "123" },
                { "temporary_attribute_bool", "true"}
            };
        private readonly List<string> attachemnts = new List<string>() { "path", "path2" };

        [UnityTest]
        public IEnumerator TestReportCreation_CreateCorrectReport_WithDiffrentConstructors()
        {
            Assert.DoesNotThrow(() => new BacktraceReport("message"));
            Assert.DoesNotThrow(() => new BacktraceReport("message", new Dictionary<string, string>(), new List<string>()));
            Assert.DoesNotThrow(() => new BacktraceReport("message", attachmentPaths: attachemnts));

            var exception = new FileNotFoundException();
            Assert.DoesNotThrow(() => new BacktraceReport(exception));
            Assert.DoesNotThrow(() => new BacktraceReport(exception, new Dictionary<string, string>(), new List<string>()));
            Assert.DoesNotThrow(() => new BacktraceReport(exception, attachmentPaths: attachemnts));
            yield return null;
        }
    }
}
