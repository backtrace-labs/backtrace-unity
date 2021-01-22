using System.Collections.Generic;

namespace Backtrace.Unity.Runtime.Native
{
    /// <summary>
    /// Backtrace native client definition
    /// </summary>
    internal interface INativeClient
    {
        /// <summary>
        /// Handle ANR - Application not responding events
        /// </summary>
        void HandleAnr(string gameObjectName, string callbackName);

        /// <summary>
        /// Set native attributes in attributes dictionary
        /// </summary>
        /// <param name="data">Attributes dictionary</param>
        void GetAttributes(Dictionary<string, string> data);

        /// <summary>
        /// Set native attribute
        /// </summary>
        /// <param name="key">attribute key</param>
        /// <param name="value">attribute value</param>
        void SetAttribute(string key, string value);

        /// <summary>
        /// Report OOM via Backtrace native library.
        /// </summary>
        /// <returns>true - if native crash reprorter is enabled. Otherwise false.</returns>
        bool OnOOM();

        /// <summary>
        /// Update native client internal ANR timer.
        /// </summary>
        void UpdateClientTime(float time);

        /// <summary>
        /// Disable native integration
        /// </summary>
        void Disable();
    }
}
