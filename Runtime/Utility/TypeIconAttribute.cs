using System;

namespace Microscenes
{
    public class TypeIconAttribute : Attribute
    {
        public TypeIconAttribute(string name)
        {
            Name = name;
        }

        public TypeIconAttribute(Type type)
        {
            Name = type.Name;
            Type = type;
        }

        public Type   Type { get; }
        public string Name { get; }
    }
}
