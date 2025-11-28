using Backtrace.Unity.Model;
using Backtrace.Unity.Types;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Text;
using UnityEngine.TestTools;

namespace Backtrace.Unity.Tests.Runtime{
    public class NativeParserTests
    {
        private static BacktraceStackFrame Parse(string frame)
        {
            var instance = new BacktraceUnhandledException("msg", "");
            var mi = typeof(BacktraceUnhandledException)
                .GetMethod("SetNativeStackTraceInformation",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi);
            return (BacktraceStackFrame)mi.Invoke(instance, new object[] { frame });
        }

        [Test]
        public void SymbolLessModuleLine_ParsesAddressAndLibrary_NoThrow()
        {
            var f = Parse("0x00007ffad7723088 (UnityPlayer)");
            Assert.AreEqual("0x00007ffad7723088", f.Address);
            Assert.AreEqual("UnityPlayer", f.Library);
            Assert.AreEqual(string.Empty, f.FunctionName);
            Assert.AreEqual(Types.BacktraceStackFrameType.Native, f.StackFrameType);
        }

        [Test]
        public void WithSymbol_ParsesMethod()
        {
            var f = Parse("0x00007ffad7ee3c7d (UnityPlayer) UnityMain");
            Assert.AreEqual("UnityPlayer", f.Library);
            Assert.AreEqual("UnityMain", f.FunctionName);
        }

        [Test]
        public void UnknownShape_ReturnsRawInFunctionName()
        {
            var f = Parse("nonsense frame with no address");
            Assert.AreEqual("nonsense frame with no address", f.FunctionName);
        }
    }
}
