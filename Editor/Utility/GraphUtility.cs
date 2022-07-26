using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    public interface IStackListener
    {
        public void OnAddedToStack(StackNode node, int index);
        public void OnRemovedFromStack(StackNode node);
    }

    static class GraphExtensions
    {
        public static void SetGraphPosition(this GraphElement node, in Vector2 pos)
        {
            var rectPos = node.GetPosition();
            rectPos.x = pos.x;
            rectPos.y = pos.y;

            node.SetPosition(rectPos);
        }
    }

    [System.Serializable]
    struct StickyNoteMetadata
    {
        public StickyNoteMetadata(StickyNote note)
        {
            rect = note.GetPosition();
            fontSize = note.fontSize;
            theme = note.theme;
            title = note.title;
            content = note.contents;
        }

        public StickyNote CreateFromData()
        {
            var note = new StickyNote();
            note.SetPosition(rect);
            note.fontSize = fontSize;
            note.theme = theme;
            note.title = title;
            note.contents = content;

            return note;
        }

        public Rect   rect;
        public StickyNoteFontSize fontSize;
        public StickyNoteTheme    theme;
        public string title;
        public string content;
    }

    public class AutoGraphView : GraphView
    {
        protected GenericSearchBuilder nodesSearcher;

        public AutoGraphView() : base()
        {

            this.style.backgroundColor = ColorUtils.FromHEX(0x_1e1e1e);

            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new RectangleSelector());

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            nodesSearcher = ScriptableObject.CreateInstance<GenericSearchBuilder>();
            nodesSearcher.Title = "Create Node";

            base.elementsInsertedToStackNode = (node, index, elements) =>
            {
                foreach (var element in elements)
                {
                    if (element is IStackListener l)
                        l.OnAddedToStack(node, index);
                }
            };

            base.elementsRemovedFromStackNode = (node, elements) =>
            {
                foreach (var element in elements)
                {
                    if (element is IStackListener l)
                        l.OnRemovedFromStack(node);
                }
            };
        }

        protected List<Port> portCache = new List<Port>();
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            portCache.Clear();

            var direction = startPort.direction;
            foreach (var node in nodes.ToList())
            {
                if (startPort.node == node)
                    continue;

                if (direction == Direction.Output)
                {
                    foreach (var inputPort in node.inputContainer.Query<Port>().Build().ToList())
                    {
                        if (inputPort.direction == Direction.Input)
                            portCache.Add(inputPort);
                    }
                }
                else
                {
                    foreach (var port in node.outputContainer.Query<Port>().Build().ToList())
                    {
                        if (port.direction == Direction.Output)
                            portCache.Add(port);
                    }
                }
            }

            return portCache;
        }

        /// <summary>
        /// Removes all graph elements from graph view
        /// </summary>
        protected void ClearElements()
        {
            var graphElements = base.graphElements.ToList();
            while (graphElements.Count > 0)
            {
                RemoveElement(graphElements[0]);
                graphElements.RemoveAt(0);
            }
        }
    }

    internal class AutoPort : Port
    {
        public GraphView view;

        public Action OnConnected;
        public Action OnDisconnected;

        public override string title { get => base.portName; set => base.portName = value; }

        public class AutoEdgeConnector<TEdge> : EdgeConnector where TEdge : Edge, new()
        {
            private EdgeDragHelper dragHelper;

            private bool    active;
            private Edge    edgeCandidate;
            private Vector2 mouseDownPosition;

            internal const float CONNECTION_DISTANCE = 10f;

            public override EdgeDragHelper edgeDragHelper => dragHelper;

            public AutoEdgeConnector(IEdgeConnectorListener listener)
            {
                active = false;
                dragHelper = new EdgeDragHelper<TEdge>(listener);
                base.activators.Add(new ManipulatorActivationFilter
                {
                    button = MouseButton.LeftMouse
                });
            }

            protected override void RegisterCallbacksOnTarget()
            {
                base.target.RegisterCallback<MouseDownEvent>(OnMouseDown);
                base.target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                base.target.RegisterCallback<MouseUpEvent>(OnMouseUp);
                base.target.RegisterCallback<KeyDownEvent>(OnKeyDown);
                base.target.RegisterCallback<MouseCaptureOutEvent>(OnCaptureOut);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                base.target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                base.target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                base.target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
                base.target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            }

            protected virtual void OnMouseDown(MouseDownEvent evt)
            {
                if (active)
                {
                    evt.StopImmediatePropagation();
                }
                else
                {
                    if (CanStartManipulation(evt))
                    {
                        Port port = base.target as Port;
                        if (port != null)
                        {
                            mouseDownPosition = evt.localMousePosition;
                            
                            // if (port.direction == Direction.Input && portConnections.Count() > 0)
                            // {
                            //     m_EdgeCandidate = portConnections.First();
                            // 
                            //     m_EdgeCandidate.input?.DisconnectAll();
                            //     m_EdgeCandidate.input?.node.RefreshPorts();
                            // 
                            //     m_EdgeCandidate.output?.DisconnectAll();
                            //     m_EdgeCandidate.output?.node.RefreshPorts();
                            // 
                            //     m_EdgeCandidate = new TEdge();
                            //     m_EdgeDragHelper.draggedPort = m_EdgeCandidate.output;
                            // }
                            // else
                            // {

                            edgeCandidate = new TEdge();
                            dragHelper.draggedPort = port;

                            dragHelper.edgeCandidate = edgeCandidate;
                            if (dragHelper.HandleMouseDown(evt))
                            {
                                active = true;

                                base.target.CaptureMouse();
                                evt.StopPropagation();
                            }
                            else
                            {
                                dragHelper.Reset();
                                edgeCandidate = null;
                            }
                        }
                    }
                }
            }

            private void OnCaptureOut(MouseCaptureOutEvent evt)
            {
                active = false;
                if (edgeCandidate != null)
                    AbortConnectingOperation();
            }

            protected virtual void OnMouseMove(MouseMoveEvent evt)
            {
                if (active)
                {
                    dragHelper.HandleMouseMove(evt);
                    edgeCandidate.candidatePosition = evt.mousePosition;
                    edgeCandidate.UpdateEdgeControl();
                    evt.StopPropagation();
                }
            }

            protected virtual void OnMouseUp(MouseUpEvent evt)
            {
                if (active && CanStopManipulation(evt))
                {
                    if (Vector2.Distance(mouseDownPosition, evt.localMousePosition) > CONNECTION_DISTANCE)
                        dragHelper.HandleMouseUp(evt);
                    else
                        AbortConnectingOperation();

                    active = false;
                    edgeCandidate = null;
                    base.target.ReleaseMouse();
                    evt.StopPropagation();
                }
            }

            private void OnKeyDown(KeyDownEvent evt)
            {
                if (evt.keyCode == KeyCode.Escape && active)
                {
                    AbortConnectingOperation();
                    active = false;
                    base.target.ReleaseMouse();
                    evt.StopPropagation();
                }
            }

            private void AbortConnectingOperation()
            {
                (base.target?.GetFirstAncestorOfType<GraphView>())?.RemoveElement(edgeCandidate);
                edgeCandidate.input = null;
                edgeCandidate.output = null;
                edgeCandidate = null;
                dragHelper.Reset();
            }
        }

        private class AutoEdgeConnectorListener : IEdgeConnectorListener
        {
            public bool createIfNone;
            public GraphView view;

            private GraphViewChange    graphViewChange;
            private List<Edge>         edgesToCreate;
            private List<GraphElement> edgesToDelete;

            public AutoEdgeConnectorListener()
            {
                edgesToCreate = new List<Edge>();
                edgesToDelete = new List<GraphElement>();
                graphViewChange.edgesToCreate = edgesToCreate;
            }

            public void OnDropOutsidePort(Edge edge, Vector2 position)
            {
                edge.input?.node.RefreshPorts();
                edge.output?.node.RefreshPorts();

                if (createIfNone && edge.output != null)
                {
                    var nodeCtx = new NodeCreationContext();

                    nodeCtx.screenMousePosition = CursorPosition.GetScreenSpacePosition();
                    nodeCtx.target = edge.output;

                    view.nodeCreationRequest.Invoke(nodeCtx);
                }
            }

            public void OnDrop(GraphView graphView, Edge edge)
            {
                this.edgesToCreate.Clear();
                this.edgesToCreate.Add(edge);
                edgesToDelete.Clear();
                if (edge.input.capacity == Capacity.Single)
                {
                    foreach (Edge connection in edge.input.connections)
                    {
                        if (connection != edge)
                        {
                            edgesToDelete.Add(connection);
                        }
                    }
                }

                if (edge.output.capacity == Capacity.Single)
                {
                    foreach (Edge connection2 in edge.output.connections)
                    {
                        if (connection2 != edge)
                        {
                            edgesToDelete.Add(connection2);
                        }
                    }
                }

                if (edgesToDelete.Count > 0)
                {
                    graphView.DeleteElements(edgesToDelete);
                }

                List<Edge> edgesToCreate = this.edgesToCreate;
                if (graphView.graphViewChanged != null)
                {
                    edgesToCreate = graphView.graphViewChanged.Invoke(graphViewChange).edgesToCreate;
                }

                foreach (Edge item in edgesToCreate)
                {
                    graphView.AddElement(item);
                    edge.input.Connect(item);
                    edge.output.Connect(item);
                }
            }
        }

        protected AutoPort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type)
            : base(portOrientation, portDirection, portCapacity, type)
        {
        }

        public static AutoPort Create<TEdge>(Orientation orientation, Direction direction, Capacity capacity,
                                                       Type type, GraphView view, bool createIfNone) where TEdge : Edge, new()
        {
            AutoEdgeConnectorListener listener = new AutoEdgeConnectorListener();
            listener.view = view;
            listener.createIfNone = createIfNone;

            AutoPort port = new AutoPort(orientation, direction, capacity, type)
            {
                m_EdgeConnector = new AutoEdgeConnector<TEdge>(listener),
            };

            port.AddManipulator(port.m_EdgeConnector);
            port.view = view;
            port.portName = "";

            return port;
        }

        public override void Connect(Edge edge)
        {
            if(capacity == Capacity.Single)
                DisconnectAll();
            base.Connect(edge);

            OnConnected?.Invoke();
        }

        public override void Disconnect(Edge edge)
        {
            edge.parent?.Remove(edge);
            base.Disconnect(edge);

            OnDisconnected?.Invoke();
        }

        public override void DisconnectAll()
        {
            var connectionsOld = connections.ToList();

            base.DisconnectAll();

            foreach (var edge in connectionsOld)
            {
                var output = edge.output;
                var input  = edge.input;

                edge.parent?.Remove(edge);

                output.portCapLit = false;
                input.portCapLit = false;
            }
        }
    }
}
