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

        [Test]
        public void WithSourceFileAndLineInFunctionName_ParsesLibraryAndKeepsFunctionNameAndLine()
        {
            var backtraceStackFrame = BacktraceRawStackTraceParser.ParseNativeFrame("0x00007ffac65f0d48 (GameAssembly) UIElementsRuntimeUtilityNative_UpdatePanels_m7AA4182BFC7A561A78A786FAAD18C71158EDFCBD (at D:/fm26/apps/game/FM Unity/Library/Bee/artifacts/WinPlayerBuildProgram/il2cppOutput/cpp/UnityEngine.UIElementsModule__7.cpp:43671)");
            Assert.AreEqual("GameAssembly", backtraceStackFrame.Library);
            Assert.AreEqual("UIElementsRuntimeUtilityNative_UpdatePanels_m7AA4182BFC7A561A78A786FAAD18C71158EDFCBD (at D:/fm26/apps/game/FM Unity/Library/Bee/artifacts/WinPlayerBuildProgram/il2cppOutput/cpp/UnityEngine.UIElementsModule__7.cpp:43671)", backtraceStackFrame.FunctionName);
            Assert.AreEqual(0, backtraceStackFrame.Line); // TODO: verify if it should be 0 as line if value is above
        }
    }
}
