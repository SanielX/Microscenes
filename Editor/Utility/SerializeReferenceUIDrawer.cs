using UnityEditor;
using UnityEngine;

namespace Microscenes.Editor
{
    internal class SerializeReferenceUIDrawer
    {
        public static float CalculateSPropertyHeight(SerializedProperty prop)
        {
            float calcHeight  = 0;

            var type = prop.GetValue<object>().GetType();

            calcHeight = defaultCalculate(prop, calcHeight);

            return calcHeight;

            static float defaultCalculate(SerializedProperty serRefProp, float calcHeight)
            {
                foreach (var propChild in serRefProp.GetVisibleChildren())
                {
                    var propHeight = EditorGUI.GetPropertyHeight(propChild);
                    calcHeight += propHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                return calcHeight;
            }
        }

        public static void DrawSPropertyGUI(Rect rect, SerializedProperty prop)
        {
            var type = prop.GetValue<object>().GetType();

            defaultDraw(rect, prop);

            static void defaultDraw(Rect rect, SerializedProperty prop)
            {
                foreach (var propChild in prop.GetVisibleChildren())
                {
                    var propHeight = EditorGUI.GetPropertyHeight(propChild);
                    rect.height = propHeight;

                    EditorGUI.PropertyField(rect, propChild, true);

                    rect.y += propHeight + EditorGUIUtility.standardVerticalSpacing;
                }
            }
        }
    }
}
