using Backtrace.Unity.Model;
using System;
using UnityEditor;
using UnityEngine;

namespace Backtrace.Unity.Editor
{
    [CustomEditor(typeof(BacktraceConfiguration))]
    public class BacktraceConfigurationEditor : UnityEditor.Editor
    {
        protected static bool showBreadcrumbsSettings = false;
        protected static bool showMetricsSettings = false;
        protected static bool showClientAdvancedSettings = false;
        protected static bool showDatabaseSettings = false;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty serverUrl = serializedObject.FindProperty("ServerUrl");
            serverUrl.stringValue = BacktraceConfiguration.UpdateServerUrl(serverUrl.stringValue);
            EditorGUILayout.PropertyField(serverUrl, new GUIContent(BacktraceConfigurationLabels.LABEL_SERVER_URL));
            if (!BacktraceConfiguration.ValidateServerUrl(serverUrl.stringValue))
            {
                EditorGUILayout.HelpBox("Please insert valid Backtrace server url!", MessageType.Error);
            }

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("HandleUnhandledExceptions"),
                new GUIContent(BacktraceConfigurationLabels.LABEL_HANDLE_UNHANDLED_EXCEPTION));

            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("ReportPerMin"),
                new GUIContent(BacktraceConfigurationLabels.LABEL_REPORT_PER_MIN));

            GUIStyle clientAdvancedSettingsFoldout = new GUIStyle(EditorStyles.foldout);
            showClientAdvancedSettings = EditorGUILayout.Foldout(showClientAdvancedSettings, "Client advanced settings", clientAdvancedSettingsFoldout);
            if (showClientAdvancedSettings)
            {

#if UNITY_2018_4_OR_NEWER

                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("IgnoreSslValidation"),
                    new GUIContent(BacktraceConfigurationLabels.LABEL_IGNORE_SSL_VALIDATION));
#endif
#if UNITY_ANDROID || UNITY_IOS
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("HandleANR"),
                     new GUIContent(BacktraceConfigurationLabels.LABEL_HANDLE_ANR));

                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("OomReports"),
                     new GUIContent(BacktraceConfigurationLabels.LABEL_HANDLE_OOM));

#if UNITY_2019_2_OR_NEWER && UNITY_ANDROID
                EditorGUILayout.PropertyField(
                   serializedObject.FindProperty("SymbolsUploadToken"),
                   new GUIContent(BacktraceConfigurationLabels.LABEL_SYMBOLS_UPLOAD_TOKEN));
#endif
#endif
                EditorGUILayout.PropertyField(
                   serializedObject.FindProperty("UseNormalizedExceptionMessage"),
                   new GUIContent(BacktraceConfigurationLabels.LABEL_USE_NORMALIZED_EXCEPTION_MESSAGE));
#if UNITY_STANDALONE_WIN
                EditorGUILayout.PropertyField(
                 serializedObject.FindProperty("SendUnhandledGameCrashesOnGameStartup"),
                 new GUIContent(BacktraceConfigurationLabels.LABEL_SEND_UNHANDLED_GAME_CRASHES_ON_STARTUP));
#endif
                EditorGUILayout.PropertyField(
                       serializedObject.FindProperty("ReportFilterType"),
                       new GUIContent(BacktraceConfigurationLabels.LABEL_REPORT_FILTER));

                EditorGUILayout.PropertyField(
                 serializedObject.FindProperty("PerformanceStatistics"),
                 new GUIContent(BacktraceConfigurationLabels.LABEL_PERFORMANCE_STATISTICS));

                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("DestroyOnLoad"),
                    new GUIContent(BacktraceConfigurationLabels.LABEL_DESTROY_CLIENT_ON_SCENE_LOAD));

                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("Sampling"),
                    new GUIContent(BacktraceConfigurationLabels.LABEL_SAMPLING));

                SerializedProperty gameObjectDepth = serializedObject.FindProperty("GameObjectDepth");
                EditorGUILayout.PropertyField(gameObjectDepth, new GUIContent(BacktraceConfigurationLabels.LABEL_GAME_OBJECT_DEPTH));

                if (gameObjectDepth.intValue < -1)
                {
                    EditorGUILayout.HelpBox("Please insert value greater or equal -1", MessageType.Error);
                }
            }

#if !UNITY_WEBGL
            GUIStyle metricsFoldout = new GUIStyle(EditorStyles.foldout);
            showMetricsSettings = EditorGUILayout.Foldout(showMetricsSettings, BacktraceConfigurationLabels.LABEL_CRASH_FREE_SECTION, metricsFoldout);
            if (showMetricsSettings)
            {
                var enableMetrics = serializedObject.FindProperty("EnableMetricsSupport");
                EditorGUILayout.PropertyField(
                    enableMetrics,
                    new GUIContent(BacktraceConfigurationLabels.LABEL_ENABLE_METRICS));

                if (enableMetrics.boolValue)
                {
                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("TimeIntervalInMin"),
                        new GUIContent(BacktraceConfigurationLabels.LABEL_METRICS_TIME_INTERVAL));
                }
            }
#endif
            EditorGUILayout.PropertyField(
            serializedObject.FindProperty("AttachmentPaths"),
            new GUIContent(BacktraceConfigurationLabels.LABEL_REPORT_ATTACHMENTS));


#if !UNITY_SWITCH
            SerializedProperty enabled = serializedObject.FindProperty("Enabled");
            EditorGUILayout.PropertyField(enabled, new GUIContent(BacktraceConfigurationLabels.LABEL_ENABLE_DATABASE));
            bool databaseEnabled = enabled.boolValue;
#else
            bool databaseEnabled = false;
#endif
            if (databaseEnabled)
            {

                SerializedProperty databasePath = serializedObject.FindProperty("DatabasePath");
                EditorGUILayout.PropertyField(databasePath, new GUIContent(BacktraceConfigurationLabels.LABEL_PATH));
                if (string.IsNullOrEmpty(databasePath.stringValue))
                {
                    EditorGUILayout.HelpBox("Please insert valid Backtrace database path!", MessageType.Error);
                }


                GUIStyle databaseFoldout = new GUIStyle(EditorStyles.foldout);
                showDatabaseSettings = EditorGUILayout.Foldout(showDatabaseSettings, "Advanced database settings", databaseFoldout);
                if (showDatabaseSettings)
                {
                    EditorGUILayout.PropertyField(
                       serializedObject.FindProperty("DeduplicationStrategy"),
                       new GUIContent(BacktraceConfigurationLabels.LABEL_DEDUPLICATION_RULES));

#if UNITY_STANDALONE_WIN
                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("MinidumpType"),
                        new GUIContent(BacktraceConfigurationLabels.LABEL_MINIDUMP_SUPPORT));
#endif

#if UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("AddUnityLogToReport"),
                        new GUIContent(BacktraceConfigurationLabels.LABEL_ADD_UNITY_LOG));

#endif


                    GUIStyle breadcrumbsSupportFoldout = new GUIStyle(EditorStyles.foldout);
                    showBreadcrumbsSettings = EditorGUILayout.Foldout(showBreadcrumbsSettings, BacktraceConfigurationLabels.LABEL_BREADCRUMBS_SECTION, breadcrumbsSupportFoldout);
                    if (showBreadcrumbsSettings)
                    {
                        var enableBreadcrumbsSupport = serializedObject.FindProperty("EnableBreadcrumbsSupport");
                        EditorGUILayout.PropertyField(
                            enableBreadcrumbsSupport,
                            new GUIContent(BacktraceConfigurationLabels.LABEL_ENABLE_BREADCRUMBS));

                        if (enableBreadcrumbsSupport.boolValue)
                        {
                            EditorGUILayout.PropertyField(
                                serializedObject.FindProperty("BacktraceBreadcrumbsLevel"),
                                new GUIContent(BacktraceConfigurationLabels.LABEL_BREADCRUMBS_EVENTS));

                            EditorGUILayout.PropertyField(
                                serializedObject.FindProperty("LogLevel"),
                                new GUIContent(BacktraceConfigurationLabels.LABEL_BREADCRUMNS_LOG_LEVEL));
                        }
                    }

#if UNITY_ANDROID || UNITY_IOS
                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("CaptureNativeCrashes"),
                        new GUIContent(BacktraceConfigurationLabels.CAPTURE_NATIVE_CRASHES));
#endif
                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("AutoSendMode"),
                        new GUIContent(BacktraceConfigurationLabels.LABEL_AUTO_SEND_MODE));

                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("CreateDatabase"),
                        new GUIContent(BacktraceConfigurationLabels.LABEL_CREATE_DATABASE_DIRECTORY));

                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("GenerateScreenshotOnException"),
                        new GUIContent(BacktraceConfigurationLabels.LABEL_GENERATE_SCREENSHOT_ON_EXCEPTION));

                    SerializedProperty maxRecordCount = serializedObject.FindProperty("MaxRecordCount");
                    EditorGUILayout.PropertyField(maxRecordCount, new GUIContent(BacktraceConfigurationLabels.LABEL_MAX_REPORT_COUNT));

                    SerializedProperty maxDatabaseSize = serializedObject.FindProperty("MaxDatabaseSize");
                    EditorGUILayout.PropertyField(maxDatabaseSize, new GUIContent(BacktraceConfigurationLabels.LABEL_MAX_DATABASE_SIZE));

                    SerializedProperty retryInterval = serializedObject.FindProperty("RetryInterval");
                    EditorGUILayout.PropertyField(retryInterval, new GUIContent(BacktraceConfigurationLabels.LABEL_RETRY_INTERVAL));

                    EditorGUILayout.LabelField("Backtrace database require at least one retry.");
                    SerializedProperty retryLimit = serializedObject.FindProperty("RetryLimit");
                    EditorGUILayout.PropertyField(retryLimit, new GUIContent(BacktraceConfigurationLabels.LABEL_RETRY_LIMIT));

                    SerializedProperty retryOrder = serializedObject.FindProperty("RetryOrder");
                    EditorGUILayout.PropertyField(retryOrder, new GUIContent(BacktraceConfigurationLabels.LABEL_RETRY_ORDER));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}