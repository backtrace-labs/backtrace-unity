using Backtrace.Unity.Interfaces.Database;
using System.IO;
using System.Text;

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

        public string Write(string json, string prefix)
        {            
            return Write(Encoding.UTF8.GetBytes(json), prefix);
        }

        public string Write(byte[] data, string prefix)
        {
            string destFilePath = Path.Combine(_destinationPath, string.Format("{0}.json", prefix));
            Save(destFilePath, data);
            return destFilePath;
        }

        /// <summary>
        /// Save temporary file to hard drive.
        /// </summary>
        /// <param name="path">Path to temporary file</param>
        /// <param name="file">Current file</param>
        public void Save(string path, byte[] file)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                fs.Write(file, 0, file.Length);
            }
        }
    }
}
