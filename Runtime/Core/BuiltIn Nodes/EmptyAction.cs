namespace Microscenes
{
    [SerializeReferencePath(SRPathType.Abstract, "Empty")]
    [System.Serializable]
    [MicrosceneNode]
    public class EmptyAction : MicrosceneNode
    {
        protected override void OnStart(in MicrosceneContext ctx)
        {
            Complete();
        }
    }
}