using Backtrace.Unity.Model;
using NUnit.Framework;
using System.Globalization;
using System.Reflection;

namespace Backtrace.Unity.Tests.Runtime
{
    public class BacktraceRawStackTraceParserTests
    {
        [Test]
        public void SymbolLessModuleLine_ParsesAddressAndLibrary_NoThrow()
        {
            var backtraceStackFrame = BacktraceRawStackTraceParser.ParseNativeFrame("0x00007ffad7723088 (UnityPlayer)");
            Assert.AreEqual("0x00007ffad7723088", backtraceStackFrame.Address);
            Assert.AreEqual("UnityPlayer", backtraceStackFrame.Library);
            Assert.AreEqual(string.Empty, backtraceStackFrame.FunctionName);
            Assert.AreEqual(0, backtraceStackFrame.Line);
            Assert.AreEqual(null, backtraceStackFrame.SourceCode);
            Assert.AreEqual(false, backtraceStackFrame.InvalidFrame);
            Assert.AreEqual(Types.BacktraceStackFrameType.Native, backtraceStackFrame.StackFrameType);
        }

        [Test]
        public void WithSourceFileAndLineInFunctionName_ParsesLibraryAndKeepsFunctionNameAndLine()
        {
            var backtraceStackFrame = BacktraceRawStackTraceParser.ParseNativeFrame("0x00007ffac58086f5 (GameAssembly) DebugLogHandler_Internal_Log_m20852F18A88BB18425BA07260545E3968F7EA76C (at D:/project/app/module/Library/Example/artifacts/build/il2cppOutput/cpp/UnityEngine.CoreModule.cpp:40786)");
            Assert.AreEqual("0x00007ffac58086f5", backtraceStackFrame.Address);
            Assert.AreEqual("GameAssembly", backtraceStackFrame.Library);
            Assert.AreEqual("DebugLogHandler_Internal_Log_m20852F18A88BB18425BA07260545E3968F7EA76C", backtraceStackFrame.FunctionName);
            Assert.AreEqual(40786, backtraceStackFrame.Line);
            Assert.AreEqual("D:/project/app/module/Library/Example/artifacts/build/il2cppOutput/cpp/UnityEngine.CoreModule.cpp", backtraceStackFrame.SourceCode);
            Assert.AreEqual(false, backtraceStackFrame.InvalidFrame);
            Assert.AreEqual(Types.BacktraceStackFrameType.Native, backtraceStackFrame.StackFrameType);
        }

        [Test]
        public void WithSymbol_ParsesMethod()
        {
            var backtraceStackFrame = BacktraceRawStackTraceParser.ParseNativeFrame("0x00007ffbede3e8d7 (KERNEL32) BaseThreadInitThunk");
            Assert.AreEqual("0x00007ffbede3e8d7", backtraceStackFrame.Address);
            Assert.AreEqual("KERNEL32", backtraceStackFrame.Library);
            Assert.AreEqual("BaseThreadInitThunk", backtraceStackFrame.FunctionName);
            Assert.AreEqual(0, backtraceStackFrame.Line);
            Assert.AreEqual(null, backtraceStackFrame.SourceCode);
            Assert.AreEqual(false, backtraceStackFrame.InvalidFrame);
            Assert.AreEqual(Types.BacktraceStackFrameType.Native, backtraceStackFrame.StackFrameType);
        }

        [Test]
        public void UnknownShape_ReturnsRawInFunctionName()
        {
            var backtraceStackFrame = BacktraceNativeRawStacktraceParser.ParseFrameLine("nonsense frame with no address");
            Assert.AreEqual(null, backtraceStackFrame.Address);
            Assert.AreEqual(null, backtraceStackFrame.Library);
            Assert.AreEqual("nonsense frame with no address", backtraceStackFrame.FunctionName);
            Assert.AreEqual(0, backtraceStackFrame.Line);
            Assert.AreEqual(null, backtraceStackFrame.SourceCode);
            Assert.AreEqual(true, backtraceStackFrame.InvalidFrame);
            Assert.AreEqual(Types.BacktraceStackFrameType.Unknown, backtraceStackFrame.StackFrameType);
        }
    }
}
