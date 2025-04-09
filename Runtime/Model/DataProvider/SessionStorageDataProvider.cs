using UnityEngine;

namespace Backtrace.Unity.Model.DataProvider
{
    internal class SessionStorageDataProvider : ISessionStorageDataProvider
    {
        public string GetString(string key)
        {
            return PlayerPrefs.GetString(key);
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }
    }
}
