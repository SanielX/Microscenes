namespace Microscenes
{
    [MicrosceneNodeType(MicrosceneNodeType.Action)]  // Specify if node is action, precondition or hybrid
    // This is small enum which will unfold into "Abstract/Empty" path
    // You may as well just use path as an argument
    [SerializeReferencePath(SRPathType.Abstract, "Empty")] 
    internal class EmptyActionNode : MicrosceneNode
    {
        protected override void OnStart(in MicrosceneContext ctx)
        {
            Complete(); // Marks node as completed so graph can move forward
        }
    }
}