using UnityEditor.Experimental.GraphView;

namespace Microscenes.Editor
{
    internal class EntryMicrosceneNodeView : MicrosceneNodeView
    {
        public EntryMicrosceneNodeView(GraphView view, string ID) : base(view)
        {
            capabilities &= ~(Capabilities.Deletable | Capabilities.Stackable | Capabilities.Copiable);
            
            base.title = ID;
            this.ID = ID;

            var port = AutoPort.Create<Edge>(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Output, Port.Capacity.Multi, typeof(Microscene), view, true);
            port.portColor = ColorUtils.FromHEX(0x26D9D9);

            outputContainer.Add(port);
        }
        
        public string ID;
    }
}
