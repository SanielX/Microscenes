using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    [CustomEditor(typeof(Microscene))]
    public class MicrosceneEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (!this.target)
                return;

            var target = (Microscene)this.target;
            if (target.TryGetComponent<IMicrosceneContextProvider>(out var provider))
            {
                EditorGUILayout.LabelField($"Microscene execution is controlled by {ObjectNames.NicifyVariableName(provider.GetType().Name)} component",
                                           EditorStyles.wordWrappedLabel);
            }

            if (GUILayout.Button("Open graph"))
            {
                var window = EditorWindow.CreateWindow<MicrosceneGraphWindow>();
                window.SetMicroscene(target as Microscene, false);
            }
        }
    }
}
