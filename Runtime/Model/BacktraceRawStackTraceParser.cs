using System;
using System.Collections.Generic;
using System.Linq;

namespace Backtrace.Unity.Model
{
    /// <summary>
    /// Responsible solely for parsing raw Unity / Android / native stack trace strings
    /// into <see cref="BacktraceStackFrame"/> instances.
    /// </summary>
    internal class BacktraceRawStackTraceParser
    {
        private static readonly string[] _javaExtensions = new string[] { ".java", ".kt", "java." };

        internal bool NativeStackTrace { get; private set; }

        internal List<BacktraceStackFrame> ConvertStackFrames(IEnumerable<string> frames)
        {
            var result = new List<BacktraceStackFrame>();

            for (int frameIndex = 0; frameIndex < frames.Count(); frameIndex++)
            {
                var frame = frames.ElementAt(frameIndex);
                if (string.IsNullOrEmpty(frame))
                {
                    continue;
                }

                string frameString = frame.Trim();

                // validate if stack trace has exception header 
                int methodNameEndIndex = frameString.IndexOf(')');
                if (methodNameEndIndex == -1)
                {
                    //invalid stack frame
                    result.Add(new BacktraceStackFrame() { FunctionName = frame });
                    continue;

                }

                // we require a '(' that appears before this ')'
                int openParentIndex = frameString.LastIndexOf('(', methodNameEndIndex);
                if (openParentIndex == -1 || openParentIndex > methodNameEndIndex)
                {
                    // invalid shape: no matching '(' before ')'
                    result.Add(new BacktraceStackFrame { FunctionName = frame });
                    continue;
                }

                result.Add(ConvertFrame(frameString, methodNameEndIndex));
            }
            return result;
        }

        private BacktraceStackFrame ConvertFrame(string frameString, int methodNameEndIndex)
        {
            if (frameString.StartsWith("0x", StringComparison.Ordinal))
            {
                return ParseNativeFrame(frameString);
            }
            else if (frameString.StartsWith("#", StringComparison.Ordinal))
            {
                return SetJITStackTraceInformation(frameString);
            }
#if UNITY_ANDROID || UNITY_EDITOR
            // verify if the stack trace is from Unity by checking if the
            // by checking source code location
            const char argumentStartInitialChar = '(';
            var sourceCodeStartIndex = frameString.IndexOf(argumentStartInitialChar, methodNameEndIndex + 1);
            if (sourceCodeStartIndex > -1)
            {
                return SetDefaultStackTraceInformation(frameString, methodNameEndIndex);
            }
            // verify if frame has parameters that contain source code information
            var methodStartIndex = frameString.IndexOf(argumentStartInitialChar);
            if (methodStartIndex == -1)
            {
                return new BacktraceStackFrame()
                {
                    FunctionName = frameString
                };
            }

            NativeStackTrace = true;
            // add length of the '('
            methodStartIndex += 1;
            var methodArguments = frameString.Substring(methodStartIndex, methodNameEndIndex - methodStartIndex);
            if (methodArguments.IndexOf(':') != -1 || methodArguments == "Unknown Source")
            {
                return SetAndroidStackTraceInformation(frameString, methodStartIndex, methodNameEndIndex);
            }
            // check if popular extensions are available in the frame to determine if 
            // the frame has any reference to java.
            for (int i = 0; i < _javaExtensions.Length; i++)
            {
                if (frameString.IndexOf(_javaExtensions[i], StringComparison.Ordinal) != -1)
                {
                    return SetAndroidStackTraceInformation(frameString, methodStartIndex, methodNameEndIndex);
                }
            }
#endif
            return SetDefaultStackTraceInformation(frameString, methodNameEndIndex);
        }

        /// <summary>
        /// Try to convert JIT stack trace.
        /// </summary>
        /// <param name="frameString">JIT stack frame.</param>
        /// <returns>Backtrace stack frame.</returns>
        private BacktraceStackFrame SetJITStackTraceInformation(string frameString)
        {
            var stackFrame = new BacktraceStackFrame
            {
                StackFrameType = Types.BacktraceStackFrameType.Native
            };
            if (!frameString.StartsWith("#", StringComparison.Ordinal))
            {
                //handle situation when we detected jit stack trace
                // but jit stack trace doesn't start with #
                stackFrame.FunctionName = frameString;
                return stackFrame;
            }

            frameString = frameString.Substring(frameString.IndexOf(' ')).Trim();
            const string monoJitPrefix = "(Mono JIT Code)";
            var monoPrefixIndex = frameString.IndexOf(monoJitPrefix, StringComparison.Ordinal);
            if (monoPrefixIndex != -1)
            {
                frameString = frameString.Substring(monoPrefixIndex + monoJitPrefix.Length).Trim();
            }

            const string managedWraperPrefix = "(wrapper managed-to-native)";
            var managedWraperIndex = frameString.IndexOf(managedWraperPrefix, StringComparison.Ordinal);
            if (managedWraperIndex != -1)
            {
                frameString = frameString.Substring(managedWraperIndex + managedWraperPrefix.Length).Trim();
            }

            // right now we outfiltered all known prefixes 
            // we should have only function name with parameters

            // filter parameters, if we can't use full frameString as function name
            var parametersStart = frameString.IndexOf('(');
            var parametersEnd = frameString.IndexOf(')');
            if (parametersStart != -1 && parametersEnd != -1 && parametersEnd > parametersStart)
            {
                stackFrame.FunctionName = frameString.Substring(0, parametersStart).Trim();
            }
            else
            {
                stackFrame.FunctionName = frameString;
            }

            if (!string.IsNullOrEmpty(stackFrame.FunctionName))
            {
                var libraryNameSeparator = stackFrame.FunctionName.IndexOf(':');
                if (libraryNameSeparator != -1)
                {
                    stackFrame.Library = stackFrame.FunctionName.Substring(0, libraryNameSeparator).Trim();
                    stackFrame.FunctionName = stackFrame.FunctionName.Substring(++libraryNameSeparator).Trim();
                }
                else
                {
                    stackFrame.Library = "native";
                }
            }
            return stackFrame;
        }

        /// <summary>
        /// Try to safely convert a native stack frame string into a Backtrace stack frame.
        /// Handles frames with or without symbols (e.g. "0xADDR (Module)" or "0xADDR (Module) Symbol").
        /// Prevents ArgumentOutOfRangeException by validating index ranges and allowing empty symbols.
        /// </summary>
        /// <param name="frameString">Raw native stack frame line to parse.</param>
        /// <returns>Parsed Backtrace stack frame containing address, library, function name, and optional line number.</returns>
        internal static BacktraceStackFrame ParseNativeFrame(string frameString)
        {
            var stackFrame = new BacktraceStackFrame
            {
                StackFrameType = Types.BacktraceStackFrameType.Native,
            };

            if (string.IsNullOrEmpty(frameString))
            {
                return stackFrame;
            }

            frameString = frameString.Trim();

            // Address: starts with "0x" and ends at first space
            int index = 0;
            if (frameString.StartsWith("0x", StringComparison.Ordinal))
            {
                int space = frameString.IndexOf(' ');
                if (space > 2)
                {
                    stackFrame.Address = frameString.Substring(0, space);
                    index = space + 1;
                }
                else
                {
                    // if Unknown format keep raw text and return
                    stackFrame.FunctionName = frameString;
                    return stackFrame;
                }
            }

            // Library: Module
            if (index < frameString.Length && frameString[index] == '(')
            {
                index++;
                int close = frameString.IndexOf(')', index);
                // if ')' missing leave Library null and continue
                if (close > -1)
                {
                    stackFrame.Library = frameString.Substring(index, close - index);
                    index = close + 1;
                    if (index < frameString.Length && frameString[index] == ' ')
                    {
                        index++;
                    }
                }
            }

            // 3) symbol
            stackFrame.FunctionName = (index < frameString.Length)
                ? frameString.Substring(index).Trim()
                : string.Empty;

            // 4) Normalize known wrappers
            if (stackFrame.FunctionName.StartsWith("(wrapper managed-to-native)", StringComparison.Ordinal))
            {
                stackFrame.FunctionName = stackFrame.FunctionName.Replace("(wrapper managed-to-native)", string.Empty).Trim();
            }

            if (stackFrame.FunctionName.StartsWith("(wrapper runtime-invoke)", StringComparison.Ordinal))
            {
                stackFrame.FunctionName = stackFrame.FunctionName.Replace("(wrapper runtime-invoke)", string.Empty).Trim();
            }

            // [file:line] suffix source code information
            int srcStart = stackFrame.FunctionName.IndexOf('[');
            int srcEnd = stackFrame.FunctionName.IndexOf(']');
            if (srcStart != -1 && srcEnd != -1 && srcEnd > srcStart)
            {
                srcStart++;
                var src = stackFrame.FunctionName.Substring(srcStart, srcEnd - srcStart);
                var parts = src.Split(new char[] { ':' }, 2);
                if (parts.Length == 2 && int.TryParse(parts[1], out var line))
                {
                    stackFrame.Line = line;
                    stackFrame.Library = parts[0];
                    // after ']'
                    int after = srcEnd + 1;
                    if (after < stackFrame.FunctionName.Length && stackFrame.FunctionName[after] == ' ')
                    { after++; }
                    stackFrame.FunctionName = (after < stackFrame.FunctionName.Length)
                        ? frameString.Substring(after)
                        : string.Empty;
                }
            }

            return stackFrame;
        }

        /// <summary>
        /// Try to convert Android stack frame string to Backtrace stack frame.
        /// </summary>
        /// <param name="frameString">Android stack frame.</param>
        /// <param name="parameterStart">Index of parameters start character '('.</param>
        /// <param name="parameterEnd">Index of parameters end character ')'.</param>
        /// <returns>Backtrace stack frame.</returns>
        private BacktraceStackFrame SetAndroidStackTraceInformation(string frameString, int parameterStart, int parameterEnd)
        {
            var stackFrame = new BacktraceStackFrame
            {
                FunctionName = frameString.Substring(0, parameterStart - 1),
                StackFrameType = Types.BacktraceStackFrameType.Android
            };
            var possibleSourceCodeInformation = frameString.Substring(parameterStart, parameterEnd - parameterStart);

            var sourceCodeInformation = possibleSourceCodeInformation.Split(':');
            if (sourceCodeInformation.Length == 2)
            {
                stackFrame.Library = sourceCodeInformation[0];
                int.TryParse(sourceCodeInformation[1], out stackFrame.Line);
            }
            else if (frameString.StartsWith("java.lang", StringComparison.Ordinal) || possibleSourceCodeInformation == "Unknown Source")
            {
                stackFrame.Library = possibleSourceCodeInformation;
            }

            return stackFrame;
        }

        /// <summary>
        /// Try to convert default Unity stack frame to Backtrace stack frame.
        /// </summary>
        /// <param name="frameString">Unity stack frame.</param>
        /// <param name="methodNameEndIndex">Index of method name end character ')'.</param>
        /// <returns>Backtrace stack frame.</returns>
        private BacktraceStackFrame SetDefaultStackTraceInformation(string frameString, int methodNameEndIndex)
        {
            const string wrapperPrefix = "(wrapper remoting-invoke-with-check)";
            if (frameString.StartsWith(wrapperPrefix, StringComparison.Ordinal))
            {
                frameString = frameString.Replace(wrapperPrefix, string.Empty);
            }
            // detect source code information - format : 'at (...)'

            // find source code start based on method parameter start index
            int sourceInformationStartIndex = frameString.IndexOf('(', methodNameEndIndex + 1);
            if (sourceInformationStartIndex == -1)
            {
                return new BacktraceStackFrame()
                {
                    FunctionName = frameString,
                    StackFrameType = Types.BacktraceStackFrameType.Dotnet
                };
            }

            // get source code information substring
            int sourceStringLength = frameString.Length - sourceInformationStartIndex;
            string sourceString = frameString.Trim()
                    .Substring(sourceInformationStartIndex, sourceStringLength);

            int lineNumberSeparator = sourceString.LastIndexOf(':') + 1;
            int endLineNumberSeparator = sourceString.LastIndexOf(')') - lineNumberSeparator;

            var result = new BacktraceStackFrame()
            {
                FunctionName = frameString.Substring(0, methodNameEndIndex + 1).Trim(),
                StackFrameType = Types.BacktraceStackFrameType.Dotnet
            };

            if (endLineNumberSeparator > 0 && lineNumberSeparator > 0)
            {
                string lineNumberString = sourceString.Substring(lineNumberSeparator, endLineNumberSeparator);
                int.TryParse(lineNumberString, out result.Line);
            }

            if (sourceString[0] == '(' && lineNumberSeparator != -1)
            {
                //avoid "at" or '('
                int atSeparator = sourceString.StartsWith("(at", StringComparison.Ordinal)
                    ? 3
                    : 1;
                int endLine = lineNumberSeparator == 0
                    ? sourceString.LastIndexOf(')') - atSeparator
                    : lineNumberSeparator - 1 - atSeparator;
                if (endLine < 0)
                {
                    return result;
                }
                var substring = sourceString.Substring(atSeparator, endLine);

                result.Library = (substring == null ? string.Empty : substring.Trim());

                if (!string.IsNullOrEmpty(result.Library))
                {
                    var testString = string.Copy(result.Library);
                    testString = testString.Replace("0", string.Empty);
                    if (testString.Length <= 2)
                    {
                        result.Library = null;
                    }
                }
                if (string.IsNullOrEmpty(result.Library))
                {
                    result.Library = result.FunctionName.Substring(0, result.FunctionName.LastIndexOf(".", result.FunctionName.IndexOf("(", StringComparison.Ordinal), StringComparison.Ordinal));
                }
            }
            return result;
        }
    }
}

