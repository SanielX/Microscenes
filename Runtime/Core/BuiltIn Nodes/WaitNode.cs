using UnityEngine;

namespace Microscenes.Nodes
{
    [NodePath(NodeFolder.Abstract + "Wait")]
    public class WaitNode : MicrosceneNode
    {
        private double timeOverStamp;
        [SerializeField, Min(0)] float m_Time;

        protected override void OnStart(in MicrosceneContext ctx)
        {
            timeOverStamp = Time.timeAsDouble + m_Time;
        }

        protected override void OnUpdate(in MicrosceneContext ctx)
        {
            if(timeOverStamp >= Time.timeAsDouble)
                Complete();
        }
    }
}