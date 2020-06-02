namespace Backtrace.Unity.Editor
{
    internal static class BacktraceConfigurationLabels
    {
        internal static string LABEL_SERVER_URL = "Server Address";
        internal static string LABEL_REPORT_PER_MIN = "Reports per minute";
        internal static string LABEL_HANDLE_UNHANDLED_EXCEPTION = "Handle unhandled exceptions";
        internal static string LABEL_ENABLE_DATABASE = "Enable Database";

#if UNITY_2018_4_OR_NEWER
        internal static string LABEL_IGNORE_SSL_VALIDATION = "Ignore SSL validation";
#endif
        internal static string LABEL_DEDUPLICATION_RULES = "Client-Side deduplication";
        internal static string LABEL_GAME_OBJECT_DEPTH = "Game object depth limit";

        internal static string LABEL_DESTROY_CLIENT_ON_SCENE_LOAD = "Destroy client on new scene load (false - Backtrace managed)";

        internal static string LABEL_MINIDUMP_SUPPORT = "Minidump type";

        internal static string LABEL_PATH = "Backtrace database path";
        internal static string LABEL_AUTO_SEND_MODE = "Auto send mode";
        internal static string LABEL_CREATE_DATABASE_DIRECTORY = "Create database directory";
        internal static string LABEL_MAX_REPORT_COUNT = "Maximum number of records";
        internal static string LABEL_MAX_DATABASE_SIZE = "Maximum database size (mb)";
        internal static string LABEL_RETRY_INTERVAL = "Retry interval";
        internal static string LABEL_RETRY_LIMIT = "Maximum retries";
        internal static string LABEL_RETRY_ORDER = "Retry order (FIFO/LIFO)";
    }
}
