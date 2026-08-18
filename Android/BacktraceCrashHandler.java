package backtraceio.library.nativeCalls;

import android.util.Log;

import java.util.Map;

/**
 * Entry point of the crash-handler child process spawned through app_process.
 * Every failure path exits nonzero and logs a stable BT_HANDLER_* code with the failure class only
 * (never the argument vector, which carries the submission URL, attributes, attachments, and resolved paths)
 */
public final class BacktraceCrashHandler {
    private static final String LOG_TAG = BacktraceCrashHandler.class.getSimpleName();

    public static final String BACKTRACE_CRASH_HANDLER = "BACKTRACE_UNITY_CRASH_HANDLER";

    private static native boolean handleCrash(String[] args);

    private BacktraceCrashHandler() {}

    public static void main(String[] args) {
        if (!run(args, System.getenv())) {
            System.exit(1);
        }
    }

    public static boolean run(String[] args, Map<String, String> environmentVariables) {
        if (environmentVariables == null) {
            Log.e(LOG_TAG, "BT_HANDLER_ENV_UNAVAILABLE: Crash-handler environment is unavailable.");
            return false;
        }

        String crashHandlerLibrary = environmentVariables.get(BACKTRACE_CRASH_HANDLER);
        if (crashHandlerLibrary == null || crashHandlerLibrary.trim().isEmpty()) {
            Log.e(LOG_TAG, "BT_HANDLER_PATH_UNAVAILABLE: Crash-handler library path is unavailable.");
            return false;
        }

        try {
            System.load(crashHandlerLibrary);
        } catch (RuntimeException | LinkageError failure) {
            Log.e(LOG_TAG, "BT_HANDLER_LOAD_FAILURE: Cannot load the native crash-handler library. "
                    + "Failure type: " + failureType(failure));
            return false;
        }

        final boolean result;
        try {
            result = handleCrash(args == null ? new String[0] : args);
        } catch (RuntimeException | LinkageError failure) {
            Log.e(LOG_TAG, "BT_HANDLER_DISPATCH_FAILURE: Cannot execute the native crash handler. "
                    + "Failure type: " + failureType(failure));
            return false;
        }

        if (!result) {
            Log.e(LOG_TAG, "BT_HANDLER_RETURNED_FAILURE: The native crash handler returned failure.");
            return false;
        }

        Log.i(LOG_TAG, "Successfully ran crash-handler code.");
        return true;
    }

    private static String failureType(Throwable failure) {
        if (failure == null) {
            return "unknown";
        }
        String type = failure.getClass().getName();
        return type == null || type.trim().isEmpty() ? "unknown" : type;
    }
}
