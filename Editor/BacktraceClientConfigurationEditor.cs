using Backtrace.Unity.Model;
using Backtrace.Unity.Types;
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

#if UNITY_2018_4_OR_NEWER
        public const string LABEL_IGNORE_SSL_VALIDATION = "Ignore SSL validation";
#endif
#if UNITY_ANDROID
        public const string LABEL_HANDLE_ANR = "Handle ANR (Application not responding)";
#endif
        public const string LABEL_MINIDUMP_SUPPORT = "Minidump type";
        public const string LABEL_DEDUPLICATION_RULES = "Deduplication rules";
        public const string LABEL_GAME_OBJECT_DEPTH = "Game object depth limit";

        public const string LABEL_DESTROY_CLIENT_ON_SCENE_LOAD = "Destroy client on new scene load (false - Backtrace managed)";


        public override void OnInspectorGUI()
        {
            var settings = (BacktraceClientConfiguration)target;

            settings.ServerUrl = EditorGUILayout.TextField(LABEL_SERVER_URL, settings.ServerUrl);
            settings.UpdateServerUrl();
            if (!settings.ValidateServerUrl())
            {
                EditorGUILayout.HelpBox("Detected different pattern of url. Please make sure you passed valid Backtrace url", MessageType.Warning);
            }

            settings.DestroyOnLoad = EditorGUILayout.Toggle(LABEL_DESTROY_CLIENT_ON_SCENE_LOAD, settings.DestroyOnLoad);
            settings.ReportPerMin = EditorGUILayout.IntField(LABEL_REPORT_PER_MIN, settings.ReportPerMin);
            settings.HandleUnhandledExceptions = EditorGUILayout.Toggle(LABEL_HANDLE_UNHANDLED_EXCEPTION, settings.HandleUnhandledExceptions);

#if UNITY_2018_4_OR_NEWER
            settings.IgnoreSslValidation = EditorGUILayout.Toggle(LABEL_IGNORE_SSL_VALIDATION, settings.IgnoreSslValidation);
#else
            settings.IgnoreSslValidation = false;
#endif
            settings.DeduplicationStrategy = (DeduplicationStrategy)EditorGUILayout.EnumPopup(LABEL_DEDUPLICATION_RULES, settings.DeduplicationStrategy);
#if UNITY_ANDROID
            settings.HandleANR = EditorGUILayout.Toggle(LABEL_HANDLE_ANR, settings.HandleANR);
#endif
            settings.GameObjectDepth = EditorGUILayout.IntField(LABEL_GAME_OBJECT_DEPTH, settings.GameObjectDepth);
        }
    }

}
