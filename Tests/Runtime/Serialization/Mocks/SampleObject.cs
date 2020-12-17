using System;
using System.Collections.Generic;

namespace Backtrace.Unity.Tests.Runtime
{
    [Serializable]
    internal class BaseJObject
    {
        public SampleObject InnerObject;
    }
    [Serializable]
    internal class SampleObject
    {
        public string AgentName;
        public string TestString;
        public bool Active;
        public int IntNumber;
        public float FloatNumber;
        public long LongNumber;
        public double DoubleNumber;


        public List<string> StringList;
        public string[] StringArray;
        public int[] NumberArray;
    }
}
