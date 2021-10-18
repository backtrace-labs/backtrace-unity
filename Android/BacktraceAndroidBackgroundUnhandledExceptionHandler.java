package backtrace.io.backtrace_unity_android_plugin;

import android.os.Build;
import android.os.Looper;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import java.io.PrintWriter;
import java.io.StringWriter;

/**
 * Handle unhandled Android exceptions from background threads.
 */
public class BacktraceAndroidBackgroundUnhandledExceptionHandler implements Thread.UncaughtExceptionHandler{
    private final static transient String LOG_TAG = BacktraceAndroidBackgroundUnhandledExceptionHandler.class.getSimpleName();
    private final Thread.UncaughtExceptionHandler mRootHandler;

    /**
     * Check if data shouldn't be reported.
     */
    private volatile boolean shouldStop = false;

    private final String _gameObject;
    private final String _methodName;

    public BacktraceAndroidBackgroundUnhandledExceptionHandler(String gameObject, String methodName) {
        Log.d(LOG_TAG, "Initializing Android unhandled exception handler");
        this._gameObject = gameObject;
        this._methodName = methodName;

        mRootHandler = Thread.getDefaultUncaughtExceptionHandler();
        Thread.setDefaultUncaughtExceptionHandler(this);
    }

    @Override
    public void uncaughtException(Thread thread, Throwable exception) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.CUPCAKE && mRootHandler != null && shouldStop == false) {
            if(Looper.getMainLooper().getThread().getId() == thread.getId()) {
                // prevent from sending exception happened to main thread - we will catch them via unity logger
                return;
            }
            String exceptionType = exception.getClass().getName();
            Log.d(LOG_TAG, "Detected unhandled background thread exception. Exception type: " + exceptionType + ". Reporting to Backtrace");
            ReportThreadException(exceptionType + " : " + exception.getMessage(), stackTraceToString(exception.getStackTrace()));
        }
    }

    public void ReportThreadException(String message, String stackTrace) {        
        UnityPlayer.UnitySendMessage(this._gameObject, this._methodName, message + '\n' + stackTrace);
    }

    private static String stackTraceToString(StackTraceElement[] stackTrace) {
        StringWriter sw = new StringWriter();
        printStackTrace(stackTrace, new PrintWriter(sw));
        return sw.toString();
    }

    private static void printStackTrace(StackTraceElement[] stackTrace, PrintWriter pw) {
        for(StackTraceElement stackTraceEl : stackTrace) {
            pw.println(stackTraceEl);
        }
    }

    public void stopMonitoring() {
        Log.d(LOG_TAG, "Uncaught exception handler has been disabled.");
        shouldStop = true;
    }
} 