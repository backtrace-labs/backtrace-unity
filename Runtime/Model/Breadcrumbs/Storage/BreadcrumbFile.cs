using System.IO;

namespace Backtrace.Unity.Model.Breadcrumbs.Storage
{
    internal sealed class BreadcrumbFile : IBreadcrumbFile
    {
        private readonly string _path;
        public BreadcrumbFile(string path)
        {
            _path = path;
        }

        public void Delete()
        {
            File.Delete(_path);
        }

        public bool Exists()
        {
            return File.Exists(_path);
        }

        public Stream GetCreateStream()
        {
            return new FileStream(_path, FileMode.CreateNew, FileAccess.Write);
        }

        public Stream GetIOStream()
        {
            return new FileStream(_path, FileMode.Open, FileAccess.ReadWrite);
        }

        public Stream GetWriteStream()
        {
            return new FileStream(_path, FileMode.Open, FileAccess.Write);
        }
    }
}
