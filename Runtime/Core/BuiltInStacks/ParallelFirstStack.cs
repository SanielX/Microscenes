namespace Microscenes.Nodes
{
    [MicrosceneStackBehaviour(MicrosceneStackConnectionType.MultipleOutput, 
        tooltip: "Will update each child node every frame and select output of a node that was completed first")]
    [NodePath("Parallel\\First Stack")]
    sealed class ParallelFirstStack : MicrosceneStackBehaviour
    {
        public override MicrosceneStackResult Update(ref MicrosceneStackContext ctx)
        {
            for (int i = 0; i < ctx.StackLength; i++)
            {
                var nodeState = ctx.UpdateNode(i);

                if (nodeState == MicrosceneNodeState.Finished)
                {
                    return FinishAndSelect(i);
                }
            }
            
            return Continue();
        }
    }
}