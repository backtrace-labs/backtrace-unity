#if !UNITY_WINRT || UNITY_EDITOR || (UNITY_WP8 &&  !UNITY_WP_8_1)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backtrace.Newtonsoft.Shims;

namespace System.ComponentModel
{
    [Preserve]
    public delegate void AddingNewEventHandler(Object sender, AddingNewEventArgs e);
}

#endif