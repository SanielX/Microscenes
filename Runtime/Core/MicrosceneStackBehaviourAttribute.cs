using System;

namespace Microscenes
{
    public enum MicrosceneStackConnectionType
    {
        SingleOutput,
        MultipleOutput,
    }
    
    /// <summary>
    /// Use this attribute to make your class browsable from graph view
    /// </summary>
    public class MicrosceneStackBehaviourAttribute : Attribute
    {
        public MicrosceneStackBehaviourAttribute(MicrosceneStackConnectionType type, string tooltip = null)
        {
            Type    = type;
            Tooltip = tooltip;
        }
        
        public MicrosceneStackConnectionType Type { get; }
        public string Tooltip { get; }
    }
}