namespace Microscenes.Nodes
{
    [MicrosceneStackBehaviour(MicrosceneStackConnectionType.MultipleOutput,
        tooltip: "Selects child node at random")]
    sealed class RandomStack : MicrosceneStackBehaviour
    {
        int nodeIndex = 0;
        
        public override void Start(ref MicrosceneStackContext ctx)
        {
            nodeIndex = UnityEngine.Random.Range(0, ctx.StackLength);
        }

        public override MicrosceneStackResult Update(ref MicrosceneStackContext ctx)
        {
            var finishedNode = ctx.UpdateNode(nodeIndex) == MicrosceneNodeState.Finished;
            return FinishIf(finishedNode);
        }
    }
}