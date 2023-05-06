using UnityEditor;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    internal static class MicrosceneGraphViewResources
    {
        // private const string STYLE_PATH                = "Packages/com.alexk.microscenes/Editor/Microscene Graph/MicrosceneGraphStyles.uss";
        // public const string CONDITION_STACK_ICON_PATH = "Packages/com.alexk.microscenes/Editor/Icons/ConditionStack.png";
        // public const string ACTION_STACK_ICON_PATH    = "Packages/com.alexk.microscenes/Editor/Icons/ActionStack.png";
        
        // public static StyleSheet LoadStyles() => AssetDatabase.LoadAssetAtPath<StyleSheet>(STYLE_PATH);
        public static StyleSheet LoadStyles() => UnityEngine.Resources.Load<StyleSheet>("MicrosceneGraphStyles");
    }
}