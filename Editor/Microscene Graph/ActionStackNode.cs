using UnityEditor.Experimental.GraphView;

namespace Microscenes.Editor
{
    class ActionStackNode : MicrosceneStackNode<ActionMicrosceneNodeView>
    {
        public ActionStackNode(GraphView view) : base(view)
        {
            title   = "Actions Stack";
            tooltip = "Nodes inside stack executed in parallel";
        }

        protected override void CreatePorts(GraphView view)
        {
            input  = AutoPort.Create<Edge>(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Input, Port.Capacity.Multi, typeof(Microscene), view, true);
            output = AutoPort.Create<Edge>(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Output, Port.Capacity.Multi, typeof(Microscene), view, true);
            output.portColor = ColorUtils.FromHEX(0x26D9D9);
        }

        public override string stackName => "action-stack-node";
    }
}
