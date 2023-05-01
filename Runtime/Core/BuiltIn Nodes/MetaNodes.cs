using UnityEngine;

namespace Microscenes.Nodes
{
    [MicrosceneNode, NodePath(NodeFolder.Abstract + "Start Microscene")]
    public class StartMicroscene : MicrosceneNode
    {
        [SerializeField] Microscene m_Microscene;
        [SerializeField] bool       m_Wait;

        protected override void OnStart(in MicrosceneContext ctx)
        {
            if (!m_Microscene)
            {
                Debug.LogError("Referenced microscene was not assigned or destroyed", ctx.caller);
                Complete();
                return;
            }
            
            m_Microscene.StartExecutingMicroscene(null);
            if(!m_Wait)
                Complete();
        }

        protected override void OnUpdate(in MicrosceneContext ctx)
        {
            if (m_Microscene.GraphState == MicrosceneGraphState.Finished)
            {
                Complete();
            }
        }
    }

    [MicrosceneNode, NodePath(NodeFolder.Abstract + "Wait For Microscene")]
    public class WaitForMicroscene : MicrosceneNode
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