using System;

namespace Backtrace.Unity.Common
{
    internal static class MathHelper
    {
        public static double Clamp(double value, double minimum, double maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        public static double Uniform(double minimum, double maximum)
        {
            return new System.Random().NextDouble() * (maximum - minimum) + minimum;
        }
    }
}
