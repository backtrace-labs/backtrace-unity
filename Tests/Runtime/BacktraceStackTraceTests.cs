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
                    FileName = "TestEnumeratorWrapper.cs",
                    Library = "C:/ buildslave / unity / build / Extensions / TestRunner / UnityEngine.TestRunner / TestRunner / TestEnumeratorWrapper.cs"
                },
                new SampleStackFrame()
                {
                    FunctionName ="UnityEngine.TestTools.EnumerableTestMethodCommand +< ExecuteEnumerable > c__Iterator0.MoveNext()",
                    Line = 112,
                    FileName = "EnumerableTestMethodCommand.cs",
                    Library = "C:/ buildslave / unity / build / Extensions / TestRunner / UnityEngine.TestRunner / NUnitExtensions / Commands / EnumerableTestMethodCommand.cs"
                },
                new SampleStackFrame()
                {
                    FunctionName = "UnityEngine.TestTools.EnumerableSetUpTearDownCommand +< ExecuteEnumerable > c__Iterator0.MoveNext()",
                    Line = 71,
                    FileName = "EnumerableTestMethodCommand.cs",
                    Library = "C:/ buildslave / unity / build / Extensions / TestRunner / UnityEngine.TestRunner / NUnitExtensions / Commands / EnumerableTestMethodCommand.cs"
                },
                new SampleStackFrame()
                {
                    FunctionName = "UnityEngine.TestRunner.NUnitExtensions.Runner.UnityLogCheckDelegatingCommand +< ExecuteEnumerable > c__Iterator0.MoveNext()",
                    Line = 67,
                    FileName = "UnityLogCheckDelegatingCommand.cs",
                    Library = "C:/ buildslave / unity / build / Extensions / TestRunner / UnityEngine.TestRunner / NUnitExtensions / Runner / UnityLogCheckDelegatingCommand.cs"
                },
            };

        private readonly List<SampleStackFrame> _simpleStack = new List<SampleStackFrame>()
        {
            new SampleStackFrame()
                {
                    FunctionName ="Startup.GetRandomFileStream ()",
                    Line = 104,
                    FileName = "Startup.cs",
                    Library = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    FunctionName ="Startup.GetRandomFile ()",
                    Line = 99,
                    FileName = "Startup.cs",
                    Library = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    FunctionName ="Startup.ReadRandomFile ()",
                    Line = 94,
                    FileName = "Startup.cs",
                    Library = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    FunctionName ="Startup.DoSomethingDifferent ()",
                    Line = 89,
                    FileName = "Startup.cs",
                    Library = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    FunctionName ="Startup.DoSomethingElse ()",
                    Line = 84,
                    FileName = "Startup.cs",
                    Library = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    FunctionName ="Startup.DoSomething ()",
                    Line = 80,
                    FileName = "Startup.cs",
                    Library = "Assets/Startup.cs"
                },
            new SampleStackFrame()
                {
                    FunctionName ="Startup.Update ()",
                    Line = 116,
                    FileName = "Startup.cs",
                    Library = "Assets/Startup.cs"
                }
        };


        private readonly List<SampleStackFrame> _anrStackTrace = new List<SampleStackFrame>()
        {
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    FunctionName ="java.lang.Thread.sleep",
                    FileName= "Thread.java",
                    Library = "NativeMethod"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    FunctionName ="java.lang.Thread.sleep",
                    Line = 440,
                    FileName = "Thread.java",
                    Library = "Thread.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    FunctionName ="java.lang.Thread.sleep",
                    Line = 356,
                    FileName = "Thread.java",
                    Library = "Thread.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    FunctionName ="backtrace.io.backtrace_unity_android_plugin.BacktraceCrashHelper$1.run",
                    Line = 16,
                    FileName = "BacktraceCrashHelper.java",
                    Library = "BacktraceCrashHelper.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    FunctionName ="android.os.Handler.handleCallback",
                    Line = 883,
                    FileName = "Handler.java",
                    Library = "Handler.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    FunctionName ="android.os.Handler.dispatchMessage",
                    Line = 100,
                    FileName = "Handler.java",
                    Library = "Handler.java"
                },
            new SampleStackFrame()
                {
                    Type = StackTraceType.Android,
                    FunctionName ="com.android.internal.os.RuntimeInit$MethodAndArgsCaller.run",
                    Line = 493,
                    FileName = "RuntimeInit.java",
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
                FileName= "Thread.java",
                Library = "NativeMethod"
            },
            // android source code stack frame
             new SampleStackFrame()
            {
                Type = StackTraceType.Android,
                FunctionName ="backtrace.io.backtrace_unity_android_plugin.BacktraceCrashHelper.InternalCall",
                Library = "BacktraceCrashHelper.java",
                FileName = "BacktraceCrashHelper.java",
                Line = 31
            },
            new SampleStackFrame() {
                Type = StackTraceType.Android,
                FunctionName = "com.google.android.gms.ads.internal.webview.ac.loadUrl",
                Custom = "com.google.android.gms.ads.internal.webview.ac.loadUrl(:com.google.android.gms.policy_ads_fdr_dynamite@204102000@204102000000.334548305.334548305:1)"
            },
             // android unknown source
             new SampleStackFrame() {
                 Type = StackTraceType.Android,
                 FunctionName = "com.unity3d.player.UnityPlayer.access$300",
                 Line = 0,
                 FileName = "UnityPlayer.java",
                 Library = "Unknown Source"
             },
             // csharp layer
            new SampleStackFrame() {
                 FunctionName = "UnityEngine.AndroidJNISafe.CheckException ()",
                 Line = 24,
                 FileName = "AndroidJNISafe.cs",
                 Library = "/Users/builduser/buildslave/unity/build/Modules/AndroidJNI/AndroidJNISafe.cs"
             },
            // csharp layer with arguments 
             new SampleStackFrame() {
                 FunctionName = "UnityEngine.AndroidJavaObject.CallStatic (System.String methodName, System.Object[] args)",
                 Line = 252,
                 FileName = "AndroidJava.cs",
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
                FileName = "StackWalker",
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
                FileName = "DebugLogHandler",
                FunctionName = "UnityEngine.DebugLogHandler:Internal_Log (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)"
            },
            new SampleStackFrame()
            {
                Type = StackTraceType.Native,
                StackFrame = "0x00000266BD67295B (Mono JIT Code) [firstSceneButtons.cs:41] firstSceneButtons:Start ()",
                Line = 41,
                FileName = "firstSceneButtons.cs",
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
                FileName = "mini-runtime.c",
                Library = "mini-runtime.c",
                Address = "0x00007FFFEEB8CBB0",
                FunctionName = "mono_jit_runtime_invoke"
            },
        };



        [UnityTest]
        public IEnumerator TestReportStackTrace_StackTraceShouldBeTheSameLikeExceptionStackTrace_ShouldReturnCorrectStackTrace()
        {
            try
            {
                //simulate real exception with real stack trace
                System.IO.File.ReadAllText("not existing file");
            }
            catch (Exception exception)
            {
                var report = new BacktraceReport(exception);
                var errorStackFramesCount = new StackTrace(exception, true)
                    .GetFrames()
                    .Count(n => n.GetMethod() != null);

                Assert.AreEqual(report.DiagnosticStack.Count, errorStackFramesCount);
            }
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
        public IEnumerator TestStackTraceCreation_ShouldUseEnvStackTraceWhenExStackTraceIsEmpty_ValidStackTraceObject()
        {
            var backtraceStackTrace = new BacktraceStackTrace(new Exception());
            Assert.IsNotEmpty(backtraceStackTrace.StackFrames);
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
                Assert.AreEqual(anrStackFrame.FileName, backtraceStackFrame.FileName);
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
                if (!string.IsNullOrEmpty(mixModeCallStack.Custom))
                {
                    Assert.AreEqual(mixModeCallStack.FunctionName, backtraceStackFrame.FunctionName);
                }
                else
                {
                    Assert.AreEqual(mixModeCallStack.FunctionName, backtraceStackFrame.FunctionName);
                    Assert.AreEqual(mixModeCallStack.Line, backtraceStackFrame.Line);
                    Assert.AreEqual(mixModeCallStack.FileName, backtraceStackFrame.FileName);
                    Assert.AreEqual(mixModeCallStack.Library, backtraceStackFrame.Library);
                }
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
                    Assert.AreEqual(realStackFrame.FileName, backtraceStackFrame.FileName);
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
            Assert.AreEqual("error", exception.Classifier);
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
                Assert.AreEqual(expectedStackFrame.FileName, backtraceStackFrame.FileName);
            }

        }
        [Test]
        public void ExceptionStackTrace_NoStackTraceAvailable_ExceptionShouldHaveEnvironmentStackTrace()
        {
            var message = "message";
            // in this case BacktraceUnhandledException should generate environment stack trace
            var unhandledExceptionReport = new BacktraceUnhandledException(message, string.Empty);
            Assert.IsNotEmpty(unhandledExceptionReport.StackFrames);
        }


        [Test]
        public void ExceptionStackTrace_StackTraceAvailable_ExceptionShouldHaveExceptionStackTrace()
        {
            var message = "message";
            var stringBuilder = new StringBuilder();
            foreach (var stackFrame in _simpleStack)
            {
                stringBuilder.Append(stackFrame.ToStackFrameString());
            }
            var stackTrace = stringBuilder.ToString();
            var error = new BacktraceUnhandledException(message, stackTrace);
            Assert.AreEqual(_simpleStack.Count, error.StackFrames.Count);
        }

        [Test]
        public void ExceptionStackTrace_ShouldGenerateEnvStackTraceIfExStackTraceIsInvalid_ExceptionShouldHaveExceptionStackTrace()
        {
            var invalidStackTrace = "--";
            var error = new BacktraceUnhandledException("error message", invalidStackTrace);
            Assert.AreEqual(error.StackTrace, invalidStackTrace);
            Assert.IsTrue(error.StackFrames.Any());
        }

        [Test]
        public void ExceptionStackTrace_ShouldGenerateEnvStackTraceIfExStackTraceIsEmpty_ExceptionShouldHaveExceptionStackTrace()
        {
            var error = new BacktraceUnhandledException("error message", string.Empty);
            Assert.AreEqual(error.StackTrace, string.Empty);
            Assert.IsTrue(error.StackFrames.Any());
        }

        [Test]
        public void JITStackTrace_ShouldParseCorrectlyJITStackTrace_StackTraceObjectIsGeneratedCorrectly()
        {

            var simpleFunctionName = "GetStacktrace";
            var functionNameWithWrappedManagedPrefix = "UnityEngine.DebugLogHandler:Internal_Log";
            var functioNamewithMonoJitCodePrefix = "ServerGameManager:SendInitialiseNetObjectToClient";
            var jitStackTrace = string.Format(@"#0 {0} (int)
                 #1 DebugStringToFile(DebugStringToFileData const&)
                 #2 DebugLogHandler_CUSTOM_Internal_Log(LogType, LogOption, ScriptingBackendNativeStringPtrOpaque*, ScriptingBackendNativeObjectPtrOpaque*)
                 #3  (Mono JIT Code) (wrapper managed-to-native) {1} (UnityEngine.LogType,UnityEngine.LogOption,string,UnityEngine.Object)
                 #4  (Mono JIT Code) {2} (FG.Common.GameConnection,FG.Common.NetObjectSpawnData)
                 #5  (Mono JIT Code) ServerGameManager:SpawnAlreadySpawnedObjectsForClient (FG.Common.GameConnection)
                 #6  (Mono JIT Code) ServerGameManager:ProcessLevelLoaded (FG.Common.GameConnection)
                 #7  (Mono JIT Code) ServerGameManager:ProcessClientReady (FG.Common.GameConnection,FG.Common.PlayerReadinessState)
                 #8  (Mono JIT Code) FG.Common.UnityNetworkMessageHandler:HandleAndFreeInboundGameMessage (FG.Common.MessageEnvelope)
                 #9  (Mono JIT Code) FG.Common.UnityNetworkMessageHandler:PeekAndHandleMessage (UnityEngine.Networking.NetworkMessage,FG.Common.GameConnection)
                 #10  (Mono JIT Code) FG.Common.FG_UnityInternetNetworkManager:ServerHandleMessageReceived (UnityEngine.Networking.NetworkMessage)",
                 simpleFunctionName, functionNameWithWrappedManagedPrefix, functioNamewithMonoJitCodePrefix);


            var backtraceUnhandledException = new BacktraceUnhandledException("foo", jitStackTrace);

            Assert.AreEqual(11, backtraceUnhandledException.StackFrames.Count);
            Assert.AreEqual(backtraceUnhandledException.StackFrames[0].FunctionName, simpleFunctionName);
            Assert.AreEqual(backtraceUnhandledException.StackFrames[3].FunctionName, functionNameWithWrappedManagedPrefix);
            Assert.AreEqual(backtraceUnhandledException.StackFrames[4].FunctionName, functioNamewithMonoJitCodePrefix);
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
        public string Custom { get; set; }
        public string StackFrame { get; set; }
        public string FileName { get; set; } = string.Empty;
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
            if (!string.IsNullOrEmpty(Custom))
            {
                return string.Format("{0}\n", Custom);
            }
            var formattedLineNumber = Line != 0 ? string.Format(":{0}", Line) : string.Empty;
            return string.Format("{0}({1}{2})\n", FunctionName, Library, formattedLineNumber);
        }
    }

}
