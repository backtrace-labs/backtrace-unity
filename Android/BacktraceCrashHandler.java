package backtraceio.library.nativeCalls;

import android.util.Log;

import java.util.Map;

public class BacktraceCrashHandler {
    private static final String LOG_TAG = BacktraceCrashHandler.class.getSimpleName();
    
    public static final String BACKTRACE_CRASH_HANDLER = "BACKTRACE_UNITY_CRASH_HANDLER";


    private static native boolean handleCrash(String[] args);

    public static void main(String[] args) {
        run(args, System.getenv());
    }

    public static boolean run(String[] args, Map<String, String> environmentVariables) {
        if (environmentVariables == null) {
            Log.e(LOG_TAG, "Cannot capture crash dump. Environment variables are undefined");
            return false;
        }

        String crashHandlerLibrary = environmentVariables.get(BACKTRACE_CRASH_HANDLER);
        if (crashHandlerLibrary == null) {
            Log.e(LOG_TAG, String.format("Cannot capture crash dump. Cannot find %s environment variable", BACKTRACE_CRASH_HANDLER));
            return false;
        }
        System.load(crashHandlerLibrary);

        boolean result = handleCrash(args);
        if (!result) {
            Log.e(LOG_TAG, String.format("Cannot capture crash dump. Invocation parameters: %s", String.join(" ", args)));
            return false;
        }

        Log.i(LOG_TAG, "Successfully ran crash handler code.");
        return true;
    }
}
