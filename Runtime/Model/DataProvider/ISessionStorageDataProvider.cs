[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Backtrace.Unity.Tests.Runtime")]
namespace Backtrace.Unity.Model.DataProvider
{
    internal interface ISessionStorageDataProvider
    {
        void SetString(string key, string value);
        string GetString(string key);
    }
}
