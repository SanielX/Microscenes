namespace Microscenes
{
    [MicrosceneStackBehaviour(tooltip: "Selects child node at random")]
    sealed class RandomStack : MicrosceneStackBehaviour
    {
        int nodeIndex = 0;
        
        public override void Reset(MicrosceneNode[] stack)
        {
            nodeIndex = UnityEngine.Random.Range(0, stack.Length);
        }

        public override bool Update(in MicrosceneContext ctx, MicrosceneNode[] stack, ref int winnerIndex)
        {
            stack[nodeIndex].UpdateNode(ctx);
            return stack[nodeIndex].State == MicrosceneNodeState.Finished;
        }
    }
}