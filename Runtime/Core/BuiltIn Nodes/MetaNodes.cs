using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Microscenes
{
    [MicrosceneNode, SerializeReferencePath(SRPathType.Abstract, "Start Microscene")]
    class StartMicroscene : MicrosceneNode
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

    [MicrosceneNode, SerializeReferencePath(SRPathType.Abstract, "Wait For Microscene")]
    class WaitForMicroscene : MicrosceneNode
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