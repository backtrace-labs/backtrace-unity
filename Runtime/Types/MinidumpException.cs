using System;
using System.Collections.Generic;
using System.Text;

namespace Backtrace.Unity.Types
{
    /// <summary>
    /// Set information if exception present
    /// </summary>
    internal enum MinidumpException
    {
        /// <summary>
        /// There is no exception in current context - sending message
        /// </summary>
        None,
        /// <summary>
        /// An exception exists and should be added to minidump file
        /// </summary>
        Present
    }
}
