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
    private final long  _mainThreadId;

    public BacktraceAndroidBackgroundUnhandledExceptionHandler(String gameObject, String methodName) {
        Log.d(LOG_TAG, "Initializing Android unhandled exception handler");
        this._gameObject = gameObject;
        this._methodName = methodName;
        _mainThreadId = Looper.getMainLooper().getThread().getId();
        mRootHandler = Thread.getDefaultUncaughtExceptionHandler();
        Thread.setDefaultUncaughtExceptionHandler(this);
    }

    @Override
    public void uncaughtException(final Thread thread, final Throwable throwable) {
        Log.d(LOG_TAG, "Captured unhandled android exception");
        if (shouldStop == false) {
            Log.d(LOG_TAG, "Detected an exception generated in the main thread");
            mRootHandler.uncaughtException(thread, throwable);
        }
        String throwableType = throwable.getClass().getName();
        Log.d(LOG_TAG, "Detected unhandled background thread exception. Exception type: " + throwableType + ". Reporting to Backtrace");
        ReportThreadException(throwableType + " : " + throwable.getMessage(), stackTraceToString(throwable.getStackTrace()));
        mRootHandler.uncaughtException(thread, throwable);
    }

    public void ReportThreadException(String message, String stackTrace) {        
        UnityPlayer.UnitySendMessage(this._gameObject, this._methodName, message + '\n' + stackTrace);
        Log.d(LOG_TAG, "UnitySendMessageFinished. passing an exception object.");
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