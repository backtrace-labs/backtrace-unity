using UnityEditor;
using Backtrace.Unity.Model;
using Backtrace.Unity.Types;

namespace Backtrace.Unity.Editor
{
    [CustomEditor(typeof(BacktraceDatabaseConfiguration))]
    public class BacktraceDatabaseConfigurationEditor : BacktraceClientConfigurationEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var settings = (BacktraceDatabaseConfiguration)target;

            EditorGUILayout.LabelField("Backtrace Database settings.");
            EditorGUILayout.LabelField("If path doesn't exist or is empty, database will be disabled");
            settings.DatabasePath = EditorGUILayout.TextField(BacktraceConfigurationLabels.LABEL_PATH, settings.DatabasePath);
            if (!settings.ValidDatabasePath())
            {
                EditorGUILayout.HelpBox("Please insert valid Backtrace database path!", MessageType.Error);
            }

#if UNITY_STANDALONE_WIN
            settings.MinidumpType = (MiniDumpType)EditorGUILayout.EnumFlagsField(BacktraceConfigurationLabels.LABEL_MINIDUMP_SUPPORT, settings.MinidumpType);
#else
            settings.MinidumpType = MiniDumpType.None;

#endif

            settings.DeduplicationStrategy = (DeduplicationStrategy)EditorGUILayout.EnumFlagsField(BacktraceConfigurationLabels.LABEL_DEDUPLICATION_RULES, settings.DeduplicationStrategy);
            settings.GenerateScreenshotOnException = EditorGUILayout.Toggle(
                BacktraceConfigurationLabels.LABEL_GENERATE_SCREENSHOT_ON_EXCEPTION,
                settings.GenerateScreenshotOnException);

            settings.AutoSendMode = EditorGUILayout.Toggle(BacktraceConfigurationLabels.LABEL_AUTO_SEND_MODE, settings.AutoSendMode);
            settings.CreateDatabase = EditorGUILayout.Toggle(BacktraceConfigurationLabels.LABEL_CREATE_DATABASE_DIRECTORY, settings.CreateDatabase);
            settings.MaxRecordCount = EditorGUILayout.IntField(BacktraceConfigurationLabels.LABEL_MAX_REPORT_COUNT, settings.MaxRecordCount);
            if (settings.MaxRecordCount < 0)
            {
                settings.MaxRecordCount = BacktraceConfiguration.DefaultMaxRecordCount;
            }
            settings.MaxDatabaseSize = EditorGUILayout.LongField(BacktraceConfigurationLabels.LABEL_MAX_DATABASE_SIZE, settings.MaxDatabaseSize);
            if (settings.MaxDatabaseSize < 0)
            {
                settings.MaxDatabaseSize = BacktraceConfiguration.DefaultMaxDatabaseSize;
            }


            settings.RetryInterval = EditorGUILayout.IntField(BacktraceConfigurationLabels.LABEL_RETRY_INTERVAL, settings.RetryInterval);
            EditorGUILayout.LabelField("Backtrace database require at least one retry.");
            settings.RetryLimit = EditorGUILayout.IntField(BacktraceConfigurationLabels.LABEL_RETRY_LIMIT, settings.RetryLimit);
            if (settings.RetryLimit < 0)
            {
                settings.RetryLimit = BacktraceConfiguration.DefaultRetryLimit;
            }
            settings.RetryOrder = (RetryOrder)EditorGUILayout.EnumPopup(BacktraceConfigurationLabels.LABEL_RETRY_ORDER, settings.RetryOrder);
        }
    }

}