using Backtrace.Newtonsoft.Shims;

#if !UNITY_WINRT || UNITY_EDITOR || (UNITY_WP8 &&  !UNITY_WP_8_1)
namespace System.ComponentModel
{
    [Preserve]
    public interface INotifyPropertyChanging
	{
		event PropertyChangingEventHandler PropertyChanging;
	}
}

#endif