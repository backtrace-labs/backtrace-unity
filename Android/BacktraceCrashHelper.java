package backtrace.io.backtrace_unity_android_plugin;

import android.os.Handler;
import android.os.Looper;
import android.util.Log;


public class BacktraceCrashHelper {
    public static void StartAnr() {
        Log.d("BacktraceCrashHelper", "Starting ANR");
        Handler handler = new Handler(Looper.getMainLooper());
        handler.post(new Runnable() {
            @Override
            public void run() {
                try {
                    Thread.sleep(10000);
                } catch (InterruptedException e) {
                    e.printStackTrace();
                }
            }
        });
    }

    public static void ThrowNativeException() {
        Log.d("BacktraceCrashHelper", "Trying to throw native exception");
        BacktraceCrashHelper.InternalCall();
    }

    private static void InternalCall(){
        int[] numbers = {10, 20, 30, 40};
        Log.d("BacktraceCrashHelper", String.valueOf(numbers[5]));
    }
}
