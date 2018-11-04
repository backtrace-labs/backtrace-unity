using System.IO;
using UnityEditor;
using UnityEngine;
using Backtrace.Unity.Model;

namespace Backtrace.Unity.Port.Editor
{
    [CustomEditor(typeof(BacktraceClientConfiguration))]
    public class BacktraceClientConfigurationEditor : UnityEditor.Editor
    {
        public const string LABEL_SERVER_URL = "Server Address";
        public const string LABEL_TOKEN = "Token";
        public const string LABEL_REPORT_PER_MIN = "Reports per minute";

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
            settings.Token = EditorGUILayout.TextField(LABEL_TOKEN, settings.Token);
            if (!settings.ValidateToken())
            {
                EditorGUILayout.HelpBox("Token require at least 64 characters!", MessageType.Warning);
            }
            settings.ReportPerMin = EditorGUILayout.IntField(LABEL_REPORT_PER_MIN, settings.ReportPerMin);
        }
    }

}