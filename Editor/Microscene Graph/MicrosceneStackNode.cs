using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    abstract class MicrosceneStackNode<T> : StackNode, IConnectable
    {
        protected AutoPort input, output;
        private GraphView view;

        public Edge ConnectInputTo(IConnectable connectable)
        {
            return ConnectInputTo(connectable.output);
        }

        public Edge ConnectOutputTo(IConnectable connectable)
        {
            return ConnectOutputTo(connectable.input);
        }

        public MicrosceneStackNode(GraphView view) : base()
        {
            this.view = view;

            name = stackName;
            titleElement = new Label();
            titleElement.AddToClassList("unity-label");

            inputContainer.Add(titleElement);
            CreatePorts(view);

            output.AddToClassList("stack-output-port");
            input.AddToClassList("stack-input-port");

            inputContainer.Add(input);
            inputContainer.Add(output);
        }

        protected virtual void CreatePorts(GraphView view)
        {
            input = AutoPort.Create<Edge>(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Input, Port.Capacity.Multi, typeof(Microscene), view, true);
            output = AutoPort.Create<Edge>(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Output, Port.Capacity.Multi, typeof(Microscene), view, true);
        }

        protected override bool AcceptsElement(GraphElement element, ref int proposedIndex, int maxIndex)
        {
            bool v = base.AcceptsElement(element, ref proposedIndex, maxIndex);
            bool v1 = element is T;
            return v && v1;
        }

        public Edge ConnectInputTo(Port p)
        {
            return input.ConnectTo(p);
        }

        public Edge ConnectOutputTo(Port p)
        {
            return output.ConnectTo(p);
        }

        public IEnumerable<Edge> OutputEdges()
        {
            return output.connections;
        }

        Label titleElement;
        public override string title { get => titleElement.text; set => titleElement.text = value; }
        public new string tooltip { get => titleElement.tooltip; set => titleElement.tooltip = value; }
        public abstract string stackName { get; }
        Port IConnectable.input  => input;
        Port IConnectable.output => output;
    }
}
