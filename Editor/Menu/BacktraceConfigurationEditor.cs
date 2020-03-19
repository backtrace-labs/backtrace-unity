#if UNITY_EDITOR
using Backtrace.Unity.Model;
using Backtrace.Unity.Model.JsonData;
using UnityEditor;
using UnityEngine;

namespace Backtrace.Unity.Editor
{
    [CustomEditor(typeof(BacktraceConfiguration))]
    public class BacktraceConfigurationEditor : UnityEditor.Editor
    {
        public const string LABEL_SERVER_URL = "Server Address";
        public const string LABEL_REPORT_PER_MIN = "Reports per minute";
        public const string LABEL_HANDLE_UNHANDLED_EXCEPTION = "Handle unhandled exceptions";
        public const string LABEL_ENABLE_DATABASE = "Enable Database";

#if UNITY_2018_4_OR_NEWER
        public const string LABEL_IGNORE_SSL_VALIDATION = "Ignore SSL validation";
#endif
        public const string LABEL_DEDUPLICATION_RULES = "Deduplication rules";
        public const string LABEL_GAME_OBJECT_DEPTH = "Game object depth limit";

        public const string LABEL_DESTROY_CLIENT_ON_SCENE_LOAD = "Destroy client on new scene load (false - Backtrace managed)";

        public const string LABEL_MINIDUMP_SUPPORT = "Minidump type";

        public const string LABEL_PATH = "Backtrace database path";
        public const string LABEL_AUTO_SEND_MODE = "Auto send mode";
        public const string LABEL_CREATE_DATABASE_DIRECTORY = "Create database directory";
        public const string LABEL_MAX_REPORT_COUNT = "Maximum number of records";
        public const string LABEL_MAX_DATABASE_SIZE = "Maximum database size (mb)";
        public const string LABEL_RETRY_INTERVAL = "Retry interval";
        public const string LABEL_RETRY_LIMIT = "Maximum retries";
        public const string LABEL_RETRY_ORDER = "Retry order (FIFO/LIFO)";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty serverUrl = serializedObject.FindProperty("ServerUrl");
            serverUrl.stringValue = BacktraceConfiguration.UpdateServerUrl(serverUrl.stringValue);
            EditorGUILayout.PropertyField(serverUrl, new GUIContent(LABEL_SERVER_URL));
            if (!BacktraceConfiguration.ValidateServerUrl(serverUrl.stringValue))
            {
                EditorGUILayout.HelpBox("Please insert valid Backtrace server url!", MessageType.Error);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("ReportPerMin"), new GUIContent(LABEL_REPORT_PER_MIN));

            SerializedProperty unhandledExceptions = serializedObject.FindProperty("HandleUnhandledExceptions");
            EditorGUILayout.PropertyField(unhandledExceptions, new GUIContent(LABEL_HANDLE_UNHANDLED_EXCEPTION));


#if UNITY_2018_4_OR_NEWER
            SerializedProperty sslValidation = serializedObject.FindProperty("IgnoreSslValidation");
            EditorGUILayout.PropertyField(sslValidation, new GUIContent(LABEL_IGNORE_SSL_VALIDATION));
#endif

            SerializedProperty deduplicationStrategy = serializedObject.FindProperty("DeduplicationStrategy");
            EditorGUILayout.PropertyField(deduplicationStrategy, new GUIContent(LABEL_DEDUPLICATION_RULES));

            SerializedProperty destroyOnLoad = serializedObject.FindProperty("DestroyOnLoad");
            EditorGUILayout.PropertyField(destroyOnLoad, new GUIContent(LABEL_DESTROY_CLIENT_ON_SCENE_LOAD));


            SerializedProperty gameObjectDepth = serializedObject.FindProperty("GameObjectDepth");
            EditorGUILayout.PropertyField(gameObjectDepth, new GUIContent(LABEL_GAME_OBJECT_DEPTH));

            if (gameObjectDepth.intValue < 0)
            {
                EditorGUILayout.HelpBox("Please inser value greater or equal 0", MessageType.Error);
            }

            SerializedProperty enabled = serializedObject.FindProperty("Enabled");
            EditorGUILayout.PropertyField(enabled, new GUIContent(LABEL_ENABLE_DATABASE));

            if (enabled.boolValue)
            {
                EditorGUILayout.LabelField("Backtrace Database settings.");

                SerializedProperty databasePath = serializedObject.FindProperty("DatabasePath");
                EditorGUILayout.PropertyField(databasePath, new GUIContent(LABEL_PATH));
                if (string.IsNullOrEmpty(databasePath.stringValue))
                {
                    EditorGUILayout.HelpBox("Please insert valid Backtrace database path!", MessageType.Error);
                }

#if UNITY_STANDALONE_WIN
                EditorGUILayout.HelpBox("Minidump support works only on Windows machines.", MessageType.Warning);
                SerializedProperty miniDumpType = serializedObject.FindProperty("MinidumpType");
                EditorGUILayout.PropertyField(miniDumpType, new GUIContent(LABEL_MINIDUMP_SUPPORT));
#endif

                SerializedProperty autoSendMode = serializedObject.FindProperty("AutoSendMode");
                EditorGUILayout.PropertyField(autoSendMode, new GUIContent(LABEL_AUTO_SEND_MODE));


                SerializedProperty createDatabase = serializedObject.FindProperty("CreateDatabase");
                EditorGUILayout.PropertyField(createDatabase, new GUIContent(LABEL_CREATE_DATABASE_DIRECTORY));

                SerializedProperty maxRecordCount = serializedObject.FindProperty("MaxRecordCount");
                EditorGUILayout.PropertyField(maxRecordCount, new GUIContent(LABEL_MAX_REPORT_COUNT));

                SerializedProperty maxDatabaseSize = serializedObject.FindProperty("MaxDatabaseSize");
                EditorGUILayout.PropertyField(maxDatabaseSize, new GUIContent(LABEL_MAX_DATABASE_SIZE));

                SerializedProperty retryInterval = serializedObject.FindProperty("RetryInterval");
                EditorGUILayout.PropertyField(retryInterval, new GUIContent(LABEL_RETRY_INTERVAL));

                EditorGUILayout.LabelField("Backtrace database require at least one retry.");
                SerializedProperty retryLimit = serializedObject.FindProperty("RetryLimit");
                EditorGUILayout.PropertyField(retryLimit, new GUIContent(LABEL_RETRY_LIMIT));

                SerializedProperty retryOrder = serializedObject.FindProperty("RetryOrder");
                EditorGUILayout.PropertyField(retryOrder, new GUIContent(LABEL_RETRY_ORDER));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}
#endif