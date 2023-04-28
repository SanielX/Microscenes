namespace Microscenes
{
    [SerializeReferencePath("Parallel\\All Stack")]
    [MicrosceneStackBehaviour(MicrosceneStackConnectionType.SingleOutput, 
        tooltip: "Will update each child node every frame and move when all of them are completed")]
    sealed class ParallelAllStack : MicrosceneStackBehaviour
    {
        public override void Reset(MicrosceneNode[] stack)
        {
            
        }

        public override bool Update(in MicrosceneContext ctx, MicrosceneNode[] stack, ref int winnerIndex)
        {
            bool finished = true;
            for (int i = 0; i < stack.Length; i++)
            {
                var node = stack[i];
                node.UpdateNode(ctx);

                finished &= node.State == MicrosceneNodeState.Finished;
            }
            
            return finished;
        }
    }
}