using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Profiling;

namespace Backtrace.Unity.Model.Attributes
{
    internal sealed class ProcessAttributeProvider : IDynamicAttributeProvider
    {
        public void GetAttributes(IDictionary<string, string> attributes)
        {
            if (attributes == null)
            {
                return;
            }
            attributes["gc.heap.used"] = GC.GetTotalMemory(false).ToString(CultureInfo.InvariantCulture);
            attributes["process.age"] = Math.Round(Time.realtimeSinceStartup).ToString(CultureInfo.InvariantCulture);
            attributes["system.memory.active"] = Profiler.GetTotalAllocatedMemoryLong().ToString(CultureInfo.InvariantCulture);
            attributes["system.memory.reserved"] = Profiler.GetTotalReservedMemoryLong().ToString(CultureInfo.InvariantCulture);
            attributes["system.memory.unused"] = Profiler.GetTotalUnusedReservedMemoryLong().ToString(CultureInfo.InvariantCulture);
            attributes["system.memory.temp"] = Profiler.GetTempAllocatorSize().ToString(CultureInfo.InvariantCulture);
            attributes["mono.heap"] = Profiler.GetMonoHeapSizeLong().ToString(CultureInfo.InvariantCulture);
            attributes["mono.used"] = Profiler.GetMonoUsedSizeLong().ToString(CultureInfo.InvariantCulture);
            attributes["application.playing"] = Application.isPlaying.ToString(CultureInfo.InvariantCulture);
            attributes["application.focused"] = Application.isFocused.ToString(CultureInfo.InvariantCulture);
            attributes["application.internet_reachability"] = Application.internetReachability.ToString();

        }
    }
}
