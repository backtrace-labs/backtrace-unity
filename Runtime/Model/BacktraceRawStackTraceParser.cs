using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using static UnityEngine.Networking.UnityWebRequest;

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

                return ParseStacktraceFrame(frameString);
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception while parsing stack frame: '{frame}'. Exception: {e}");
                return null;
            }
        }

        private BacktraceStackFrame ParseStacktraceFrame(string frameString)
        {
            if (frameString.StartsWith("0x", StringComparison.Ordinal))
            {
                return ParseNativeFrame(frameString);
            }
            return null;
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

