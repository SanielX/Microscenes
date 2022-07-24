using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Microscenes.Editor
{
    public struct EditorIcon
    {
        public EditorIcon(Texture icon)
        {
            this.icon = icon;
        }

        /// <summary>
        /// <para>Creates editor icon based on filter parameters that can mean many things.</para>
        /// <br>If filter starts with "Assets/" or "Packages/", then icon is loaded as absolute path using <see cref="AssetDatabase"/>.</br>
        /// <br>If filter starts with "Resources/" then rest of the path is used for <see cref="Resources.Load(string)"/> call.</br>
        /// <br>If filter starts with t: and contains '.' symbol, icon is retrieved from type with the same name (Must match whole name including namespace)</br>
        /// <br>If filter starts with t: and <b>does not</b> contain '.', then search through types is used but only for Type.Name, first match is used to retrieve the icon. </br>
        /// If type is found, then rules are the same as for <see cref="EditorIcon.EditorIcon(Type)"/>
        /// <para>If none of these criterias are met, icon is loaded using <see cref="EditorGUIUtility.IconContent(string)"/></para>
        /// </summary>
        public EditorIcon(string filter)
        {
            icon = default;
            if (string.IsNullOrWhiteSpace(filter))
                return;

            if (filter.StartsWith("Assets/") || filter.StartsWith("Packages/"))
            {
                icon = AssetDatabase.LoadAssetAtPath<Texture>(filter);
                return;
            }

            if (filter.StartsWith("Resources/"))
            {
                string path = filter.Substring("Resources/".Length);
                icon = (Texture)Resources.Load(path);
                return;
            }

            if (filter.StartsWith("t:"))
            {
                filter = filter.Substring(2).Trim();

                if (filter.Length == 0)
                    return;

                if (filter.Contains("."))
                    icon = FromTypeFullName(filter).icon;
                else
                    icon = FromTypeName(filter).icon;

                return;
            }

            try
            {
                icon = EditorGUIUtility.IconContent(filter).image;
            }
            finally { }
        }

        /// <summary>
        /// <br>If <paramref name="type"/> is <see cref="MonoBehaviour"/> then finds <see cref="MonoScript"/> asset and retrieves icon from it.</br>
        /// <br>If <paramref name="type"/> is <see cref="ScriptableObject"/> then creates temporary instance and gets icon from it.</br><br></br>
        /// If <paramref name="type"/> is built in unity type, then <see cref="AssetPreview.GetMiniTypeThumbnail(Type)"/> is used
        /// </summary>
        public EditorIcon(Type type)
        {
            icon = GetIcon(type);
        }

        public static EditorIcon FromTypeFullName(string fullName, bool ignoreCase = false)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName, false, ignoreCase);
                if (t != null)
                    return t;
            }

            return default;
        }

        public static EditorIcon FromTypeName(string name, bool ignoreCase = false)
        {
            if (name.Contains("."))
                throw new ArgumentException("Name must not contain any separators. Use FromFullTypeName instead");

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.GetTypes())
                {
                    if (type.Name.Equals(name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                        return type;
                }
            }

            return default;
        }

        public Texture icon { get; }


        private static Dictionary<Type, Texture> iconsDictionary = new Dictionary<Type, Texture>();

        private static Texture2D indentTexture;
        public static Texture2D IndentTexture
        {
            get
            {
                if (!indentTexture)
                {
                    indentTexture = new Texture2D(1, 1);
                    indentTexture.SetPixel(0, 0, Color.clear);
                    indentTexture.Apply();
                }

                return indentTexture;
            }
        }

        static MonoScript[] allMonoScripts;

        /// <inheritdoc cref="EditorIcon.EditorIcon(Type)"/>
        public static Texture GetIcon(Type assetType)
        {
            if (iconsDictionary.TryGetValue(assetType, out var result))
                return result;

            if (typeof(ScriptableObject).IsAssignableFrom(assetType))
            {
                // Getting thumbnail for type results in unity returning some weird blank icon
                // So we have to do this
                var obj = ScriptableObject.CreateInstance(assetType);
                var preview = AssetPreview.GetMiniThumbnail(obj);
                iconsDictionary[assetType] = preview;

                UnityEngine.Object.DestroyImmediate(obj);

                return preview;
            }

            var thumbnail = AssetPreview.GetMiniTypeThumbnail(assetType);

            if (!thumbnail)
            {
                allMonoScripts ??= MonoImporter.GetAllRuntimeMonoScripts();

                foreach (var monoScript in allMonoScripts)
                {
                    if (monoScript.GetClass() == assetType)
                    {
                        thumbnail = AssetPreview.GetMiniThumbnail(monoScript);
                        break;
                    }
                }
            }

            return thumbnail;
        }

        public static implicit operator EditorIcon(Texture tex) => new EditorIcon(tex);

        /// <inheritdoc cref="EditorIcon.EditorIcon(string)"/>
        public static implicit operator EditorIcon(string name) => new EditorIcon(name);

        /// <inheritdoc cref="EditorIcon.EditorIcon(Type)"/>
        public static implicit operator EditorIcon(Type type) => new EditorIcon(type);
        public static implicit operator Texture(EditorIcon i) => i.icon;
    }
}
