using System;

namespace Backtrace.Unity.Extensions
{
    public static class EnumExtensions
    {
#if !(NET_STANDARD_2_0 && NET_4_6)
        internal static bool HasFlag(this Enum variable, Enum value)
        {
            // check if from the same type.
            if (variable.GetType() != value.GetType())
            {
                throw new ArgumentException("The checked flag is not from the same type as the checked variable.");
            }
            // Get the type code of the enumeration
            var typeCode = variable.GetTypeCode();

            // If the underlying type of the flag is signed
            if (typeCode == TypeCode.SByte || typeCode == TypeCode.Int16 || typeCode == TypeCode.Int32 || typeCode == TypeCode.Int64)
            {
                return (Convert.ToInt64(variable) & Convert.ToInt64(value)) != 0;
            }

            // If the underlying type of the flag is unsigned
            if (typeCode == TypeCode.Byte || typeCode == TypeCode.UInt16 || typeCode == TypeCode.UInt32 || typeCode == TypeCode.UInt64)
            {
                return (Convert.ToUInt64(variable) & Convert.ToUInt64(value)) != 0;
            }
            return false;
        }
#endif

        /// <summary>
        /// Check if enum flag has all flags set
        /// </summary>
        /// <typeparam name="T">Type with enum flag</typeparam>
        /// <param name="source">enum</param>
        /// <returns>True if all options are set.</returns>
        public static bool HasAllFlags<T>(this T rawSource)
        {
            var source = rawSource as Enum;
            Type enumType = typeof(T);

            var enumValues = Enum.GetValues(enumType);
            foreach (var value in enumValues)
            {
                if (!source.HasFlag((T)value as Enum))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
