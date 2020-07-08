package backtrace.io.backtrace_unity_android_plugin;
import android.os.Debug;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import java.io.PrintWriter;
import java.io.StringWriter;
import java.util.Calendar;


/**
 * This is the class that is responsible for monitoring the
 * user interface thread and sending an error if it is blocked
 */
public class BacktraceANRWatchdog extends Thread {

    private static BacktraceANRWatchdog _instance;

    private final static transient String LOG_TAG = BacktraceANRWatchdog.class.getSimpleName();

    /**
     * Default timeout value in milliseconds
     */
    private final static transient int DEFAULT_ANR_TIMEOUT = 5000;


    /**
     * Enable debug mode - errors will not be sent if the debugger is connected
     */
    private final boolean debug;
    /**
     * Handler for UI Thread - used to check if the thread is not blocked
     */
    private final Handler mainThreadHandler = new Handler(Looper.getMainLooper());
    /**
     * Maximum time in milliseconds after which should check if the main thread is not hanged
     */
    private int timeout;


    /**
     * Check if thread should stop
     */
    private volatile boolean shouldStop = false;


    /**
     * Game object name - required by JNI to learn how to call Backtrace Unity plugin
     * when library will detect ANR
     */
    private String gameObjectName;


    /**
     * Unity library callback method name
     */
    private String methodName;

    /**
     * Initialize new instance of BacktraceANRWatchdog with default timeout
     */
    public BacktraceANRWatchdog(String gameObjectName, String methodName) {
        Log.d(LOG_TAG, "Initializing ANR watchdog");
        this.methodName = methodName;
        this.gameObjectName = gameObjectName;
        this.timeout = DEFAULT_ANR_TIMEOUT;
        this.debug = false;
        BacktraceANRWatchdog._instance = this;
        this.start();
    }

    /**
     * Method which is using to check if the user interface thread has been blocked
     */
    @Override
    public void run() {
        while (!shouldStop && !isInterrupted()) {
            String dateTimeNow = Calendar.getInstance().getTime().toString();
            Log.d(LOG_TAG, "ANR WATCHDOG - " + dateTimeNow);
            final backtrace.io.backtrace_unity_android_plugin.BacktraceThreadWatcher threadWatcher = new backtrace.io.backtrace_unity_android_plugin.BacktraceThreadWatcher(0, 0);
            mainThreadHandler.post(new Runnable() {
                @Override
                public void run() {
                    threadWatcher.tickCounter();
                }
            });
            try {
                Thread.sleep(this.timeout);
            } catch (InterruptedException e) {
                Log.d(LOG_TAG, "Thread is interrupted", e);
                return;
            }
            threadWatcher.tickPrivateCounter();

            if (threadWatcher.getCounter() == threadWatcher.getPrivateCounter()) {
                Log.d(LOG_TAG, "ANR is not detected");
                continue;
            }

            if (debug && (Debug.isDebuggerConnected() || Debug.waitingForDebugger())) {
                Log.d(LOG_TAG, "ANR detected but will be ignored because debug mode " +
                        "is on and connected debugger");
                continue;
            }
            NotifyUnityAboutANR();
        }
    }

    public void NotifyUnityAboutANR() {
        String stackTrace = stackTraceToString(Looper.getMainLooper().getThread().getStackTrace());
        Log.d(LOG_TAG, stackTrace);
        UnityPlayer.UnitySendMessage(this.gameObjectName, this.methodName, stackTrace);
    }

    public static String stackTraceToString(StackTraceElement[] stackTrace) {
        StringWriter sw = new StringWriter();
        printStackTrace(stackTrace, new PrintWriter(sw));
        return sw.toString();
    }
    public static void printStackTrace(StackTraceElement[] stackTrace, PrintWriter pw) {
        for(StackTraceElement stackTraceEl : stackTrace) {
            pw.println(stackTraceEl);
        }
    }

    public void stopMonitoringAnr() {
        Log.d(LOG_TAG, "Stop monitoring ANR");
        shouldStop = true;
    }
}
