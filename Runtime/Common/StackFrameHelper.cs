using System.Reflection;
using System.Text;

namespace Backtrace.Unity.Common
{
    /// <summary>
    /// Stack frame helper methods
    /// </summary>
    public static class StackFrameHelper
    {
        public static StringBuilder AddSyncMethodName(this StringBuilder builder, string methodName)
        {
            builder.Append(".");
            builder.Append(methodName);
            return builder;
        }

        public static StringBuilder AddFrameParameters(this StringBuilder builder, ParameterInfo[] parameters)
        {
            builder.Append("(");
            var firstParam = true;
            foreach (var param in parameters)
            {
                if (!firstParam)
                {
                    builder.Append(", ");
                }
                else
                {
                    firstParam = false;
                }
                // ReSharper disable once ConstantConditionalAccessQualifier
                // ReSharper disable once ConstantNullCoalescingCondition
                var typeName = param.ParameterType != null ? param.ParameterType.Name : "<UnknownType>";
                builder.Append(typeName);
                builder.Append(" ");
                builder.Append(param.Name);
            }
            builder.Append(")");
            return builder;
        }

        internal static string GetAsyncFrameFullName(string frameFullName)
        {
            var start = frameFullName.LastIndexOf('<');
            var end = frameFullName.LastIndexOf('>');
            if (start >= 0 && end >= 0)
            {
                return frameFullName.Remove(start, 1).Substring(0, end - 1);
            }
            return frameFullName;
        }
    }
}
