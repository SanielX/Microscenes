namespace Microscenes.Nodes
{
    sealed class DefaultStackBehvaiour : MicrosceneStackBehaviour
    {
        public override void Reset(MicrosceneNode[] stack)
        {
        }

        public override bool Update(in MicrosceneContext ctx, MicrosceneNode[] stack, ref int winnerIndex)
        {
            stack[0].UpdateNode(ctx);
            return stack[0].State == MicrosceneNodeState.Finished;
        }
    }
}