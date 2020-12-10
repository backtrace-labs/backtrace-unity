namespace Backtrace.Unity.Interfaces.Database
{
    internal interface IBacktraceDatabaseRecordWriter
    {
        string Write(string data, string prefix);
        string Write(byte[] data, string prefix);
        void Save(string path, byte[] file);
    }
}
