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
#if UNITY_2019_2_OR_NEWER
        [InspectorName("Disable")]
#endif
        None = 0,

        /// <summary>
        /// Only stack trace
        /// </summary>
        [Tooltip("Faulting callstack - use the faulting callstack as a factor in client-side rate limiting.")]
#if UNITY_2019_2_OR_NEWER
        [InspectorName("Faulting callstack")]
#endif
        Default = 1,

        /// <summary>
        /// Stack trace and exception type
        /// </summary>
        [Tooltip("Unity by default will validate ssl certificates. By using this option you can avoid ssl certificates validation. However, if you don't need to ignore ssl validation, please set this option to false.", order = 0)]
#if UNITY_2019_2_OR_NEWER
        [InspectorName("Exception type")]
#endif
        Classifier = 2,

        /// <summary>
        /// Stack trace and exception message
        /// </summary>
        [Tooltip("Unity by default will validate ssl certificates. By using this option you can avoid ssl certificates validation. However, if you don't need to ignore ssl validation, please set this option to false.", order = 0)]
#if UNITY_2019_2_OR_NEWER
        [InspectorName("Exception message")]
#endif
        Message = 4
    }
}