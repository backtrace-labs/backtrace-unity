#if !UNITY_WINRT || UNITY_EDITOR || (UNITY_WP8 &&  !UNITY_WP_8_1)
using System;
using Backtrace.Newtonsoft.Shims;

namespace System.ComponentModel
{
    [Preserve]
    public delegate void PropertyChangingEventHandler(Object sender, PropertyChangingEventArgs e);

}

#endif