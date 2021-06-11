
using System.IO;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Model.Breadcrumbs.Storage
{
    internal interface IBreadcrumbFile
    {
        bool Exists();
        void Delete();
        Stream GetCreateStream();

        Stream GetIOStream();
        Stream GetWriteStream();

    }
}
