﻿using Backtrace.Unity.Model;
using UnityEditor;
using UnityEngine;

namespace Backtrace.Unity.Editor
{
    [CustomEditor(typeof(BacktraceConfiguration))]
    public class BacktraceConfigurationEditor : UnityEditor.Editor
    {
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

                EditorGUILayout.PropertyField(
                   serializedObject.FindProperty("UseNormalizedExceptionMessage"),
                   new GUIContent(BacktraceConfigurationLabels.LABEL_USE_NORMALIZED_EXCEPTION_MESSAGE));

                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty("DestroyOnLoad"),
                    new GUIContent(BacktraceConfigurationLabels.LABEL_DESTROY_CLIENT_ON_SCENE_LOAD));


                SerializedProperty gameObjectDepth = serializedObject.FindProperty("GameObjectDepth");
                EditorGUILayout.PropertyField(gameObjectDepth, new GUIContent(BacktraceConfigurationLabels.LABEL_GAME_OBJECT_DEPTH));

                if (gameObjectDepth.intValue < -1)
                {
                    EditorGUILayout.HelpBox("Please insert value greater or equal -1", MessageType.Error);
                }
            }
            SerializedProperty enabled = serializedObject.FindProperty("Enabled");
            EditorGUILayout.PropertyField(enabled, new GUIContent(BacktraceConfigurationLabels.LABEL_ENABLE_DATABASE));

            if (enabled.boolValue)
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
                    EditorGUILayout.HelpBox("Minidump support works only on Windows machines.", MessageType.Warning);
                    SerializedProperty miniDumpType = serializedObject.FindProperty("MinidumpType");
                    EditorGUILayout.PropertyField(miniDumpType, new GUIContent(BacktraceConfigurationLabels.LABEL_MINIDUMP_SUPPORT));
#endif

                    SerializedProperty autoSendMode = serializedObject.FindProperty("AutoSendMode");
                    EditorGUILayout.PropertyField(autoSendMode, new GUIContent(BacktraceConfigurationLabels.LABEL_AUTO_SEND_MODE));


                    SerializedProperty createDatabase = serializedObject.FindProperty("CreateDatabase");
                    EditorGUILayout.PropertyField(createDatabase, new GUIContent(BacktraceConfigurationLabels.LABEL_CREATE_DATABASE_DIRECTORY));

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