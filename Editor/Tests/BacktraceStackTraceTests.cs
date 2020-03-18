using Backtrace.Unity;
using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class BacktraceStackTraceTests
    {
        private readonly List<SampleStackFrame> _advancedStack = new List<SampleStackFrame>()
            {
                new SampleStackFrame(){
                    Method ="InvalidSignatureException: Return type System.Void of GenerateSubmissionUrl_FromValidHostName_ValidSubmissionUrl in Tests.BacktraceCredentialsTests is not supported."
                },
                new SampleStackFrame()
                {
                    Method ="UnityEngine.TestTools.TestRunner.TestEnumeratorWrapper.GetEnumerator(NUnit.Framework.Internal.ITestExecutionContext context)",
                    LineNumber = 31,
                    Path = "C:/ buildslave / unity / build / Extensions / TestRunner / UnityEngine.TestRunner / TestRunner / TestEnumeratorWrapper.cs"
                },
                new SampleStackFrame()
                {
                    Method ="UnityEngine.TestTools.EnumerableTestMethodCommand +< ExecuteEnumerable > c__Iterator0.MoveNext()",
                    LineNumber = 112,
                    Path = "C:/ buildslave / unity / build / Extensions / TestRunner / UnityEngine.TestRunner / NUnitExtensions / Commands / EnumerableTestMethodCommand.cs"
                },
                new SampleStackFrame()
                {
                    Method = "UnityEngine.TestTools.EnumerableSetUpTearDownCommand +< ExecuteEnumerable > c__Iterator0.MoveNext()",
                    LineNumber = 71,
                    Path = "C:/ buildslave / unity / build / Extensions / TestRunner / UnityEngine.TestRunner / NUnitExtensions / Commands / EnumerableTestMethodCommand.cs"
                },
                new SampleStackFrame()
                {
                    Method = "UnityEngine.TestRunner.NUnitExtensions.Runner.UnityLogCheckDelegatingCommand +< ExecuteEnumerable > c__Iterator0.MoveNext()",
                    LineNumber = 67,
                    Path = "C:/ buildslave / unity / build / Extensions / TestRunner / UnityEngine.TestRunner / NUnitExtensions / Runner / UnityLogCheckDelegatingCommand.cs"
                },
            };

        private readonly List<SampleStackFrame> _simpleStack = new List<SampleStackFrame>()
        {
            new SampleStackFrame()
                {
                    Method ="Startup.GetRandomFileStream ()",
                    LineNumber = 104,
                    Path = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    Method ="Startup.GetRandomFile ()",
                    LineNumber = 99,
                    Path = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    Method ="Startup.ReadRandomFile ()",
                    LineNumber = 94,
                    Path = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    Method ="Startup.DoSomethingDifferent ()",
                    LineNumber = 89,
                    Path = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    Method ="Startup.DoSomethingElse ()",
                    LineNumber = 84,
                    Path = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    Method ="Startup.DoSomething ()",
                    LineNumber = 80,
                    Path = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    Method ="Startup.Update ()",
                    LineNumber = 116,
                    Path = "Assets/Startup.cs"
                }
        };

     

        [UnityTest]
        public IEnumerator TestReportStackTrace_StackTraceShouldBeTheSameLikeExceptionStackTrace_ShouldReturnCorrectStackTrace()
        {
            var exception = new Exception("exception");
            var report = new BacktraceReport(exception);
            Assert.AreEqual(report.DiagnosticStack.Count, exception.StackTrace.Count());
            yield return null;
        }


        [UnityTest]
        public IEnumerator TestReportStackTrace_StackTraceShouldIncludeEnvironmentStackTrace_ShouldReturnCorrectStackTrace()
        {
            var environmentStackTrace = new StackTrace(true);
            var report = new BacktraceReport("msg");
            Assert.AreEqual(report.DiagnosticStack.Count, environmentStackTrace.FrameCount);
            yield return null;
        }


        [UnityTest]
        public IEnumerator TestStackTraceCreation_EmptyStackTrace_ValidStackTraceObject()
        {
            var backtraceStackTrace = new BacktraceStackTrace(new Exception());
            Assert.IsTrue(backtraceStackTrace.StackFrames.Count == 0);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TestStackTraceCreation_UnityEngineException_ValidStackTraceObject()
        {
            var source = new List<List<SampleStackFrame>>() { _simpleStack, _advancedStack };
            foreach (var data in source)
            {
                var stackTrace = string.Concat(data.Select(n => n.ToString()));
                string message = "message";
                var exception = new BacktraceUnhandledException(message, stackTrace);
                var backtraceStackTrace = new BacktraceStackTrace(exception);

                //skip first frame
                int startIndex = exception.Header ? 1 : 0;
                for (int i = startIndex; i < backtraceStackTrace.StackFrames.Count; i++)
                {
                    var backtraceStackFrame = backtraceStackTrace.StackFrames[i - startIndex];
                    var realStackFrame = data[i];
                    Assert.AreEqual(realStackFrame.Method, backtraceStackFrame.FunctionName);
                    Assert.AreEqual(realStackFrame.LineNumber, backtraceStackFrame.Line);
                    Assert.AreEqual(realStackFrame.Path, backtraceStackFrame.Library);
                }
                // -1 because we include header in stack trace
                Assert.AreEqual(data.Count - startIndex, backtraceStackTrace.StackFrames.Count);
            }
            yield return null;
        }
    }

    public class SampleStackFrame
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public int LineNumber { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Path) && LineNumber == 0)
            {
                return string.Format("{0} \r\n", Method);
            }
            return string.Format("{0} (at {1}:{2}) \r\n", Method, Path, LineNumber);
        }
    }

}
