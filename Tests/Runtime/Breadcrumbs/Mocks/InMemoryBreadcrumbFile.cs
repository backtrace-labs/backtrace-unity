using Backtrace.Unity.Model.Breadcrumbs.Storage;
using System.IO;

namespace Backtrace.Unity.Tests.Runtime.Breadcrumbs.Mocks
{
    public class InMemoryBreadcrumbFile : IBreadcrumbFile
    {
        public MemoryStream MemoryStream = new MemoryStream();

        public long Size
        {
            get
            {
                return MemoryStream.ToArray().Length;
            }
        }
        public bool FileExists { get; set; } = true;
        public void Delete()
        {
            return;
        }

        public bool Exists()
        {
            return FileExists;
        }

        public Stream GetCreateStream()
        {
            return MemoryStream;
        }

        public Stream GetIOStream()
        {
            RecreateMemoryStream();
            return MemoryStream;
        }

        public Stream GetWriteStream()
        {
            RecreateMemoryStream();
            return MemoryStream;
        }

        private void RecreateMemoryStream()
        {
            var memoryStream = new MemoryStream();
            var content = MemoryStream.ToArray();
            memoryStream.Write(content, 0, content.Length);
            MemoryStream = memoryStream;
        }
    }
}
