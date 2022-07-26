using UnityEngine;

namespace Microscenes
{
    // Example of a hybdrid node
    // Waiting may be treated as action but also you might want to have condition
    // E.g. "5 seconds pass or player entered trigger"
    [MicrosceneNodeType(MicrosceneNodeType.Hybrid)]
    [SerializeReferencePath(SRPathType.Abstract, "Wait")]
    public class WaitHybdridNode : MicrosceneNode, INameableNode // INameableNode is explained below
    {
        float timer;
        [SerializeField, Min(0)] float m_WaitTime = 1f;

        protected override void OnStart(in MicrosceneContext ctx)
        {
            timer = 0;
        }

        protected override void OnUpdate(in MicrosceneContext ctx)
        {
            timer += Time.deltaTime;
            if (timer >= m_WaitTime)
                Complete();
        }

        public string GetNiceNameString()
        {
            return m_WaitTime == 1f ? "Wait 1 second" : $"Wait {m_WaitTime} seconds";
        }
    }
}