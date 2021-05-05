namespace Backtrace.Unity.Editor
{
    internal static class BacktraceConfigurationLabels
    {
        //client labels
        internal static string LABEL_SERVER_URL = "Server Address";
        internal static string LABEL_REPORT_PER_MIN = "Reports per minute";
        internal static string LABEL_HANDLE_UNHANDLED_EXCEPTION = "Handle unhandled exceptions";

        internal static string LABEL_DESTROY_CLIENT_ON_SCENE_LOAD = "Destroy client on new scene load (false - Backtrace managed)";
        internal static string LABEL_SAMPLING = "Log random sampling rate";
        internal static string LABEL_HANDLE_ANR = "Handle ANR (Application not responding)";
#if UNITY_ANDROID || UNITY_IOS
        internal static string LABEL_HANDLE_OOM =
#if UNITY_ANDROID
            "(Early access) Send Low memory warnings to Backtrace";
#elif UNITY_IOS
            "(Early access) Send Out of memory exceptions to Backtrace";
#endif
#endif

        internal const string LABEL_ENABLE_EVENT_AGGREGATION = "Enable default crash free events";
        internal const string LABEL_EVENT_AGGREGATION_URL = "Event aggregation submission URL";
        internal const string LABEL_EVENT_AGGREGATION_TIME_INTERVAL = "Event aggregation time interval in ms";
        internal const string LABEL_EVENT_AGGREGATION_MAXIMUM_NUMBER_OF_EVENTS = "Maximum number of events in the event aggregation storage";


        internal static string LABEL_REPORT_ATTACHMENTS = "Report attachment paths";
        internal static string CAPTURE_NATIVE_CRASHES = "Capture native crashes";
        internal static string LABEL_REPORT_FILTER = "Filter reports";
        internal static string LABEL_NUMBER_OF_LOGS = "Collect last n game logs";
        internal static string LABEL_GAME_OBJECT_DEPTH = "Game object depth limit";
        internal static string LABEL_IGNORE_SSL_VALIDATION = "Ignore SSL validation";
        internal static string LABEL_SEND_UNHANDLED_GAME_CRASHES_ON_STARTUP = "Send unhandled native game crashes on startup";
        internal static string LABEL_USE_NORMALIZED_EXCEPTION_MESSAGE = "Use normalized exception message";
        internal static string LABEL_PERFORMANCE_STATISTICS = "Enable performance statistics";
        internal static string LABEL_SYMBOLS_UPLOAD_TOKEN = "Symbols upload token";

        // database labels
        internal static string LABEL_ENABLE_DATABASE = "Enable Database";
        internal static string LABEL_PATH = "Backtrace database path";
        internal static string LABEL_MINIDUMP_SUPPORT = "Minidump type";
        internal static string LABEL_ADD_UNITY_LOG = "Attach Unity Player.log";
        internal static string LABEL_GENERATE_SCREENSHOT_ON_EXCEPTION = "Attach screenshot";
        internal static string LABEL_DEDUPLICATION_RULES = "Client-Side deduplication";
        internal static string LABEL_AUTO_SEND_MODE = "Auto send mode";
        internal static string LABEL_CREATE_DATABASE_DIRECTORY = "Create database directory";
        internal static string LABEL_MAX_REPORT_COUNT = "Maximum number of records";
        internal static string LABEL_MAX_DATABASE_SIZE = "Maximum database size (mb)";
        internal static string LABEL_RETRY_INTERVAL = "Retry interval";
        internal static string LABEL_RETRY_LIMIT = "Maximum retries";
        internal static string LABEL_RETRY_ORDER = "Retry order (FIFO/LIFO)";


    }
}
