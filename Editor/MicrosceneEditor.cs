using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    [CustomEditor(typeof(Microscene))]
    public class MicrosceneEditor : UnityEditor.Editor
    {
        private SerializedProperty notesProp, skipProp;
        private GUIStyle notesStyle;

        private void OnEnable()
        {
            try
            {
                if (!serializedObject.targetObject)
                    return;

                notesProp = serializedObject.FindProperty("m_Notes");
                skipProp  = serializedObject.FindProperty("m_Skip");
            }
            finally {}
        }

        public override void OnInspectorGUI()
        {
            if (!this.target)
                return;
            
            // When creating editor after domain reload styles are initialized after OnEnabled is called so we do this thing
            // Also creating it once may still not work! Fucking unity
            // if (notesStyle is null)
            {
                notesStyle = new GUIStyle(EditorStyles.textField);
                notesStyle.wordWrap = true;
            }
            
            serializedObject.Update();
            EditorGUILayout.PropertyField(skipProp);
            
            notesProp.isExpanded = EditorGUILayout.Foldout(notesProp.isExpanded, "Notes", toggleOnLabelClick: true);
            if(notesProp.isExpanded)
            {
                notesProp.stringValue = EditorGUILayout.TextArea(notesProp.stringValue, notesStyle);
            }
            
            serializedObject.ApplyModifiedProperties();
            
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
