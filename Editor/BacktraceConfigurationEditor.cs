using Backtrace.Unity.Model;
using Backtrace.Unity.Model.Breadcrumbs;
using Backtrace.Unity.Types;
using System;
using UnityEditor;
using UnityEngine;
using Backtrace.Unity.Extensions;

namespace Backtrace.Unity.Editor
{
    [CustomEditor(typeof(BacktraceConfiguration))]
    public class BacktraceConfigurationEditor : UnityEditor.Editor
    {
        protected static bool showBreadcrumbsSettings = false;
        protected static bool showMetricsSettings = false;
        protected static bool showClientAdvancedSettings = false;
        protected static bool showDatabaseSettings = false;
        protected static bool showNativeCrashesSettings = false;
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

            DrawIntegerTextboxWithDefault("ReportPerMin", BacktraceConfigurationLabels.LABEL_REPORT_PER_MIN, 0, BacktraceConfiguration.DefaultReportPerMin, serializedObject);

            GUIStyle clientAdvancedSettingsFoldout = new GUIStyle(EditorStyles.foldout);
            showClientAdvancedSettings = EditorGUILayout.Foldout(showClientAdvancedSettings, "Client advanced settings", clientAdvancedSettingsFoldout);
            if (showClientAdvancedSettings)
            {

#if UNITY_2018_4_OR_NEWER

                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("IgnoreSslValidation"),
                    new GUIContent(BacktraceConfigurationLabels.LABEL_IGNORE_SSL_VALIDATION));
#endif
                EditorGUILayout.PropertyField(
                   serializedObject.FindProperty("UseNormalizedExceptionMessage"),
                   new GUIContent(BacktraceConfigurationLabels.LABEL_USE_NORMALIZED_EXCEPTION_MESSAGE));
#if UNITY_STANDALONE_WIN
                EditorGUILayout.PropertyField(
                 serializedObject.FindProperty("SendUnhandledGameCrashesOnGameStartup"),
                 new GUIContent(BacktraceConfigurationLabels.LABEL_SEND_UNHANDLED_GAME_CRASHES_ON_STARTUP));
#endif
                var reportFilterType = (ReportFilterType)ConvertPropertyToEnum("ReportFilterType", serializedObject);
                if (reportFilterType.HasAllFlags())
                {
                    EditorGUILayout.HelpBox("You've selected to filter out Everything, which means no reports will be submitted to Backtrace.", MessageType.Error);
                }

                DrawMultiselectDropdown("ReportFilterType", reportFilterType, BacktraceConfigurationLabels.LABEL_REPORT_FILTER, serializedObject);
                DrawIntegerTextboxWithDefault("NumberOfLogs", BacktraceConfigurationLabels.LABEL_NUMBER_OF_LOGS, 0, BacktraceConfiguration.DefaultNumberOfLogs, serializedObject);

                EditorGUILayout.PropertyField(
                 serializedObject.FindProperty("PerformanceStatistics"),
                 new GUIContent(BacktraceConfigurationLabels.LABEL_PERFORMANCE_STATISTICS));

                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("DestroyOnLoad"),
                    new GUIContent(BacktraceConfigurationLabels.LABEL_DESTROY_CLIENT_ON_SCENE_LOAD));

                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("Sampling"),
                    new GUIContent(BacktraceConfigurationLabels.LABEL_SAMPLING));

                DrawIntegerTextboxWithDefault("GameObjectDepth", BacktraceConfigurationLabels.LABEL_GAME_OBJECT_DEPTH, -1, BacktraceConfiguration.DefaultGameObjectDepth, serializedObject);

                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("DisableInEditor"),
                    new GUIContent(BacktraceConfigurationLabels.DISABLE_IN_EDITOR));
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
                    DrawMultiselectDropdown("DeduplicationStrategy", BacktraceConfigurationLabels.LABEL_DEDUPLICATION_RULES, serializedObject);

                    GUIStyle showNativeCrashesSupportFoldout = new GUIStyle(EditorStyles.foldout);
                    showNativeCrashesSettings = EditorGUILayout.Foldout(showNativeCrashesSettings, BacktraceConfigurationLabels.LABEL_NATIVE_CRASHES, showNativeCrashesSupportFoldout);
                    if (showNativeCrashesSettings)
                    {
#if UNITY_STANDALONE_WIN
                        DrawMultiselectDropdown("MinidumpType", BacktraceConfigurationLabels.LABEL_MINIDUMP_SUPPORT, serializedObject);
#endif


#if UNITY_ANDROID || UNITY_IOS || UNITY_STANDALONE_WIN || UNITY_GAMECORE_XBOXSERIES
                        SerializedProperty captureNativeCrashes = serializedObject.FindProperty("CaptureNativeCrashes");
                        EditorGUILayout.PropertyField(
                            captureNativeCrashes,
                            new GUIContent(BacktraceConfigurationLabels.CAPTURE_NATIVE_CRASHES));
#if !UNITY_2019_1_OR_NEWER
                        if (captureNativeCrashes.boolValue)
                        {
                            EditorGUILayout.HelpBox("You're using Backtrace-Unity integration with Unity 16b NDK support. Please contact Backtrace support for any additional help", MessageType.Warning);
                        }
#endif
#if !UNITY_GAMECORE_XBOXSERIES
                        EditorGUILayout.PropertyField(
                            serializedObject.FindProperty("HandleANR"),
                             new GUIContent(BacktraceConfigurationLabels.LABEL_HANDLE_ANR));
#endif
#if UNITY_ANDROID || UNITY_IOS
                        EditorGUILayout.PropertyField(
                            serializedObject.FindProperty("OomReports"),
                             new GUIContent(BacktraceConfigurationLabels.LABEL_HANDLE_OOM));

#if UNITY_2019_2_OR_NEWER
                        EditorGUILayout.PropertyField(
                            serializedObject.FindProperty("ClientSideUnwinding"),
                            new GUIContent(BacktraceConfigurationLabels.LABEL_ENABLE_CLIENT_SIDE_UNWINDING));
#endif

#endif

#if UNITY_2019_2_OR_NEWER && UNITY_ANDROID

                        EditorGUILayout.PropertyField(
                           serializedObject.FindProperty("SymbolsUploadToken"),
                           new GUIContent(BacktraceConfigurationLabels.LABEL_SYMBOLS_UPLOAD_TOKEN));
#endif
#endif
                    }
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
                            DrawMultiselectDropdown("BacktraceBreadcrumbsLevel", BacktraceConfigurationLabels.LABEL_BREADCRUMBS_EVENTS, serializedObject);
                            DrawMultiselectDropdown("LogLevel", BacktraceConfigurationLabels.LABEL_BREADCRUMNS_LOG_LEVEL, serializedObject);
                        }
                    }

#if UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
                    EditorGUILayout.PropertyField(
                        serializedObject.FindProperty("AddUnityLogToReport"),
                        new GUIContent(BacktraceConfigurationLabels.LABEL_ADD_UNITY_LOG));
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

                    DrawIntegerTextboxWithDefault("MaxRecordCount", BacktraceConfigurationLabels.LABEL_MAX_REPORT_COUNT, 1, BacktraceConfiguration.DefaultMaxRecordCount, serializedObject);
                    DrawIntegerTextboxWithDefault("MaxDatabaseSize", BacktraceConfigurationLabels.LABEL_MAX_DATABASE_SIZE, 0, BacktraceConfiguration.DefaultMaxDatabaseSize, serializedObject);
                    DrawIntegerTextboxWithDefault("RetryInterval", BacktraceConfigurationLabels.LABEL_RETRY_INTERVAL, 1, BacktraceConfiguration.DefaultRetryInterval, serializedObject);

                    EditorGUILayout.LabelField("Backtrace database require at least one retry.");
                    DrawIntegerTextboxWithDefault("RetryLimit", BacktraceConfigurationLabels.LABEL_RETRY_LIMIT, 0, BacktraceConfiguration.DefaultRetryLimit, serializedObject);

                    SerializedProperty retryOrder = serializedObject.FindProperty("RetryOrder");
                    EditorGUILayout.PropertyField(retryOrder, new GUIContent(BacktraceConfigurationLabels.LABEL_RETRY_ORDER));
                }
            }

            serializedObject.ApplyModifiedProperties();
        }


        /// <summary>
        /// Draw the textbox control dedicated to unsigned integers and apply default if user passes negative value 
        /// </summary>
        /// <param name="propertyName">Backtrace configuration property name</param>
        /// <param name="label">Property label</param>
        /// <param name="defaultValue">Default value</param>
        /// <param name="serializedObject">Configuration object</param>
        private static void DrawIntegerTextboxWithDefault(string propertyName, string label, int minimumValue, int defaultValue, SerializedObject serializedObject)
        {
            var property = serializedObject.FindProperty(propertyName);
            EditorGUILayout.PropertyField(property, new GUIContent(label));
            if (property.intValue < minimumValue)
            {
                property.intValue = defaultValue;
            }
        }

        /// <summary>
        /// Draw multiselect dropdown. By default PropertyField won't work correctly in Unity 2017/2018
        /// if editor has to display multiselect dropdown by using enum flags. This code allows to generate
        /// multiselect dropdown based on the available enum.
        /// </summary>
        /// <param name="propertyName">Enum property name</param>
        /// <param name="label">Label</param>
        /// <param name="serializedObject">Serialized object</param>
        private static void DrawMultiselectDropdown(string propertyName, string label, SerializedObject serializedObject)
        {
            var @enum = ConvertPropertyToEnum(propertyName, serializedObject);
            DrawMultiselectDropdown(propertyName, @enum, label, serializedObject);
        }

        private static void DrawMultiselectDropdown(string propertyName, Enum enumValue, string label, SerializedObject serializedObject)
        {
            EditorGUI.BeginChangeCheck();

            var value = EditorGUILayout.EnumFlagsField(label, enumValue);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.FindProperty(propertyName).longValue = Convert.ToInt64(value);
            }
        }

        /// <summary>
        /// Convert UI serialized property to enum
        /// </summary>
        /// <param name="propertyName">Enum property name</param>
        /// <param name="serializedObject">UI serialized object</param>
        /// <returns>Enum </returns>
        private static Enum ConvertPropertyToEnum(string propertyName, SerializedObject serializedObject)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

            var property = serializedObject.FindProperty(propertyName);
            var targetObject = property.serializedObject.targetObject;
            var enumValue = (Enum)targetObject.GetType().GetField(propertyName, flags).GetValue(targetObject);
            return enumValue;
        }
    }
}
