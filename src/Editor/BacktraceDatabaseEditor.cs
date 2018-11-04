using Backtrace.Unity.Model;
using UnityEditor;

namespace Backtrace.Unity.Port.Editor
{
    [CustomEditor(typeof(BacktraceDatabase))]
    public class BacktraceDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var component = (BacktraceDatabase)target;
            component.Configuration =
                (BacktraceDatabaseConfiguration)EditorGUILayout.ObjectField(
                    "Configuration",
                    component.Configuration,
                    typeof(BacktraceDatabaseConfiguration),
                    false);
            if (component.Configuration != null)
            {
                CreateEditor(component.Configuration).OnInspectorGUI();
            }
        }
    }

}
