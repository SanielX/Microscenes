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
            Name = type.Name;
            Type = type;
        }

        public Type   Type { get; }
        public string Name { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class OverrideNodeIconAttribute : Attribute
    {
        public Type   Type { get; }
        public string Name { get; }
    }
}
