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
    * Last caught background exception/thread that will be passed to the main thread when Unity notifies 
    * that the C# layer stored the data in database/sent it to user
    */
    private Thread _lastCaughtBackgroundExceptionThread;
    private Throwable _lastCaughtBackgroundException;

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
    public void uncaughtException(final Thread thread, final Throwable throwable) {
        _lastCaughtBackgroundExceptionThread = thread;
        _lastCaughtBackgroundException = throwable;
        if (shouldStop == true) {
            Log.d(LOG_TAG, "Background exception handler is disabled.");
            finish();
            return;
        }
        String throwableType = throwable.getClass().getName();
        Log.d(LOG_TAG, "Detected unhandled background thread exception. Exception type: " + throwableType + ". Reporting to Backtrace");
        ReportThreadException(throwableType + " : " + throwable.getMessage(), stackTraceToString(throwable.getStackTrace()));
    }

    public void ReportThreadException(String message, String stackTrace) {        
        UnityPlayer.UnitySendMessage(this._gameObject, this._methodName, message + '\n' + stackTrace);
        Log.d(LOG_TAG, "UnitySendMessageFinished. passing an exception object. Game object: " + this._gameObject + " method name: " + this._methodName);
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

    public void finish() {
        if (_lastCaughtBackgroundExceptionThread == null || _lastCaughtBackgroundException == null) {
            Log.d(LOG_TAG, "The exception object or the exception thread is not available. This is probably a bug.");
            return;
        }
        if (shouldStop) {
            Log.d(LOG_TAG, "Backtrace client has been disposed. The report won't be available.");
            return;
        }
        mRootHandler.uncaughtException(_lastCaughtBackgroundExceptionThread, _lastCaughtBackgroundException);
    }


    public void stopMonitoring() {
        Log.d(LOG_TAG, "Uncaught exception handler has been disabled.");
        shouldStop = true;
    }
} 