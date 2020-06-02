using System;
using UnityEngine;

namespace Backtrace.Unity.Types
{

    /// <summary>
    /// Determine deduplication strategy
    /// </summary>
    [Flags]
    public enum DeduplicationStrategy
    {
        /// <summary>
        /// Ignore deduplication strategy
        /// </summary>
        [Tooltip("Deduplication rules are disabled.")]
        [InspectorName("Disabled")]
        None = 0,

        /// <summary>
        /// Only stack trace
        /// </summary>
        [Tooltip("Faulting callstack - use the faulting callstack as a factor in client-side rate limiting.")]
        [InspectorName("Faulting callstack")]        
        Default = 1,

        /// <summary>
        /// Stack trace and exception type
        /// </summary>
        [Tooltip("Unity by default will validate ssl certificates. By using this option you can avoid ssl certificates validation. However, if you don't need to ignore ssl validation, please set this option to false.", order = 0)]
        [InspectorName("Exception type")]
        Classifier = 2,

        /// <summary>
        /// Stack trace and exception message
        /// </summary>
        [Tooltip("Unity by default will validate ssl certificates. By using this option you can avoid ssl certificates validation. However, if you don't need to ignore ssl validation, please set this option to false.", order = 0)]
        [InspectorName("Exception message")]
        Message = 4
    }
}