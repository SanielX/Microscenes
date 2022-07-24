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

        //
        // Сводка:
        //     Manipulator for creating new edges.
        public class ActionEdgeConnector<TEdge> : EdgeConnector where TEdge : Edge, new()
        {
            private readonly EdgeDragHelper m_EdgeDragHelper;

            private Edge m_EdgeCandidate;

            private bool m_Active;

            private Vector2 m_MouseDownPosition;

            internal const float k_ConnectionDistanceTreshold = 10f;

            public override EdgeDragHelper edgeDragHelper => m_EdgeDragHelper;

            public ActionEdgeConnector(IEdgeConnectorListener listener)
            {
                m_EdgeDragHelper = new EdgeDragHelper<TEdge>(listener);
                m_Active = false;
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

            protected virtual void OnMouseDown(MouseDownEvent e)
            {
                if (m_Active)
                {
                    e.StopImmediatePropagation();
                }
                else
                {
                    if (!CanStartManipulation(e))
                    {
                        return;
                    }

                    Port port = base.target as Port;
                    if (port != null)
                    {
                        m_MouseDownPosition = e.localMousePosition;
                        var portConnections = port.connections;

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
                        m_EdgeCandidate = new TEdge();
                        m_EdgeDragHelper.draggedPort = port;
                        // }

                        m_EdgeDragHelper.edgeCandidate = m_EdgeCandidate;
                        if (m_EdgeDragHelper.HandleMouseDown(e))
                        {
                            m_Active = true;
                            base.target.CaptureMouse();
                            e.StopPropagation();
                        }
                        else
                        {
                            m_EdgeDragHelper.Reset();
                            m_EdgeCandidate = null;
                        }
                    }
                }
            }

            private void OnCaptureOut(MouseCaptureOutEvent e)
            {
                m_Active = false;
                if (m_EdgeCandidate != null)
                {
                    Abort();
                }
            }

            protected virtual void OnMouseMove(MouseMoveEvent e)
            {
                if (m_Active)
                {
                    m_EdgeDragHelper.HandleMouseMove(e);
                    m_EdgeCandidate.candidatePosition = e.mousePosition;
                    m_EdgeCandidate.UpdateEdgeControl();
                    e.StopPropagation();
                }
            }

            protected virtual void OnMouseUp(MouseUpEvent e)
            {
                if (m_Active && CanStopManipulation(e))
                {
                    if (CanPerformConnection(e.localMousePosition))
                    {
                        m_EdgeDragHelper.HandleMouseUp(e);
                    }
                    else
                    {
                        Abort();
                    }

                    m_Active = false;
                    m_EdgeCandidate = null;
                    base.target.ReleaseMouse();
                    e.StopPropagation();
                }
            }

            private void OnKeyDown(KeyDownEvent e)
            {
                if (e.keyCode == KeyCode.Escape && m_Active)
                {
                    Abort();
                    m_Active = false;
                    base.target.ReleaseMouse();
                    e.StopPropagation();
                }
            }

            private void Abort()
            {
                (base.target?.GetFirstAncestorOfType<GraphView>())?.RemoveElement(m_EdgeCandidate);
                m_EdgeCandidate.input = null;
                m_EdgeCandidate.output = null;
                m_EdgeCandidate = null;
                m_EdgeDragHelper.Reset();
            }

            private bool CanPerformConnection(Vector2 mousePosition)
            {
                return Vector2.Distance(m_MouseDownPosition, mousePosition) > 10f;
            }
        }

        private class DefaultEdgeConnectorListener : IEdgeConnectorListener
        {
            public bool createIfNone;
            public GraphView view;

            private GraphViewChange m_GraphViewChange;

            private List<Edge> m_EdgesToCreate;

            private List<GraphElement> m_EdgesToDelete;

            public DefaultEdgeConnectorListener()
            {
                m_EdgesToCreate = new List<Edge>();
                m_EdgesToDelete = new List<GraphElement>();
                m_GraphViewChange.edgesToCreate = m_EdgesToCreate;
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
                m_EdgesToCreate.Clear();
                m_EdgesToCreate.Add(edge);
                m_EdgesToDelete.Clear();
                if (edge.input.capacity == Capacity.Single)
                {
                    foreach (Edge connection in edge.input.connections)
                    {
                        if (connection != edge)
                        {
                            m_EdgesToDelete.Add(connection);
                        }
                    }
                }

                if (edge.output.capacity == Capacity.Single)
                {
                    foreach (Edge connection2 in edge.output.connections)
                    {
                        if (connection2 != edge)
                        {
                            m_EdgesToDelete.Add(connection2);
                        }
                    }
                }

                if (m_EdgesToDelete.Count > 0)
                {
                    graphView.DeleteElements(m_EdgesToDelete);
                }

                List<Edge> edgesToCreate = m_EdgesToCreate;
                if (graphView.graphViewChanged != null)
                {
                    edgesToCreate = graphView.graphViewChanged(m_GraphViewChange).edgesToCreate;
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
            DefaultEdgeConnectorListener listener = new DefaultEdgeConnectorListener();
            listener.view = view;
            listener.createIfNone = createIfNone;

            AutoPort port = new AutoPort(orientation, direction, capacity, type)
            {
                m_EdgeConnector = new ActionEdgeConnector<TEdge>(listener),

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
