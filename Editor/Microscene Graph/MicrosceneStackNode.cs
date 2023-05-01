using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    internal class MicrosceneStackNode : StackNode, IConnectable
    {
        protected AutoPort input, output;
        private GraphView view;
        
        public ScriptableWrapper wrapper;
        
        public MicrosceneStackConnectionType ConnectionType { get; }
        
        public int NodeID { get; set; }
        
        /// <summary>
        /// Update on GeometryChangedEvent but never contains NaN values which will hopefully fix random losses of data
        /// </summary>
        public Rect NodePosition { get; set; }

        public MicrosceneStackNode(MicrosceneStackBehaviour stackBehaviour, GraphView view) : base()
        {
            wrapper = ScriptableObject.CreateInstance<ScriptableWrapper>();
            wrapper.binding = stackBehaviour;
            
            this.view = view;
            
            string path;
            var attr = stackBehaviour.GetType().GetCustomAttribute<NodePathAttribute>();
            if (attr is null)
            {
                path = ObjectNames.NicifyVariableName(stackBehaviour.GetType().Name);
            }
            else
                path = attr.Path;
            
            var stackTypeAttr = stackBehaviour.GetType().GetCustomAttribute<MicrosceneStackBehaviourAttribute>();
            ConnectionType = stackTypeAttr.Type;

            titleElement = new Label();
            titleElement.AddToClassList("unity-label");
            
            title = path[(path.LastIndexOf('/')+1)..^0];
            title = title.Replace("Stack", "", StringComparison.OrdinalIgnoreCase).Trim();
            
            tooltip = stackTypeAttr.Tooltip;

            inputContainer.Add(titleElement);
            CreatePorts(view);

            output.AddToClassList("stack-output-port");
            input .AddToClassList("stack-input-port");

            inputContainer.Add(input);
            
            if(ConnectionType == MicrosceneStackConnectionType.SingleOutput)
                inputContainer.Add(output);
            
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            var newRect = evt.newRect;
            if (float.IsNaN(newRect.size.x) || float.IsNaN(newRect.size.y))
            {
                return;
            }
            
            NodePosition = newRect;
        }

        IEnumerable<IConnectable> IConnectable.Children
        {
            get
            {
                if(ConnectionType == MicrosceneStackConnectionType.MultipleOutput)
                    return base.Children().Select((child) => child as IConnectable).Where(c => c is not null);
                else 
                    return new[] { this };
            }
        }

        public Edge ConnectOutputTo(IConnectable connectable)
        {
            return ConnectOutputTo(connectable.input);
        }

        protected virtual void CreatePorts(GraphView view)
        {
            input = AutoPort.Create<Edge>(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Input, Port.Capacity.Multi, typeof(Microscene), view, true);
            output = AutoPort.Create<Edge>(Orientation.Horizontal, UnityEditor.Experimental.GraphView.Direction.Output, Port.Capacity.Multi, typeof(Microscene), view, true);
        }

        protected override bool AcceptsElement(GraphElement element, ref int proposedIndex, int maxIndex)
        {
            bool v = base.AcceptsElement(element, ref proposedIndex, maxIndex);
            var v1 = element as MicrosceneNodeView;
            if(v1 is not null && !v1.wrapper)
                return false;
            
            return v && v1 is not null;
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
        
        Port IConnectable.input  => input;
        Port IConnectable.output => output;
    }
}
