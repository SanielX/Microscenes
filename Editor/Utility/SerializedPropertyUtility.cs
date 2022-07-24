using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Microscenes.Editor
{
    public static class SerializedPropertyUtility
    {
        // https://forum.unity.com/threads/loop-through-serializedproperty-children.435119/
        /// <summary>
        /// Gets visible children of `SerializedProperty` at 1 level depth.
        /// </summary>
        /// <param name="serializedProperty">Parent `SerializedProperty`.</param>
        /// <returns>Collection of `SerializedProperty` children.</returns>
        public static IEnumerable<SerializedProperty> GetVisibleChildren(this SerializedProperty serializedProperty)
        {
            SerializedProperty currentProperty = serializedProperty.Copy();
            SerializedProperty nextSiblingProperty = serializedProperty.Copy();
            {
                nextSiblingProperty.NextVisible(false);
            }

            if (currentProperty.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                        break;

                    yield return currentProperty.Copy();
                }
                while (currentProperty.NextVisible(false));
            }
        }

        public static T GetValue<T>(this SerializedProperty prop)
        {
            return GetObjectFromProperty<T>(prop, out _);
        }

        // Source: https://github.com/lordofduct/spacepuppy-unity-framework/blob/master/SpacepuppyBaseEditor/EditorHelper.cs
        public static T GetObjectFromProperty<T>(SerializedProperty prop, out FieldInfo fieldInfo)
        {
            fieldInfo = null;
            if (prop == null) return default(T);

            var path = prop.propertyPath.Replace(".Array.data[", "[");
            return GetObjectFromPath<T>(prop.serializedObject.targetObject, path, out fieldInfo);
        }

        public static T GetObjectFromPath<T>(UnityEngine.Object targetObject, string path, out FieldInfo fieldInfo)
        {
            fieldInfo = null;
            object obj = targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index, out fieldInfo);
                }
                else
                {
                    obj = GetValue_Imp(obj, element, out fieldInfo);
                }
            }
            Type t = typeof(T);

            return (T)obj;
        }

        private static object GetValue_Imp(object source, string name, out FieldInfo fieldInfo)
        {
            fieldInfo = null;
            if (source == null)
                return null;
            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                {
                    fieldInfo = f;
                    return f.GetValue(source);
                }
                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                {
                    return p.GetValue(source, null);
                }

                type = type.BaseType;
            }
            return null;
        }
        private static object GetValue_Imp(object source, string name, int index, out FieldInfo info)
        {
            var enumerable = GetValue_Imp(source, name, out info) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            //while (index-- >= 0)
            //    enm.MoveNext();
            //return enm.Current;

            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) return null;
            }
            return enm.Current;
        }
    }
}