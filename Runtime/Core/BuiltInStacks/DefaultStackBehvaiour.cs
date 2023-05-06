namespace Microscenes.Nodes
{
    sealed class DefaultStackBehvaiour : MicrosceneStackBehaviour
    {
        public override MicrosceneStackResult Update(ref MicrosceneStackContext ctx)
        {
            var finishedNode = ctx.UpdateNode(0) == MicrosceneNodeState.Finished;
            return FinishIf(finishedNode);
        }
    }
}