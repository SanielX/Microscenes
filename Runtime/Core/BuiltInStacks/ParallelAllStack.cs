namespace Microscenes.Nodes
{
    [NodePath("Parallel\\All Stack")]
    [MicrosceneStackBehaviour(MicrosceneStackConnectionType.SingleOutput, 
        tooltip: "Will update each child node every frame and move when all of them are completed")]
    sealed class ParallelAllStack : MicrosceneStackBehaviour
    {
        public override MicrosceneStackResult Update(ref MicrosceneStackContext ctx)
        {
            bool allNodesCompleted = true;
            for (int i = 0; i < ctx.StackLength; i++)
            {
                var nodeState = ctx.UpdateNode(i);

                allNodesCompleted &= nodeState == MicrosceneNodeState.Finished;
            }
            
            return FinishIf(allNodesCompleted);
        }
    }
}