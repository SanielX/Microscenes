namespace Microscenes
{
    /// <summary>
    /// Helpers for implementing <see cref="INameableNode"/>
    /// </summary>
    public static class NodeNamingUtility
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
    }
}