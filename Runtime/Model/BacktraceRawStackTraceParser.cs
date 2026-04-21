using System;
using System.Collections.Generic;
using UnityEngine;

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

            foreach (var frame in frames)
            {
                if (string.IsNullOrEmpty(frame))
                {
                    continue;
                }

                BacktraceStackFrame convertedFrame = TryParseFrameOrFallback(frame);
                if (convertedFrame != null)
                {
                    result.Add(convertedFrame);
                }
            }
            return result;
        }

        private BacktraceStackFrame TryParseFrameOrFallback(string frame)
        {
            try
            {
                string frameString = frame.Trim();

                // validate if stack trace has exception header               
                int methodNameEndIndex = frameString.IndexOf(')');
                int openParentIndex = frameString.LastIndexOf('(', methodNameEndIndex); // we require a '(' that appears before this ')'

                if (methodNameEndIndex == -1 || openParentIndex == -1 || openParentIndex > methodNameEndIndex)
                {
                    // If either index is missing, it's an invalid frame
                    Debug.LogWarning($"Invalid stack frame format: '{frameString}'.");
                    return new BacktraceStackFrame { FunctionName = frame };
                }

                return ParseStacktraceFrame(frameString, methodNameEndIndex);
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception while parsing stack frame: '{frame}'. Exception: {e}");
                return null;
            }
        }

        private BacktraceStackFrame ParseStacktraceFrame(string frameString, int methodNameEndIndex)
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

            if (string.IsNullOrWhiteSpace(frameString))
            {
                return stackFrame;
            }

            frameString = frameString.Trim();

            int index = 0;


            if (!TryParseAddress(frameString, ref index, stackFrame))
            {
                return Fallback(stackFrame, frameString);
            }

            TryParseLibrary(frameString, ref index, stackFrame);
            ParseFunction(frameString, index, stackFrame);
            NormalizeWrapper(ref stackFrame.FunctionName);
            ParseBracketSource(ref stackFrame);
            return stackFrame;
        }

        private static bool TryParseAddress(string s, ref int index, BacktraceStackFrame frame)
        {
            if (!s.StartsWith("0x", StringComparison.Ordinal))
                return false;

            int space = s.IndexOf(' ');
            if (space <= 2)
                return false;

            frame.Address = s.Substring(0, space);
            index = space + 1;
            return true;
        }

        private static void TryParseLibrary(string s, ref int index, BacktraceStackFrame frame)
        {
            SkipSpaces(s, ref index);

            if (index >= s.Length || s[index] != '(')
                return;

            int start = index + 1;
            int end = s.IndexOf(')', start);

            if (end > start)
            {
                frame.Library = s.Substring(start, end - start);
                index = end + 1;
            }
        }

        private static void ParseFunction(string s, int index, BacktraceStackFrame frame)
        {
            int atIndex = s.IndexOf(" (at ", index, StringComparison.Ordinal);

            if (atIndex == -1)
            {
                frame.FunctionName = (index < s.Length)
                    ? s.Substring(index).Trim()
                    : string.Empty;
            }
            else
            {
                frame.FunctionName = s.Substring(index, atIndex - index).Trim();

                ParseAtSource(s, atIndex, frame);
            }
        }

        private static void ParseAtSource(string s, int atIndex, BacktraceStackFrame frame)
        {
            int pathStart = atIndex + 5; // skip " (at "
            int endParen = s.LastIndexOf(')');

            if (endParen <= pathStart)
                return;

            string pathAndLine = s.Substring(pathStart, endParen - pathStart);

            int colon = pathAndLine.LastIndexOf(':');
            if (colon <= 0 || colon >= pathAndLine.Length - 1)
                return;

            string path = pathAndLine.Substring(0, colon);
            string lineStr = pathAndLine.Substring(colon + 1);

            if (int.TryParse(lineStr, out int line))
            {
                frame.Line = line;
                frame.SourceCode = path;
            }
        }

        private static void ParseBracketSource(ref BacktraceStackFrame frame)
        {
            var fn = frame.FunctionName;
            if (string.IsNullOrEmpty(fn))
                return;

            int start = fn.IndexOf('[');
            int end = fn.IndexOf(']');

            if (start == -1 || end == -1 || end <= start)
                return;

            string content = fn.Substring(start + 1, end - start - 1);

            int colon = content.LastIndexOf(':');
            if (colon <= 0 || colon >= content.Length - 1)
                return;

            string file = content.Substring(0, colon);
            string lineStr = content.Substring(colon + 1);

            if (int.TryParse(lineStr, out int line))
            {
                frame.Line = line;

                if (string.IsNullOrEmpty(frame.SourceCodeFullPath))
                {
                    frame.SourceCodeFullPath = file;
                }

                // remove [file:line] from function name
                frame.FunctionName = RemoveSegment(fn, start, end + 1);
            }
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

        private static void NormalizeWrapper(ref string functionName)
        {
            if (string.IsNullOrEmpty(functionName))
                return;

            functionName = RemovePrefix(functionName, "(wrapper managed-to-native)");
            functionName = RemovePrefix(functionName, "(wrapper runtime-invoke)");
        }

        private static string RemovePrefix(string value, string prefix)
        {
            if (value.StartsWith(prefix, StringComparison.Ordinal))
            {
                return value.Substring(prefix.Length).Trim();
            }
            return value;
        }

        private static void SkipSpaces(string s, ref int index)
        {
            while (index < s.Length && s[index] == ' ')
                index++;
        }

        private static string RemoveSegment(string s, int start, int end)
        {
            if (start >= end || start < 0 || end > s.Length)
                return s;

            return (s.Substring(0, start) + s.Substring(end)).Trim();
        }

        private static BacktraceStackFrame Fallback(BacktraceStackFrame frame, string raw)
        {
            frame.FunctionName = raw;
            return frame;
        }
    }
}

