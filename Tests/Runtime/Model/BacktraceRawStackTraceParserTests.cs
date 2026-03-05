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
            Assert.AreEqual(Types.BacktraceStackFrameType.Native, backtraceStackFrame.StackFrameType);
        }

        [Test]
        public void WithSymbol_ParsesMethod()
        {
            var backtraceStackFrame = BacktraceRawStackTraceParser.ParseNativeFrame("0x00007ffad7ee3c7d (UnityPlayer) UnityMain");
            Assert.AreEqual("UnityPlayer", backtraceStackFrame.Library);
            Assert.AreEqual("UnityMain", backtraceStackFrame.FunctionName);
        }

        [Test]
        public void UnknownShape_ReturnsRawInFunctionName()
        {
            var backtraceStackFrame = BacktraceRawStackTraceParser.ParseNativeFrame("nonsense frame with no address");
            Assert.AreEqual("nonsense frame with no address", backtraceStackFrame.FunctionName);
        }
    }
}
