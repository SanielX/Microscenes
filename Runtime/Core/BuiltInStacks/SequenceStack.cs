namespace Microscenes
{
    [MicrosceneStackBehaviour(MicrosceneStackConnectionType.SingleOutput)]
    sealed class SequenceStack : MicrosceneStackBehaviour
    {
        private int index;
        
        public override void Reset(MicrosceneNode[] stack)
        {
            index = 0;
        }

        public override bool Update(in MicrosceneContext ctx, MicrosceneNode[] stack, ref int winnerIndex)
        {
            stack[index].UpdateNode(ctx);
            if(stack[index].State == MicrosceneNodeState.Finished)
                ++index;
            
            return index >= stack.Length;
        }
    }
}