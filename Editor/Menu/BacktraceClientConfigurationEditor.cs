#if UNITY_EDITOR
using Backtrace.Unity.Model;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Backtrace.Unity.Editor
{
    [CustomEditor(typeof(BacktraceClientConfiguration))]
    public class BacktraceClientConfigurationEditor : UnityEditor.Editor
    {
        public const string LABEL_SERVER_URL = "Server Address";
        public const string LABEL_REPORT_PER_MIN = "Reports per minute";
        public const string LABEL_HANDLE_UNHANDLED_EXCEPTION = "Handle unhandled exceptions";
        public const string LABEL_IGNORE_SSL_VALIDATION = "Ignore SSL validation";


        private const string CONFIG_NAME = "backtrace_client_config";

        public override void OnInspectorGUI()
        {
            var settings = (BacktraceClientConfiguration)target;

            settings.ServerUrl = EditorGUILayout.TextField(LABEL_SERVER_URL, settings.ServerUrl);
            settings.UpdateServerUrl();
            if (!settings.ValidateServerUrl())
            {
                EditorGUILayout.HelpBox("Please insert valid Backtrace server url!", MessageType.Error);
            }
            settings.ReportPerMin = EditorGUILayout.IntField(LABEL_REPORT_PER_MIN, settings.ReportPerMin);
            settings.HandleUnhandledExceptions = EditorGUILayout.Toggle(LABEL_HANDLE_UNHANDLED_EXCEPTION, settings.HandleUnhandledExceptions);
            settings.IgnoreSslValidation = EditorGUILayout.Toggle(LABEL_IGNORE_SSL_VALIDATION, settings.IgnoreSslValidation);
        }
    }

}

#endif