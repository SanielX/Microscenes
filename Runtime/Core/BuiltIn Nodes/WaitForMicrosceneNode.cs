using UnityEngine;

namespace Microscenes.Nodes
{
    [MicrosceneNode, NodePath(NodeFolder.Abstract + "Wait For Microscene")]
    public class WaitForMicrosceneNode : MicrosceneNode
    {
        [SerializeField] Microscene m_Microscene;

        protected override void OnStart(in MicrosceneContext ctx)
        {
            if (!m_Microscene)
            {
                Debug.LogError("Referenced microscene was not assigned or destroyed", ctx.caller);
                Complete();
                return;
            }
        }

        protected override void OnUpdate(in MicrosceneContext ctx)
        {
            if(m_Microscene.GraphState == MicrosceneGraphState.Finished)
                Complete();
        }
    }
}