using Backtrace.Unity.Model;
using UnityEditor;

namespace Backtrace.Unity.Port.Editor
{
    [CustomEditor(typeof(BacktraceClient))]
    public class BacktraceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var component = (BacktraceClient)target;
            component.Configuration =
                (BacktraceClientConfiguration)EditorGUILayout.ObjectField(
                    "Backtrace configuration",
                    component.Configuration,
                    typeof(BacktraceClientConfiguration),
                    false);
            if (component.Configuration != null)
            {
                CreateEditor(component.Configuration).OnInspectorGUI();
            }
        }
    }

}
