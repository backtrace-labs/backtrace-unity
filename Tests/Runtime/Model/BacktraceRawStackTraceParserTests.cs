using Backtrace.Unity.Model;
using Backtrace.Unity.Types;
using NUnit.Framework;

namespace Backtrace.Unity.Tests.Runtime
{
    public class BacktraceRawStackTraceParserTests
    {
        [TestCase("0x00007ffad7723088 (UnityPlayer)", "0x00007ffad7723088", "UnityPlayer", "", 0, null, false, BacktraceStackFrameType.Native)]
        [TestCase("0x00007ffac58086f5 (GameAssembly) DebugLogHandler_Internal_Log_m20852F18A88BB18425BA07260545E3968F7EA76C (at D:/project/app/module/Library/Example/artifacts/build/il2cppOutput/cpp/UnityEngine.CoreModule.cpp:40786)",
            "0x00007ffac58086f5", "GameAssembly", "DebugLogHandler_Internal_Log_m20852F18A88BB18425BA07260545E3968F7EA76C", 40786, "D:/project/app/module/Library/Example/artifacts/build/il2cppOutput/cpp/UnityEngine.CoreModule.cpp", false, BacktraceStackFrameType.Native)]
        [TestCase("0x00007ffbede3e8d7 (KERNEL32) BaseThreadInitThunk", "0x00007ffbede3e8d7", "KERNEL32", "BaseThreadInitThunk", 0, null, false, BacktraceStackFrameType.Native)]
        [TestCase("nonsense frame with no address", null, null, "nonsense frame with no address", 0, null, false, BacktraceStackFrameType.Unknown)] //TODO: what about InvalidFrame in this case?
        public void ParseNativeFrame_WithVariousInputs_ReturnsExpectedStackFrame(
            string input,
            string expectedAddress,
            string expectedLibrary,
            string expectedFunctionName,
            int expectedLine,
            string expectedSourceCode,
            bool expectedInvalidFrame,
            Types.BacktraceStackFrameType expectedStackFrameType)
        {
            var backtraceStackFrame = BacktraceRawStackTraceParser.ParseNativeFrame(input);

            Assert.AreEqual(expectedAddress, backtraceStackFrame.Address, "Address mismatch");
            Assert.AreEqual(expectedLibrary, backtraceStackFrame.Library, "Library mismatch");
            Assert.AreEqual(expectedFunctionName, backtraceStackFrame.FunctionName, "Function name mismatch");
            Assert.AreEqual(expectedLine, backtraceStackFrame.Line, "Line number mismatch");
            Assert.AreEqual(expectedSourceCode, backtraceStackFrame.SourceCode, "Source code path mismatch");
            Assert.AreEqual(expectedInvalidFrame, backtraceStackFrame.InvalidFrame, "InvalidFrame flag mismatch");
            Assert.AreEqual(expectedStackFrameType, backtraceStackFrame.StackFrameType, "StackFrameType mismatch");
        }
    }
}
