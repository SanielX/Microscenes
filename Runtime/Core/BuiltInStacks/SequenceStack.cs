namespace Microscenes.Nodes
{
    [MicrosceneStackBehaviour(MicrosceneStackConnectionType.SingleOutput)]
    sealed class SequenceStack : MicrosceneStackBehaviour
    {
        private int index;
        
        public override void Start(ref MicrosceneStackContext ctx)
        {
            index = 0;
        }

        public override MicrosceneStackResult Update(ref MicrosceneStackContext ctx)
        {
            var nodeState = ctx.UpdateNode(index);
            if(nodeState == MicrosceneNodeState.Finished)
                ++index;
            
            return FinishIf(index >= ctx.StackLength);
        }
    }
}