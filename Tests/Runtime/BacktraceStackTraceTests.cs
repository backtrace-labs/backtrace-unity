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
                    FunctionName = string.Format("{0}: Return type System.Void of GenerateSubmissionUrl_FromValidHostName_ValidSubmissionUrl in Tests.BacktraceCredentialsTests is not supported.", _advancedStackTraceClassifier)
                },
                new SampleStackFrame()
                {
                    FunctionName ="UnityEngine.TestTools.TestRunner.TestEnumeratorWrapper.GetEnumerator(NUnit.Framework.Internal.ITestExecutionContext context)",
                    Line = 31,
                    Library = "C:/ buildslave / unity / build / Extensions / TestRunner / UnityEngine.TestRunner / TestRunner / TestEnumeratorWrapper.cs"
                },
                new SampleStackFrame()
                {
                    FunctionName ="UnityEngine.TestTools.EnumerableTestMethodCommand +< ExecuteEnumerable > c__Iterator0.MoveNext()",
                    Line = 112,
                    Library = "C:/ buildslave / unity / build / Extensions / TestRunner / UnityEngine.TestRunner / NUnitExtensions / Commands / EnumerableTestMethodCommand.cs"
                },
                new SampleStackFrame()
                {
                    FunctionName = "UnityEngine.TestTools.EnumerableSetUpTearDownCommand +< ExecuteEnumerable > c__Iterator0.MoveNext()",
                    Line = 71,
                    Library = "C:/ buildslave / unity / build / Extensions / TestRunner / UnityEngine.TestRunner / NUnitExtensions / Commands / EnumerableTestMethodCommand.cs"
                },
                new SampleStackFrame()
                {
                    FunctionName = "UnityEngine.TestRunner.NUnitExtensions.Runner.UnityLogCheckDelegatingCommand +< ExecuteEnumerable > c__Iterator0.MoveNext()",
                    Line = 67,
                    Library = "C:/ buildslave / unity / build / Extensions / TestRunner / UnityEngine.TestRunner / NUnitExtensions / Runner / UnityLogCheckDelegatingCommand.cs"
                },
            };

        private readonly List<SampleStackFrame> _simpleStack = new List<SampleStackFrame>()
        {
            new SampleStackFrame()
                {
                    FunctionName ="Startup.GetRandomFileStream ()",
                    Line = 104,
                    Library = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    FunctionName ="Startup.GetRandomFile ()",
                    Line = 99,
                    Library = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    FunctionName ="Startup.ReadRandomFile ()",
                    Line = 94,
                    Library = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    FunctionName ="Startup.DoSomethingDifferent ()",
                    Line = 89,
                    Library = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    FunctionName ="Startup.DoSomethingElse ()",
                    Line = 84,
                    Library = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    FunctionName ="Startup.DoSomething ()",
                    Line = 80,
                    Library = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    FunctionName ="Startup.Update ()",
                    Line = 116,
                    Library = "Assets/Startup.cs"
                }
        };


        private readonly List<SampleStackFrame> _anrStackTrace = new List<SampleStackFrame>()
        {
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    FunctionName ="java.lang.Thread.sleep",
                    Library = "NativeMethod"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    FunctionName ="java.lang.Thread.sleep",
                    Line = 440,
                    Library = "Thread.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    FunctionName ="java.lang.Thread.sleep",
                    Line = 356,
                    Library = "Thread.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    FunctionName ="backtrace.io.backtrace_unity_android_plugin.BacktraceCrashHelper$1.run",
                    Line = 16,
                    Library = "BacktraceCrashHelper.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    FunctionName ="android.os.Handler.handleCallback",
                    Line = 883,
                    Library = "Handler.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    FunctionName ="android.os.Handler.dispatchMessage",
                    Line = 100,
                    Library = "Handler.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    FunctionName ="com.android.internal.os.RuntimeInit$MethodAndArgsCaller.run",
                    Line = 493,
                    Library = "RuntimeInit.java"
                }
        };

        private static string _mixModeClassifier = "java.lang.ArrayIndexOutOfBoundsException";
        private readonly List<SampleStackFrame> _mixModeCallStack = new List<SampleStackFrame>()
        {
            //method header
            new SampleStackFrame()
            {
                FunctionName = string.Format("AndroidJavaException: {0}: length=4; index=5", _mixModeClassifier)
            },
            //android native stack frame
            new SampleStackFrame()
            {
                Type = StackTraceType.Android,
                FunctionName ="java.lang.Thread.sleep",
                Library = "NativeMethod"
            },
            // android source code stack frame
             new SampleStackFrame()
            {
                Type = StackTraceType.Android,
                FunctionName ="backtrace.io.backtrace_unity_android_plugin.BacktraceCrashHelper.InternalCall",
                Library = "BacktraceCrashHelper.java",
                Line = 31
            },
             // android unknown source
             new SampleStackFrame() {
                 Type = StackTraceType.Android,
                 FunctionName = "com.unity3d.player.UnityPlayer.access$300",
                 Line = 0,
                 Library = "Unknown Source"
             },
             // csharp layer
            new SampleStackFrame() {
                 FunctionName = "UnityEngine.AndroidJNISafe.CheckException ()",
                 Line = 24,
                 Library = "/Users/builduser/buildslave/unity/build/Modules/AndroidJNI/AndroidJNISafe.cs"
             },
            // csharp layer with arguments 
             new SampleStackFrame() {
                 FunctionName = "UnityEngine.AndroidJavaObject.CallStatic (System.String methodName, System.Object[] args)",
                 Line = 252,
                 Library = "/Users/builduser/buildslave/unity/build/Modules/AndroidJNI/AndroidJava.cs"
             }
        };

        private readonly List<SampleStackFrame> _nativeCallStack = new List<SampleStackFrame>()
        {
            new SampleStackFrame()
            {
                Type = StackTraceType.Native,
                StackFrame = "0x00007FF6661B40EC (Unity) StackWalker::GetCurrentCallstack",
                Line = 0,
                Library = "Unity",
                Address = "0x00007FF6661B40EC",
                FunctionName = "StackWalker::GetCurrentCallstack"
            },
            new SampleStackFrame()
            {
                Type = StackTraceType.Native,
                StackFrame = "0x00007FF666E1368E (Unity) DebugStringToFile",
                Line = 0,
                Library = "Unity",
                Address = "0x00007FF666E1368E",
                FunctionName = "DebugStringToFile"
            },
            new SampleStackFrame()
            {
                Type = StackTraceType.Native,
                StackFrame = "0x00007FF66621BD75 (Unity) DebugLogHandler_CUSTOM_Internal_Log",
                Line = 0,
                Library = "Unity",
                Address = "0x00007FF66621BD75",
                FunctionName = "DebugLogHandler_CUSTOM_Internal_Log"
            },
            new SampleStackFrame()
            {
                Type = StackTraceType.Native,
                StackFrame = "0x00000266BD679AEB (Mono JIT Code) (wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)",
                Line = 0,
                Library = "Mono JIT Code",
                Address = "0x00000266BD679AEB",
                FunctionName = "(wrapper managed-to-native) UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)"
            },
            new SampleStackFrame()
            {
                Type = StackTraceType.Native,
                StackFrame = "0x00000266BD67295B (Mono JIT Code) [firstSceneButtons.cs:41] firstSceneButtons:Start ()",
                Line = 41,
                Library = "firstSceneButtons.cs",
                Address = "0x00000266BD67295B",
                FunctionName = "firstSceneButtons:Start ()"
            },
            new SampleStackFrame()
            {
                Type = StackTraceType.Native,
                StackFrame = "0x00007FF85D817BD4 (KERNEL32) BaseThreadInitThunk",
                Line = 0,
                Library = "KERNEL32",
                Address = "0x00007FF85D817BD4",
                FunctionName = "BaseThreadInitThunk"
            },
            new SampleStackFrame()
            {
                Type = StackTraceType.Native,
                StackFrame = "0x00007FFFEEB8CBB0 (mono-2.0-bdwgc) [mini-runtime.c:2809] mono_jit_runtime_invoke",
                Line = 2809,
                Library = "mini-runtime.c",
                Address = "0x00007FFFEEB8CBB0",
                FunctionName = "mono_jit_runtime_invoke"
            },
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
                Assert.AreEqual(anrStackFrame.FunctionName, backtraceStackFrame.FunctionName);
                Assert.AreEqual(anrStackFrame.Line, backtraceStackFrame.Line);
                Assert.AreEqual(anrStackFrame.Library, backtraceStackFrame.Library);
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
                Assert.AreEqual(mixModeCallStack.FunctionName, backtraceStackFrame.FunctionName);
                Assert.AreEqual(mixModeCallStack.Line, backtraceStackFrame.Line);
                Assert.AreEqual(mixModeCallStack.Library, backtraceStackFrame.Library);
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
                    Assert.AreEqual(realStackFrame.FunctionName, backtraceStackFrame.FunctionName);
                    Assert.AreEqual(realStackFrame.Line, backtraceStackFrame.Line);
                    Assert.AreEqual(realStackFrame.Library, backtraceStackFrame.Library);
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

        [Test]
        public void TestNativeStackTraceParser_ParseNativeStackTrace_ShouldSetCorrectStackFrames()
        {
            var stackTrace = ConvertStackTraceToString(_nativeCallStack);
            var exception = new BacktraceUnhandledException(string.Empty, stackTrace);

            for (int stackFrameIndex = 0; stackFrameIndex < _nativeCallStack.Count; stackFrameIndex++)
            {
                var expectedStackFrame = _nativeCallStack.ElementAt(stackFrameIndex);
                var backtraceStackFrame = exception.StackFrames[stackFrameIndex];

                Assert.AreEqual(expectedStackFrame.Address, backtraceStackFrame.Address);
                Assert.AreEqual(expectedStackFrame.Library, backtraceStackFrame.Library);
                Assert.AreEqual(expectedStackFrame.Line, backtraceStackFrame.Line);
                Assert.AreEqual(expectedStackFrame.FunctionName, backtraceStackFrame.FunctionName);
            }

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

    internal enum StackTraceType { Default, Android, Native };
    internal class SampleStackFrame
    {
        public StackTraceType Type = StackTraceType.Default;
        public string StackFrame { get; set; }
        public string FunctionName { get; set; }
        public string Library { get; set; }
        public int Line { get; set; }
        public string Address { get; set; }

        public string ToStackFrameString()
        {
            switch (Type)
            {
                case StackTraceType.Android:
                    return ParseAndroidStackTrace();
                case StackTraceType.Native:
                    return string.Format("{0}\n", StackFrame);
                case StackTraceType.Default:
                default:
                    return ParseDefaultStackTrace();
            }
        }

        private string ParseDefaultStackTrace()
        {
            if (string.IsNullOrEmpty(Library) && Line == 0)
            {
                return string.Format("{0} \r\n", FunctionName);
            }
            return string.Format("{0} (at {1}:{2}) \r\n", FunctionName, Library, Line);
        }

        public string ParseAndroidStackTrace()
        {
            var formattedLineNumber = Line != 0 ? string.Format(":{0}", Line) : string.Empty;
            return string.Format("{0}({1}{2})\n", FunctionName, Library, formattedLineNumber);
        }
    }

}
