using UnityEditor.Experimental.GraphView;

namespace Microscenes.Editor
{
    internal class PreconditionStackNode : MicrosceneStackNode
    {
        public PreconditionStackNode(GraphView view) : base(view)
        {
            title   = "Conditions Stack";
            tooltip = "Nodes inside stack executed in parallel";
        }

        protected override void CreatePorts(GraphView view)
        {
            input = AutoPort.Create<Edge>(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Input, Port.Capacity.Multi, typeof(Microscene), view, true); 
            input.portColor = ColorUtils.FromHEX(0x26D9D9);

            output = AutoPort.Create<Edge>(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Output, Port.Capacity.Multi, typeof(Microscene), view, true);
        }

        public override string stackName => "precondition-stack-node";

        public override MicrosceneNodeType AcceptingType => MicrosceneNodeType.Precondition;
    }
}
