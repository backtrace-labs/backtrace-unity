using Backtrace.Unity.Model;
using UnityEditor;

namespace Backtrace.Unity.Port.Editor
{
    [CustomEditor(typeof(BacktraceDatabase))]
    public class BacktraceDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("You can configure the database in the BacktraceClient Component");
        }
    }

}
