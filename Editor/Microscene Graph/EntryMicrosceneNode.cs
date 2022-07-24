using UnityEditor.Experimental.GraphView;

namespace Microscenes.Editor
{
    class EntryMicrosceneNode : GenericMicrosceneNodeView
    {
        public EntryMicrosceneNode(GraphView view) : base(view)
        {
            capabilities &= ~(Capabilities.Deletable | Capabilities.Stackable);
            
            base.title = "Entry";

            var port = AutoPort.Create<Edge>(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Output, Port.Capacity.Multi, typeof(Microscene), view, true);
            port.portColor = ColorUtils.FromHEX(0x26D9D9);

            outputContainer.Add(port);
        }
    }
}
