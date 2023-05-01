using UnityEngine;

namespace Microscenes.Utility
{
    /// <summary>
    /// Helpers for implementing <see cref="INameableNode"/>
    /// </summary>
    public static class NodeNaming
    {
        private static readonly string[] vowels = new[]
        {
            "a", "e", "o", "u", "y", "i",
        };

        /// <summary>
        /// Example usage: Value("meter", 10.43243f) => "10.43 meters"
        /// </summary>
        public static string Value(string units, float value)
        {
            return $"{System.Math.Round(value, 2)} {Plural(value, units)}";
        }
        
        public static string Plural(float n, string text)
        {
            if(string.IsNullOrWhiteSpace(text))
                return text;
            
            if(n == 1.0f)
                return text;
            
            if(text.EndsWith("s"))
                return text + "es";
            
            return text + "s";
        }
        
        /// <summary>
        /// Wraps text in rich text 'color' tag
        /// </summary>
        public static string Colored(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{text}</color>";
        }
        
        /// <inheritdoc cref="Colored(string,UnityEngine.Color)"/>
        public static string Colored(string text, int hexColor)
        {
            return $"<color=#{hexColor:X}>{text}</color>";
        }
        
        /// <summary>
        /// Formats text using unity's formatter for variables, so it works only at edit time
        /// </summary>
        /// <remarks>Check UnityEditor.ObjectNames.NicifyVariableName for more info</remarks>
        /// <returns> "_someName" => "Some Name" </returns>
        public static string Nicify(string text)
        {
#if UNITY_EDITOR
            return UnityEditor.ObjectNames.NicifyVariableName(text);
#else
            return text;
#endif 
        }
    }
}