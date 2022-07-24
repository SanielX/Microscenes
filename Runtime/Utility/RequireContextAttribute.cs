using System;

namespace Microscenes
{
    public class RequireContextAttribute : Attribute
    {
        public RequireContextAttribute(Type contextType)
        {
            ContextType = contextType;
        }

        public Type ContextType { get; }
    }
}
