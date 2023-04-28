using System;

namespace Microscenes
{ 
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MicrosceneNodeAttribute : System.Attribute
    {
        public MicrosceneNodeAttribute()
        {
        }

        public MicrosceneNodeAttribute(string tooltip)
        {
            Tooltip = tooltip;
        }
        
        public string Tooltip { get; }
    }
}