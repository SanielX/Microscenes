namespace Microscenes.Nodes
{
    [System.Serializable]
    [MicrosceneNode, NodePath(NodeFolder.Abstract + "Empty")]
    public class EmptyNode : MicrosceneNode
    {
        protected override void OnStart(in MicrosceneContext ctx)
        {
            Complete();
        }
    }
}