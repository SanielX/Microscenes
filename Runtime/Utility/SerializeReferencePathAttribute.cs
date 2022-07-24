using System;

namespace Microscenes
{
    public enum SRPathType
    {
        Positional,
        Interactions,
        Abstract,
        Customs
    }

    public class SerializeReferencePathAttribute : Attribute
    {
        public SerializeReferencePathAttribute(string path)
        {
            Path = path;
        }
        public SerializeReferencePathAttribute(SRPathType type, string path)
        {
            path = type.ToString() + '/' + path;

            Path = path;
        }

        public string Path { get; }
    }
}
