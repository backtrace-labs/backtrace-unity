#if !UNITY_WINRT || UNITY_EDITOR || (UNITY_WP8 &&  !UNITY_WP_8_1)
using Backtrace.Newtonsoft.Shims;

namespace System.ComponentModel
{
    [Preserve]
#pragma warning disable CS0436 // Type conflicts with imported type
    public delegate void AddingNewEventHandler(object sender, AddingNewEventArgs e);
#pragma warning restore CS0436 // Type conflicts with imported type
}

#endif