package backtraceio.unity;
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

    private final static transient String LOG_TAG = BacktraceANRWatchdog.class.getSimpleName();

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
    public BacktraceANRWatchdog(String gameObjectName, String methodName, int anrTimeout) {
        Log.d(LOG_TAG, "Initializing ANR watchdog");
        this.methodName = methodName;
        this.gameObjectName = gameObjectName;
        this.timeout = anrTimeout;
        this.start();
    }

    /**
     * Method which is using to check if the user interface thread has been blocked
     */
    @Override
    public void run() {
        if (Debug.isDebuggerConnected() || Debug.waitingForDebugger()) {
            Log.d(LOG_TAG, "Detected a debugger connection. ANR Watchdog is disabled");
            return;
        }
        
        Boolean reported = false;
        Log.d(LOG_TAG, "Starting ANR watchdog. Anr timeout: " + this.timeout);

        while (!shouldStop && !isInterrupted()) {
            final BacktraceThreadWatcher threadWatcher = new BacktraceThreadWatcher(0, 0);
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
                reported = false;
                continue;
            }

            if (reported) {
                // skipping, because we already reported an ANR report for current ANR
                continue;
            }
            reported = true;
            Log.d(LOG_TAG, "Detected blocked Java thread. Reporting Java ANR.");
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

    public void stopMonitoring() {
        Log.d(LOG_TAG, "ANR handler has been disabled.");
        shouldStop = true;
    }
}
