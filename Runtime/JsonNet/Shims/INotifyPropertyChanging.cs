using Backtrace.Newtonsoft.Shims;

#if !UNITY_WINRT || UNITY_EDITOR || (UNITY_WP8 &&  !UNITY_WP_8_1)
namespace System.ComponentModel
{
    [Preserve]
    public interface INotifyPropertyChanging
	{
#pragma warning disable CS0436 // Type conflicts with imported type
        event PropertyChangingEventHandler PropertyChanging;
#pragma warning restore CS0436 // Type conflicts with imported type
    }
}

#endif