using System;

namespace Microscenes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class NodeIconAttribute : Attribute
    {
        public NodeIconAttribute(string name)
        {
            Name = name;
        }

        public NodeIconAttribute(Type type)
        {
            Type = type;
        }

        public Type   Type { get; }
        public string Name { get; }
    }
}
