using Backtrace.Newtonsoft;
using Backtrace.Unity.Interfaces.Database;
using System.IO;
using System.Text;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace Backtrace.Unity.Model.Database
{
    /// <summary>
    /// Database record writer
    /// </summary>
    internal class BacktraceDatabaseRecordWriter : IBacktraceDatabaseRecordWriter
    {
        /// <summary>
        /// Path to destination directory
        /// </summary>
        private readonly string _destinationPath;

        /// <summary>
        /// Initialize new database record writer
        /// </summary>
        /// <param name="path">Path to destination folder</param>
        internal BacktraceDatabaseRecordWriter(string path)
        {
            _destinationPath = path;
        }

        public string Write(object data, string prefix)
        {
            var json = ToJsonFile(data);
            byte[] file = Encoding.UTF8.GetBytes(json);
            return Write(file, prefix);
        }

        public virtual string Write(byte[] data, string prefix)
        {
            string filename = $"{prefix}.json";
            string tempFilePath = Path.Combine(_destinationPath, $"temp_{filename}");
            SaveTemporaryFile(tempFilePath, data);
            string destFilePath = Path.Combine(_destinationPath, filename);
            SaveValidRecord(tempFilePath, destFilePath);
            return destFilePath;
        }

        public virtual string ToJsonFile(object data)
        {
            if (data == null)
            {
                return string.Empty;
            }
            return BacktraceDataConverter.SerializeObject(data);
        }

        /// <summary>
        /// Save valid diagnostic data from temporary file
        /// </summary>
        /// <param name="sourcePath">Temporary file path</param>
        /// <param name="destinationPath">destination path</param>
        public virtual void SaveValidRecord(string sourcePath, string destinationPath)
        {
            File.Move(sourcePath, destinationPath);
        }

        /// <summary>
        /// Save temporary file to hard drive.
        /// </summary>
        /// <param name="path">Path to temporary file</param>
        /// <param name="file">Current file</param>
        public virtual void SaveTemporaryFile(string path, byte[] file)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                fs.Write(file, 0, file.Length);
            }
        }
    }
}
