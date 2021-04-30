using Backtrace.Unity.Json;
using Backtrace.Unity.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Backtrace.Unity.Model
{
    public class BacktraceStackFrame
    {

        private static string[] _frameSeparators = new string[] { "::", ":", "." };

        /// <summary>
        /// Function where exception occurs
        /// </summary>
        public string FunctionName;

        internal BacktraceStackFrameType StackFrameType = BacktraceStackFrameType.Unknown;

        /// <summary>
        /// Function file name
        /// </summary>
        public string FileName
        {
            get
            {
                return string.IsNullOrEmpty(Library)
                        ? GetFileNameFromFunctionName()
                        : Library.IndexOfAny(Path.GetInvalidPathChars()) == -1 && Path.HasExtension(Path.GetFileName(Library))
                            ? GetFileNameFromLibraryName()
                            : GetFileNameFromFunctionName();
            }
        }


        /// <summary>
        /// Line number in source code where exception occurs
        /// </summary>
        public int Line;

        /// <summary>
        /// PBD Unique identifier
        /// </summary>
        public string MemberInfo;


        /// <summary>
        /// Full path to source code where exception occurs
        /// </summary>
        public string SourceCodeFullPath;

        /// <summary>
        /// Column number in source code where exception occurs
        /// </summary>
        public int Column;

        /// <summary>
        /// Address of the stack frame
        /// </summary>
        public int ILOffset;

        /// <summary>
        /// Source code file name where exception occurs
        /// </summary>
        public string SourceCode;

        /// <summary>
        /// Function address
        /// </summary>
        public string Address;

        /// <summary>
        /// Assembly name
        /// </summary>
        public string Assembly;

        /// <summary>
        /// Invalid stack frame
        /// </summary>
        public bool InvalidFrame { get; set; }
        public BacktraceJObject ToJson()
        {
            var stackFrame = new BacktraceJObject(new Dictionary<string, string>()
            {
                ["funcName"] = FunctionName,
                ["path"] = FileName,
                ["metadata_token"] = MemberInfo,
                ["assembly"] = Assembly
            });

            stackFrame.Add("address", ILOffset);
            if (!string.IsNullOrEmpty(Library) && !(Library.StartsWith("<") && Library.EndsWith(">")))
            {
                stackFrame.Add("library", Library);
            }

            if (Line != 0)
            {
                stackFrame.Add("line", Line);
            }

            if (Column != 0)
            {
                stackFrame.Add("column", Column);
            }

            if (!string.IsNullOrEmpty(SourceCode))
            {
                stackFrame.Add("sourceCode", SourceCode);
            }

            return stackFrame;
        }

        /// <summary>
        /// Library name where exception occurs
        /// </summary>

        public string Library;


        public BacktraceStackFrame()
        { }

        public BacktraceStackFrame(StackFrame frame, bool generatedByException)
        {
            if (frame == null)
            {
                InvalidFrame = true;
                return;
            }
            var method = frame.GetMethod();
            if (method == null)
            {
                InvalidFrame = true;
                return;
            }

            var declaringType = method.DeclaringType;
            string assembly = "unknown";
            if (declaringType != null)
            {
                var assemblyName = declaringType.Assembly.GetName().Name;
                if (assemblyName != null)
                {
                    assembly = assemblyName;
                    if (assemblyName == "Backtrace.Unity")
                    {
                        InvalidFrame = true;
                        return;
                    }
                }
            }

            FunctionName = GetMethodName(method);
            SourceCodeFullPath = frame.GetFileName();
            Line = frame.GetFileLineNumber();
            ILOffset = frame.GetILOffset();
            Assembly = assembly;
            Library = string.IsNullOrEmpty(SourceCodeFullPath) ? method.DeclaringType.ToString() : SourceCodeFullPath;

            Column = frame.GetFileColumnNumber();
            try
            {
                MemberInfo = method.MetadataToken.ToString(CultureInfo.InvariantCulture);
            }
            catch (InvalidOperationException)
            {
                //metadata token in some situations can throw Argument Exception. Plase check property definition to leran more about this behaviour
            }
            InvalidFrame = false;
        }



        /// <summary>
        /// Generate valid name for current stack frame.
        /// </summary>
        /// <returns>Valid method name in stack trace</returns>
        private string GetMethodName(MethodBase method)
        {
            var methodName = method.Name.StartsWith(".") ? method.Name.Substring(1, method.Name.Length - 1) : method.Name;
            return string.Format("{0}.{1}()", method.DeclaringType == null ? null : method.DeclaringType.ToString(), methodName);
        }

        private string GetFileNameFromLibraryName()
        {
            var libraryName = Path.GetFileName(Library).Trim();

            // detect namespace
            var lastSeparatorIndex = libraryName.LastIndexOf(".");
            if (lastSeparatorIndex == -1 || libraryName.IndexOf(".") == lastSeparatorIndex)
            {
                // detected full path to source code
                return libraryName;
            }

            // omit '.' character that substring will return based on lastSeparatorIndex
            libraryName = libraryName.Substring(lastSeparatorIndex + 1);
            switch (StackFrameType)
            {
                case BacktraceStackFrameType.Dotnet:
                    return string.Format("{0}.cs", libraryName);
                case BacktraceStackFrameType.Android:
                    return string.Format("{0}.java", libraryName);
                default:
                    return libraryName;
            }
        }

        /// <summary>
        /// Generate file name based on full functiom name
        /// </summary>
        /// <returns>File name</returns>
        private string GetFileNameFromFunctionName()
        {
            if (string.IsNullOrEmpty(FunctionName))
            {
                return string.Empty;
            }
            // use Function name instead and try to get last part of function
            var lastSearchIndex = FunctionName.IndexOf('(');
            if (lastSearchIndex == -1)
            {
                lastSearchIndex = FunctionName.Length - 1;
            }

            int separatorIndex = -1;
            for (int arrayIndex = 0; arrayIndex < _frameSeparators.Length; arrayIndex++)
            {
                separatorIndex = FunctionName.LastIndexOf(_frameSeparators[arrayIndex], lastSearchIndex);
                if (separatorIndex != -1)
                {
                    break;
                }
            }

            if (separatorIndex == -1)
            {
                return string.Empty;
            }

            var libraryPath = FunctionName.Substring(0, separatorIndex).Split(new char[] { '.' });
            // handle situation when function name is a constructor path or specific module path
            var currentIndex = libraryPath.Length - 1;
            string fileName = libraryPath[currentIndex];

            while (string.IsNullOrEmpty(fileName) && currentIndex > 0)
            {
                fileName = libraryPath[currentIndex - 1];
                currentIndex--;

            }
            if (string.IsNullOrEmpty(fileName))
            {
                return Library;
            }

            if (fileName.IndexOfAny(Path.GetInvalidPathChars()) == -1 && Path.HasExtension(fileName) || StackFrameType == BacktraceStackFrameType.Unknown)
            {
                return fileName;
            }

            switch (StackFrameType)
            {
                case BacktraceStackFrameType.Dotnet:
                    return string.Format("{0}.cs", fileName);
                case BacktraceStackFrameType.Android:
                    return string.Format("{0}.java", fileName);
                default:
                    return fileName;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} (at {1}:{2})", FunctionName, Library, Line.ToString(CultureInfo.InvariantCulture));
        }
    }
}
