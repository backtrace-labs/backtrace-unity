using System.IO;
using UnityEditor;
using UnityEngine;
using Backtrace.Unity.Model;
using Backtrace.Unity.Types;

namespace Backtrace.Unity.Port.Editor
{
    [CustomEditor(typeof(BacktraceConfiguration))]
    public class BacktraceConfigurationEditor : UnityEditor.Editor
    {
        public const string LABEL_SERVER_URL = "Server Address";
        public const string LABEL_TOKEN = "Token";
        public const string LABEL_REPORT_PER_MIN = "Reports per minute";
        public const string LABEL_HANDLE_UNHANDLED_EXCEPTION = "Handle unhandled exceptions";
        public const string LABEL_ENABLE_DATABASE = "Enable Database";

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

            SerializedProperty token = serializedObject.FindProperty("Token");
            EditorGUILayout.PropertyField(token, new GUIContent(LABEL_TOKEN));
            if (!BacktraceConfiguration.ValidateToken(token.stringValue))
            {
                EditorGUILayout.HelpBox("Token requires at least 64 characters!", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("ReportPerMin"),new GUIContent(LABEL_REPORT_PER_MIN));


            SerializedProperty unhandledExceptions = serializedObject.FindProperty("HandleUnhandledExceptions");
            EditorGUILayout.PropertyField(unhandledExceptions, new GUIContent(LABEL_HANDLE_UNHANDLED_EXCEPTION));

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