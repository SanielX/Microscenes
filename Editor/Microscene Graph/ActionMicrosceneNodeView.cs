using UnityEditor.Experimental.GraphView;

namespace Microscenes.Editor
{
    class ActionMicrosceneNodeView : GenericMicrosceneNodeView<MicroAction>
    {
        public ActionMicrosceneNodeView(MicroAction binding, GraphView view) : base(binding, view)
        {
        }

        protected override void CreatePorts(GraphView view, out AutoPort inputPort, out AutoPort outputPort)
        {
            inputPort = AutoPort.Create<Edge>(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Input, Port.Capacity.Multi, typeof(Microscene), view, true);
            outputPort = AutoPort.Create<Edge>(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Output, Port.Capacity.Multi, typeof(Microscene), view, true);

            outputPort.portColor = ColorUtils.FromHEX(0x26D9D9);
        }
    }
}
