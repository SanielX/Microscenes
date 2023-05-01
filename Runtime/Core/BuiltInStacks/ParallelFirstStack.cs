namespace Microscenes.Nodes
{
    [MicrosceneStackBehaviour(tooltip: "Will update each child node every frame and select output of a node that was completed first")]
    [NodePath("Parallel\\First Stack")]
    sealed class ParallelFirstStack : MicrosceneStackBehaviour
    {
        public override void Reset(MicrosceneNode[] stack)
        {
            
        }

        public override bool Update(in MicrosceneContext ctx, MicrosceneNode[] stack, ref int winnerIndex)
        {
            for (int i = 0; i < stack.Length; i++)
            {
                var node = stack[i];
                node.UpdateNode(ctx);

                if (node.State == MicrosceneNodeState.Finished)
                {
                    winnerIndex = i;
                    return true;
                }
            }
            
            return false;
        }
    }
}