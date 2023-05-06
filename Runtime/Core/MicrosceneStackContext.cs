using System.Runtime.CompilerServices;
using UnityEngine;

namespace Microscenes
{
    public readonly ref struct MicrosceneStackResult
    {
        internal MicrosceneStackResult(bool finished, int winnerIndex)
        {
            this.finished = finished;
            this.winnerIndex = winnerIndex;
        }

        public readonly bool finished;
        public readonly int  winnerIndex;
    }
    
    public ref struct MicrosceneStackContext
    {
        internal MicrosceneStackContext(Component caller, object customData) : this()
        {
            sceneContext.caller     = caller;
            sceneContext.customData = customData;
        }
        
        internal MicrosceneContext sceneContext;
        internal MicrosceneNode[]  childrenNodes;
        
        public object customData => sceneContext.customData;
        public Component caller  => sceneContext.caller;
        public int StackLength   => childrenNodes.Length;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MicrosceneNodeState UpdateNode(int index)
        {
            sceneContext.CurrentNode = childrenNodes[index];
            childrenNodes[index].UpdateNode(sceneContext);
            
            return childrenNodes[index].State;
        }
    }
}