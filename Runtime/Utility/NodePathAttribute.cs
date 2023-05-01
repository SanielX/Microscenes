using System;

namespace Microscenes
{
    public static class NodeFolder
    {
        public const string Abstract     = "Abstract/";
        public const string Interactions = "Interactions/";
        public const string Other        = "Other/";
    }

    public class NodePathAttribute : Attribute
    {
        public NodePathAttribute(string path)
        {
            Path = path;
        }
 
        public string Path { get; }
    }
}
