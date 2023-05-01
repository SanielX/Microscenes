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
        public MicrosceneStackBehaviourAttribute(
            MicrosceneStackConnectionType type = MicrosceneStackConnectionType.MultipleOutput,
            string tooltip = null)
        {
            Type    = type;
            Tooltip = tooltip;
        }
        
        public MicrosceneStackConnectionType Type { get; }
        public string Tooltip { get; }
    }
    
    [System.Serializable, NodeIcon("Assets/Game Core/Level Design/Microscene System/microscenes/Editor/Icons/ActionStack.png")]
    public abstract class MicrosceneStackBehaviour
    {
        public abstract void Reset (MicrosceneNode[] stack);
        public abstract bool Update(in MicrosceneContext ctx, MicrosceneNode[] stack, ref int winnerIndex);
    }
}