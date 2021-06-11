using Backtrace.Unity.Model;
using UnityEditor;
using UnityEngine;

namespace Backtrace.Unity.Editor
{
    [CustomEditor(typeof(BacktraceClient))]
    public class BacktraceClientEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (Application.isPlaying)
            {
                return;
            }
            var component = (BacktraceClient)target;
            component.Configuration =
                (BacktraceConfiguration)EditorGUILayout.ObjectField(
                    "Backtrace configuration",
                    component.Configuration,
                    typeof(BacktraceConfiguration),
                    false);
            if (component.Configuration != null)
            {
                var editor = (BacktraceConfigurationEditor)CreateEditor(component.Configuration);
                editor.OnInspectorGUI();
                if (component.Configuration.Enabled && component.gameObject.GetComponent<BacktraceDatabase>() == null)
                {
                    component.gameObject.AddComponent<BacktraceDatabase>();
                }
                else if (!component.Configuration.Enabled && component.gameObject.GetComponent<BacktraceDatabase>() != null)
                {
                    DestroyImmediate(component.gameObject.GetComponent<BacktraceDatabase>());
                }
            }
        }
    }

}
