using System.IO;
using UnityEditor;
using UnityEngine;
using Backtrace.Unity.Model;
using Backtrace.Unity.Types;

namespace Backtrace.Unity.Port.Editor
{
    [CustomEditor(typeof(BacktraceDatabaseConfiguration))]
    public class BacktraceDatabaseConfigurationEditor : BacktraceClientConfigurationEditor
    {
        public const string LABEL_PATH = "Backtrace database path";
        public const string LABEL_AUTO_SEND_MODE = "Automatically send";
        public const string LABEL_CREATE_DATABASE_DIRECTORY = "Create database directory";
        public const string LABEL_MAX_REPORT_COUNT = "Maximum number of records";
        public const string LABEL_MAX_DATABASE_SIZE = "Maximum database size (mb)";
        public const string LABEL_RETRY_INTERVAL = "Retry interval";
        public const string LABEL_RETRY_LIMIT = "Maximum retries";
        public const string LABEL_RETRY_ORDER = "Retry order (Stack/Queue)";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var settings = (BacktraceDatabaseConfiguration)target;
            
            EditorGUILayout.LabelField("Backtrace Database settings.");
            EditorGUILayout.LabelField("If path doesn't exist or is empty, database will be disabled");
            settings.DatabasePath = EditorGUILayout.TextField(LABEL_PATH, settings.DatabasePath);
            if (!settings.ValidDatabasePath())
            {
                EditorGUILayout.HelpBox("Please insert valid Backtrace database path!", MessageType.Error);
            }
            settings.AutoSendMode = EditorGUILayout.Toggle(LABEL_AUTO_SEND_MODE, settings.AutoSendMode);
            settings.CreateDatabase = EditorGUILayout.Toggle(LABEL_CREATE_DATABASE_DIRECTORY, settings.CreateDatabase);
            settings.MaxRecordCount = EditorGUILayout.IntField(LABEL_MAX_REPORT_COUNT, settings.MaxRecordCount);
            if(settings.MaxRecordCount< 0)
            {
                settings.MaxRecordCount = 0;
            }
            settings.MaxDatabaseSize = EditorGUILayout.LongField(LABEL_MAX_DATABASE_SIZE, settings.MaxDatabaseSize);
            if(settings.MaxDatabaseSize < 0)
            {
                settings.MaxDatabaseSize = 0;
            }
           
                    
            settings.RetryInterval = EditorGUILayout.IntField(LABEL_RETRY_INTERVAL, settings.RetryInterval);
            EditorGUILayout.LabelField("Backtrace database require at least one retry.");
            settings.RetryLimit = EditorGUILayout.IntField(LABEL_RETRY_LIMIT, settings.RetryLimit);
            if (settings.RetryLimit < 0)
            {
                settings.RetryLimit = 1;
            }
            settings.RetryOrder = (RetryOrder)EditorGUILayout.EnumPopup(LABEL_RETRY_ORDER, settings.RetryOrder);
        }
    }

}