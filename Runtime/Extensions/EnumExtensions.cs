using System;

namespace Backtrace.Unity.Extensions
{
    internal static class EnumExtensions
    {
#if !(NET_STANDARD_2_0 && NET_4_6)
        internal static bool HasFlag(this Enum variable, Enum value)
        {
            // check if from the same type.
            if (variable.GetType() != value.GetType())
            {
                throw new ArgumentException("The checked flag is not from the same type as the checked variable.");
            }

            Convert.ToUInt64(value);
            ulong num = Convert.ToUInt64(value);
            ulong num2 = Convert.ToUInt64(variable);

            return (num2 & num) == num;
        }
#endif
    }
}
