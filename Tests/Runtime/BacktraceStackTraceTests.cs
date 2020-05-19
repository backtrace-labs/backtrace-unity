using Backtrace.Unity.Model;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime
{
     public class BacktraceStackTraceTests
    {
        private static string _advancedStackTraceClassifier = "InvalidSignatureException";
        private readonly List<SampleStackFrame> _advancedStack = new List<SampleStackFrame>()
            {
                new SampleStackFrame(){
                    Method = string.Format("{0}: Return type System.Void of GenerateSubmissionUrl_FromValidHostName_ValidSubmissionUrl in Tests.BacktraceCredentialsTests is not supported.", _advancedStackTraceClassifier)
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


        private readonly List<SampleStackFrame> _anrStackTrace = new List<SampleStackFrame>()
        {
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    Method ="java.lang.Thread.sleep",
                    Path = "NativeMethod"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    Method ="java.lang.Thread.sleep",
                    LineNumber = 440,
                    Path = "Thread.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    Method ="java.lang.Thread.sleep",
                    LineNumber = 356,
                    Path = "Thread.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    Method ="backtrace.io.backtrace_unity_android_plugin.BacktraceCrashHelper$1.run",
                    LineNumber = 16,
                    Path = "BacktraceCrashHelper.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    Method ="android.os.Handler.handleCallback",
                    LineNumber = 883,
                    Path = "Handler.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    Method ="android.os.Handler.dispatchMessage",
                    LineNumber = 100,
                    Path = "Handler.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    Method ="com.android.internal.os.RuntimeInit$MethodAndArgsCaller.run",
                    LineNumber = 493,
                    Path = "RuntimeInit.java"
                }
        };

        private static string _mixModeClassifier = "java.lang.ArrayIndexOutOfBoundsException";
        private readonly List<SampleStackFrame> _mixModeCallStack = new List<SampleStackFrame>()
        {
            //method header
            new SampleStackFrame()
            {
                Method = string.Format("AndroidJavaException: {0}: length=4; index=5", _mixModeClassifier)
            },
            //android native stack frame
            new SampleStackFrame()
            {
                Type = StackTraceType.Android,
                Method ="java.lang.Thread.sleep",
                Path = "NativeMethod"
            },
            // android source code stack frame
             new SampleStackFrame()
            {
                Type = StackTraceType.Android,
                Method ="backtrace.io.backtrace_unity_android_plugin.BacktraceCrashHelper.InternalCall",
                Path = "BacktraceCrashHelper.java",
                LineNumber = 31
            },
             // android unknown source
             new SampleStackFrame() {
                 Type = StackTraceType.Android,
                 Method = "com.unity3d.player.UnityPlayer.access$300",
                 LineNumber = 0,
                 Path = "Unknown Source"
             },
             // csharp layer
            new SampleStackFrame() {
                 Method = "UnityEngine.AndroidJNISafe.CheckException ()",
                 LineNumber = 24,
                 Path = "/Users/builduser/buildslave/unity/build/Modules/AndroidJNI/AndroidJNISafe.cs"
             },
            // csharp layer with arguments 
             new SampleStackFrame() {
                 Method = "UnityEngine.AndroidJavaObject.CallStatic (System.String methodName, System.Object[] args)",
                 LineNumber = 252,
                 Path = "/Users/builduser/buildslave/unity/build/Modules/AndroidJNI/AndroidJava.cs"
             }
        };



        [UnityTest]
        public IEnumerator TestReportStackTrace_StackTraceShouldBeTheSameLikeExceptionStackTrace_ShouldReturnCorrectStackTrace()
        {
            var exception = new Exception("exception");
            var report = new BacktraceReport(exception);
            Assert.AreEqual(report.DiagnosticStack.Count, exception.StackTrace == null ? 0 : exception.StackTrace.Count());
            yield return null;
        }


        [UnityTest]
        public IEnumerator TestReportStackTrace_StackTraceShouldIncludeEnvironmentStackTrace_ShouldReturnCorrectStackTrace()
        {
            var environmentStackTrace = new StackTrace(true);
            var report = new BacktraceReport("msg");
            Assert.AreEqual(environmentStackTrace.FrameCount, report.DiagnosticStack.Count);
            yield return null;
        }


        [UnityTest]
        public IEnumerator TestStackTraceCreation_EmptyStackTrace_ValidStackTraceObject()
        {
            var backtraceStackTrace = new BacktraceStackTrace(new Exception());
            Assert.IsTrue(backtraceStackTrace.StackFrames.Count == 0);
            yield return null;
        }

        [Test]
        public void TestStackTraceCreation_AndroidException_ValidStackTraceObject()
        {
            var stackTrace = ConvertStackTraceToString(_anrStackTrace);
            var exception = new BacktraceUnhandledException(string.Empty, stackTrace);
            var backtraceStackTrace = new BacktraceStackTrace(exception);
            for (int i = 0; i < _anrStackTrace.Count; i++)
            {
                var anrStackFrame = _anrStackTrace.ElementAt(i);
                var backtraceStackFrame = backtraceStackTrace.StackFrames[i];
                Assert.AreEqual(anrStackFrame.Method, backtraceStackFrame.FunctionName);
                Assert.AreEqual(anrStackFrame.LineNumber, backtraceStackFrame.Line);
                Assert.AreEqual(anrStackFrame.Path, backtraceStackFrame.Library);
            }
        }


        [Test]
        public void TestStackTraceCreation_AndroidMixModeCallStack_ValidStackTraceObject()
        {
            var stackTrace = ConvertStackTraceToString(_mixModeCallStack);
            var exception = new BacktraceUnhandledException(string.Empty, stackTrace);
            var backtraceStackTrace = new BacktraceStackTrace(exception);
            //skip first frame
            int startIndex = exception.Header ? 1 : 0;
            for (int i = startIndex; i < _mixModeCallStack.Count; i++)
            {
                var mixModeCallStack = _mixModeCallStack.ElementAt(i);
                var backtraceStackFrame = backtraceStackTrace.StackFrames[i - startIndex];
                Assert.AreEqual(mixModeCallStack.Method, backtraceStackFrame.FunctionName);
                Assert.AreEqual(mixModeCallStack.LineNumber, backtraceStackFrame.Line);
                Assert.AreEqual(mixModeCallStack.Path, backtraceStackFrame.Library);
            }
        }

        [Test]
        public void TestStackTraceCreation_UnityEngineException_ValidStackTraceObject()
        {
            var source = new List<List<SampleStackFrame>>() { _simpleStack, _advancedStack };
            foreach (var data in source)
            {
                var stackTrace = ConvertStackTraceToString(data);

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
        }

        [Test]
        public void TestClassifierSetter_SetMixModeCallStackWithJavaClassifier_ShoudlSetJavaClassifier()
        {
            var stackTrace = ConvertStackTraceToString(_mixModeCallStack);
            // should guess classifier based on first stack frame
            string message = string.Empty;
            var exception = new BacktraceUnhandledException(message, stackTrace);
            Assert.AreEqual(_mixModeClassifier, exception.Classifier);
        }

        [Test]
        public void TestClassifierSetter_SetAdvancedIl2CPPClassifier_ShoudlSetClassifier()
        {
            var stackTrace = ConvertStackTraceToString(_advancedStack);
            // should guess classifier based on first stack frame
            string message = string.Empty;
            var exception = new BacktraceUnhandledException(message, stackTrace);
            Assert.AreEqual(_advancedStackTraceClassifier, exception.Classifier);
        }

        [Test]
        public void TestClassifierSetter_DontSetClassifierIfClassifierIsNotAvailable_ShouldUseDefaultClassifier()
        {
            var stackTrace = ConvertStackTraceToString(_simpleStack);
            // should guess classifier based on first stack frame
            string message = string.Empty;
            var exception = new BacktraceUnhandledException(message, stackTrace);
            Assert.AreEqual("BacktraceUnhandledException", exception.Classifier);
        }


        [Test]
        public void TestClassifierSetter_SetAClassifierBasedOnErrorMessageNotStackFrames_ShoudlSetClassifier()
        {
            var stackTrace = ConvertStackTraceToString(_simpleStack);
            // should guess classifier based on first stack frame
            string message = _mixModeClassifier;
            var exception = new BacktraceUnhandledException(message, stackTrace);
            Assert.AreEqual(_mixModeClassifier, exception.Classifier);
        }

        [Test]
        public void TestClassifierSetter_SetCorrectReportClassifier_ShoudlSetReportClassifier()
        {
            var stackTrace = ConvertStackTraceToString(_simpleStack);
            // should guess classifier based on first stack frame
            string message = _mixModeClassifier;
            var exception = new BacktraceUnhandledException(message, stackTrace);
            var report = new BacktraceReport(exception);
            Assert.AreEqual(_mixModeClassifier, report.Classifier);
        }

        internal string ConvertStackTraceToString(List<SampleStackFrame> data)
        {
            var stringBuilder = new StringBuilder();
            foreach (var stackFrame in data)
            {
                stringBuilder.Append(stackFrame.ToStackFrameString());
            }
            return stringBuilder.ToString();
        }
    }

    internal enum StackTraceType { Default, Android };
    internal class SampleStackFrame
    {
        public StackTraceType Type = StackTraceType.Default;
        public string Method { get; set; }
        public string Path { get; set; }
        public int LineNumber { get; set; }

        public string ToStackFrameString()
        {
            return Type == StackTraceType.Default
                ? ParseDefaultStackTrace()
                : ParseAndroidStackTrace();
        }

        private string ParseDefaultStackTrace()
        {
            if (string.IsNullOrEmpty(Path) && LineNumber == 0)
            {
                return string.Format("{0} \r\n", Method);
            }
            return string.Format("{0} (at {1}:{2}) \r\n", Method, Path, LineNumber);
        }

        public string ParseAndroidStackTrace()
        {
            var formattedLineNumber = LineNumber != 0 ? string.Format(":{0}", LineNumber) : string.Empty;
            return string.Format("{0}({1}{2})\n", Method, Path, formattedLineNumber);
        }
    }

}
