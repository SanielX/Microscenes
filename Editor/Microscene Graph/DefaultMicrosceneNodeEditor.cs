using UnityEditor;

namespace Microscenes.Editor
{
    [CustomEditor(typeof(MicrosceneNode), editorForChildClasses: true)]
    class DefaultMicrosceneNodeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var prop = serializedObject.GetIterator();
            prop.NextVisible(true);
            
            do
            {
                if(prop.propertyPath == "m_Script")
                    continue;
                
                EditorGUILayout.PropertyField(prop, includeChildren: true);
            } 
            while(prop.NextVisible(false));
        }
    }
}