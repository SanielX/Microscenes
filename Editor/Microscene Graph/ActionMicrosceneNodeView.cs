using UnityEditor.Experimental.GraphView;

namespace Microscenes.Editor
{
    internal class ActionMicrosceneNodeView : GenericMicrosceneNodeView<MicroAction>
    {
        public ActionMicrosceneNodeView(MicroAction binding, GraphView view) : base(binding, view)
        {
        }

        protected override void CreatePorts(GraphView view, out AutoPort inputPort, out AutoPort outputPort)
        {
            base.CreatePorts(view, out inputPort, out outputPort);

            outputPort.portColor = ColorUtils.FromHEX(0x26D9D9);
        }
    }
}
