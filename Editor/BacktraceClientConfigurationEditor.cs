using Backtrace.Unity.Model;
using UnityEditor;

namespace Backtrace.Unity.Editor
{
    [CustomEditor(typeof(BacktraceClientConfiguration))]
    public class BacktraceClientConfigurationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var settings = (BacktraceClientConfiguration)target;

            settings.ServerUrl = EditorGUILayout.TextField(BacktraceConfigurationLabels.LABEL_SERVER_URL, settings.ServerUrl);
            settings.UpdateServerUrl();
            if (!settings.ValidateServerUrl())
            {
                EditorGUILayout.HelpBox("Detected different pattern of url. Please make sure you passed valid Backtrace url", MessageType.Warning);
            }

            settings.DestroyOnLoad = EditorGUILayout.Toggle(BacktraceConfigurationLabels.LABEL_DESTROY_CLIENT_ON_SCENE_LOAD, settings.DestroyOnLoad);
            settings.ReportPerMin = EditorGUILayout.IntField(BacktraceConfigurationLabels.LABEL_REPORT_PER_MIN, settings.ReportPerMin);
            settings.HandleUnhandledExceptions = EditorGUILayout.Toggle(BacktraceConfigurationLabels.LABEL_HANDLE_UNHANDLED_EXCEPTION, settings.HandleUnhandledExceptions);

#if UNITY_2018_4_OR_NEWER
            settings.IgnoreSslValidation = EditorGUILayout.Toggle(BacktraceConfigurationLabels.LABEL_IGNORE_SSL_VALIDATION, settings.IgnoreSslValidation);
#else
            settings.IgnoreSslValidation = false;
#endif
#if UNITY_ANDROID || UNITY_IOS
            settings.HandleANR = EditorGUILayout.Toggle(BacktraceConfigurationLabels.LABEL_HANDLE_ANR, settings.HandleANR);
            settings.OomReports = EditorGUILayout.Toggle(BacktraceConfigurationLabels.LABEL_HANDLE_OOM, settings.OomReports);
#endif
            settings.GameObjectDepth = EditorGUILayout.IntField(BacktraceConfigurationLabels.LABEL_GAME_OBJECT_DEPTH, settings.GameObjectDepth);
        }
    }
}