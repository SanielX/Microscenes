using UnityEditor.Experimental.GraphView;

namespace Microscenes.Editor
{
    internal class PreconditionMicrosceneNodeView : GenericMicrosceneNodeView<MicroPrecondition>
    {
        public PreconditionMicrosceneNodeView(MicroPrecondition binding, GraphView view) : base(binding, view)
        {
        }

        protected override void CreatePorts(GraphView view, out AutoPort inputPort, out AutoPort outputPort)
        {
            base.CreatePorts(view, out inputPort, out outputPort);

            inputPort.portColor = ColorUtils.FromHEX(0x26D9D9);
        }
    }
}
