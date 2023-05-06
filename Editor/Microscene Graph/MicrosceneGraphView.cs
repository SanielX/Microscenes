using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microscenes.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    internal class MicrosceneGraphView : AutoGraphView
    {
        public Microscene microsceneComponent;
        private SerializedObject serializedMicroscene;

        public EditorWindow parentWindow;
        
        public Action<GraphElement> OnSelectedElement;
        
        public MicrosceneGraphView(EditorWindow parent) : base()
        {
            styleSheets.Add(MicrosceneGraphViewResources.LoadStyles());

            parentWindow = parent;
            nodeCreationRequest = CreateNode;
            
            // Turns out you can't just add Undo by serializing graph each time its changed since this callback is rather
            // unreliable, not to mention it won't cover node GUI which uses rather weird way of drawing GUI with temporary scriptable object
            // I have to get rid of that before Undo can work, but that would mean every node has to connect to an actual property inside Microscene component,
            // And that is not pretty at all
            graphViewChanged += (change) =>
            {
                if(isGeneratingContent) // During intialization this is going to be called a lot of times and we don't need any of these
                    return change;
                
                if (microsceneComponent)
                {
                    Serialize();
                    // EditorUtility.SetDirty(microsceneComponent);
                }
                
                return change;
            };
            
            Undo.undoRedoPerformed += OnUndoRedoPerformed; 
        }

        ~MicrosceneGraphView()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed; 
        }
        
        void OnUndoRedoPerformed()
        {   
            MarkDirtyRepaint();
            if (microsceneComponent)
            {
                serializedMicroscene.Update();
                GenerateMicrosceneContent(microsceneComponent, serializedMicroscene, changeViewPosition: false);
            }
        }

        public override EventPropagation DeleteSelection()
        {
            var prop = base.DeleteSelection();
            
            return prop;
        }

        void CreateNode(NodeCreationContext creationContext)
        {
            nodesSearcher.ClearEntries();

            if (!(creationContext.target is StackNode))
            {
                AddDerivedFrom<MicrosceneNode>(nodesSearcher, "Node/", new EditorIcon("Resources/MSceneEditorIcon"));
                AddStackTypes<MicrosceneStackBehaviour>(nodesSearcher, "Stack/", new EditorIcon("SortingGroup Icon"));

                if(creationContext.target is null)
                {
                    nodesSearcher.AddEntry("Sticky Note", userData: typeof(StickyNote), icon: new EditorIcon("Tile Icon"));
                }
            }
            else if(creationContext.target is MicrosceneStackNodeView stack)
            {
                AddDerivedFrom<MicrosceneNode>(nodesSearcher, "Nodes/");
            }

            nodesSearcher.Build();
            nodesSearcher.OnSelected = (entry, ctx) =>
            {
                var nodeType = entry.userData as Type;
                Node node = null;

                if (nodeType.IsSubclassOf(typeof(MicrosceneNode)))
                {
                    var action = (MicrosceneNode)ScriptableObject.CreateInstance(nodeType);
                    node = new MicrosceneNodeView(ref action, this) { NodeID = ++maxNodeID };
                }
                else if (nodeType.IsSubclassOf(typeof(MicrosceneStackBehaviour)))
                {
                    var behaviour = (MicrosceneStackBehaviour)Activator.CreateInstance(nodeType);
                    node = new MicrosceneStackNodeView(behaviour, this) { NodeID = ++maxNodeID };
                } 
                else if(nodeType == typeof(StickyNote))
                {
                    var stickyNode = new StickyNote() { title = "Note", fontSize = StickyNoteFontSize.Small };
                    AddElement(stickyNode);
                    stickyNode.SetGraphPosition(TransfromToLocal(ctx.screenMousePosition));
                    // graphViewChanged?.Invoke(new GraphViewChange());
                    return;
                }

                if (node is null)
                    return;
                
                EditorUtility.SetDirty(microsceneComponent);
                AddElement(node);
                
                Vector2 graphMousePosition = TransfromToLocal(ctx.screenMousePosition);
                Rect newNodePos = node.GetPosition();
                newNodePos.position = graphMousePosition;
                
                if(node is MicrosceneNodeView v) v.NodePosition = newNodePos;
                else if(node is MicrosceneStackNodeView s) s.NodePosition = newNodePos;

                if (creationContext.target != null)
                {
                    if (creationContext.target is MicrosceneStackNodeView stackNode)
                    {
                        List<ISelectable> selectables = new List<ISelectable>();
                        selectables.Add(node);

                        stackNode.InsertElement(creationContext.index, node);

                        if (node is IStackListener listener)
                            listener.OnAddedToStack(stackNode, creationContext.index);
                    }
                    else if (creationContext.target is AutoPort port)
                    {
                        if (port.direction == UnityEditor.Experimental.GraphView.Direction.Output)
                        {
                            if(node is IConnectable connectable)
                                AddElement(connectable.ConnectInputTo(port));
                        }
                    }
                }
                
                if(creationContext.target is not StackNode)
                    node.SetGraphPosition(newNodePos);
                
                graphViewChanged?.Invoke(new GraphViewChange());
            };

            var searchWindowCtx = new SearchWindowContext(creationContext.screenMousePosition);
            SearchWindow.Open(searchWindowCtx, nodesSearcher);
        }

        private Vector2 TransfromToLocal(Vector2 globalPosition)
        {
            var mousePosition = parentWindow.rootVisualElement.ChangeCoordinatesTo(parentWindow.rootVisualElement.parent,
                globalPosition - parentWindow.position.position);
            var graphMousePosition = this.contentViewContainer.WorldToLocal(mousePosition);

            var createPos = this.WorldToLocal(graphMousePosition);
            return createPos;
        }

        void AddStackTypes<T>(GenericSearchBuilder builder, string prefix, Texture defaultIcon = null)
        {
            var microprecondTypes = TypeCache.GetTypesDerivedFrom<T>();
            var ctxs = this.microsceneComponent.context;

            foreach (var type in microprecondTypes)
            {
                if (type is null || type.IsAbstract)
                    continue;
                
                // Serializable attribute isn't inherited but unity somehow manages to work with base classes, which is weird, but whatever
                // var isSerializable = type.GetCustomAttribute<SerializableAttribute>() is not null;
                // if(!isSerializable)
                //     continue;
                
                var nodeAttr = type.GetCustomAttribute<MicrosceneStackBehaviourAttribute>(inherit: false);
                if (nodeAttr is null)
                    continue;

                var requireAttr = type.GetCustomAttribute<RequireContextAttribute>();
                if (requireAttr != null && (ctxs is null || ctxs != requireAttr.ContextType))
                    continue;

                string path;
                var attr = type.GetCustomAttribute<NodePathAttribute>();
                if (attr is null)
                {
                    path = ObjectNames.NicifyVariableName(type.Name);
                }
                else
                    path = attr.Path;

                var iconForType   = IconsProvider.Instance.GetIconForType(type);
                    iconForType ??= defaultIcon;
                
                builder.AddEntry(prefix + path, userData: type, icon: iconForType);
            }
        }

        void AddDerivedFrom<T>(GenericSearchBuilder builder, string prefix, Texture defaultIcon = default)
        {
            var microprecondTypes = TypeCache.GetTypesDerivedFrom<T>();
            var ctxs = this.microsceneComponent.context;

            foreach (var type in microprecondTypes)
            {
                if (type is null || type.IsAbstract)
                    continue;
                
                // Serializable attribute isn't inherited but unity somehow manages to work with base classes, which is weird, but whatever
                // var isSerializable = type.GetCustomAttribute<SerializableAttribute>() is not null;
                // if(!isSerializable)
                //     continue;
 
                var nodeAttr = type.GetCustomAttribute<MicrosceneNodeAttribute>(inherit: true);
                if (nodeAttr is null)
                    continue;

                var requireAttr = type.GetCustomAttribute<RequireContextAttribute>();
                if (requireAttr != null && (ctxs is null || ctxs != requireAttr.ContextType))
                    continue;

                string path;
                var attr = type.GetCustomAttribute<NodePathAttribute>();
                if (attr is null)
                {
                    path = ObjectNames.NicifyVariableName(type.Name);
                }
                else
                    path = attr.Path;

                var iconForType   = IconsProvider.Instance.GetIconForType(type);
                    iconForType ??= defaultIcon;
                    
                builder.AddEntry(prefix + path, userData: type, icon: iconForType);
            }
        }
        
        bool isGeneratingContent;
        int maxNodeID;
        // TODO: Make serialization & deserialization less of a mess
        public void GenerateMicrosceneContent(Microscene scene, SerializedObject serializedMicroscene, bool changeViewPosition = true)
        {
            maxNodeID = 0;
            ClearElements();
            this.microsceneComponent = scene;
            this.serializedMicroscene = serializedMicroscene;

            if (!scene)
                return;
            
            isGeneratingContent = true;

            MicrosceneGraphMetadata graphMeta = new MicrosceneGraphMetadata();
            if (!string.IsNullOrEmpty(scene.m_MetadataJson))
                EditorJsonUtility.FromJsonOverwrite(scene.m_MetadataJson, graphMeta);
            
            var entryView     = new EntryMicrosceneNodeView(this, "Entry")
            {
                NodePosition = graphMeta.entryPosition,
                NodeID = int.MinValue,
            };
            var quitEntryView = new EntryMicrosceneNodeView(this, "Exit")
            {
                NodePosition = graphMeta.quitEntryPosition,
                NodeID = int.MinValue+1,
            };
            
            AddElement(entryView);
            entryView.SetGraphPosition(graphMeta.entryPosition);

            AddElement(quitEntryView);
            quitEntryView.SetGraphPosition(graphMeta.quitEntryPosition);

            if (changeViewPosition && graphMeta.cameraPosition != Vector3.zero)
            {
                base.UpdateViewTransform(graphMeta.cameraPosition, graphMeta.cameraScale);
            }

            foreach (var stickyNote in graphMeta.stickyNotes)
                AddElement(stickyNote.CreateFromData());
            
            Dictionary<MicrosceneNodeEntry, GraphElement> entryMap = new(scene.m_AllEntries.Length);
            
            for (int i = 0; i < scene.m_AllEntries.Length; i++)
            {
                var entry = scene.m_AllEntries[i];
                maxNodeID = Mathf.Max(entry.NodeID, maxNodeID);

                if (graphMeta.FindStackNodeData(entry.NodeID, out var stackMeta))
                {
                    var stackNode = new MicrosceneStackNodeView(entry.stackBehaviour, this);
                    stackNode.NodeID = entry.NodeID;

                    stackMeta.ApplyToNode(stackNode);
                    
                    AddElement(stackNode);

                    for (var iNode = 0; iNode < entry.nodeStack.Length; iNode++)
                    {
                        ref var node = ref entry.nodeStack[iNode];
                        if (node is null) // Can happen if type was renamed and now SeriaizeReference is broken :(
                            continue;

                        var nodeView = new MicrosceneNodeView(ref node, this);
                        nodeView.NodeID = entry.NodeID;

                        stackNode.AddElement(nodeView);
                        nodeView.OnAddedToStack(stackNode, stackNode.childCount - 1);
                    }

                    entryMap.Add(entry, stackNode);
                }
                else if (entry.nodeStack.Length > 0 && graphMeta.FindNodeData(entry.NodeID, out var nodeMeta))
                {
                    ref var node = ref entry.nodeStack[0];
                    
                    var nodeView = new MicrosceneNodeView(ref node, this);
                    nodeView.NodeID = entry.NodeID;
                    
                    nodeMeta.ApplyToNode(nodeView);
                    
                    AddElement(nodeView);
                    entryMap.Add(entry, nodeView);
                }
            }

            for (int i = 0; i < scene.m_AllEntries.Length; i++)
            {
                var entry = scene.m_AllEntries[i];
                
                if(!entryMap.TryGetValue(entry, out var graphElement) || graphElement is not IConnectable connectable)
                    continue;

                connectOutputs(entry, connectable);
            }
            
            connectOutputs(scene.m_Root,     entryView);
            connectOutputs(scene.m_QuitRoot, quitEntryView);
            
            foreach (var node in nodes)
            {
                node.RefreshPorts();
                node.RefreshExpandedState();
            }
            
            
            isGeneratingContent = false;
            
            void connectOutputs(MicrosceneNodeEntry entry, IConnectable connectable)
            {
                if(entry is null)
                    return;
                
                var connectableChildren = connectable.Children.ToArray();
                for (int i = 0; 
                     entry.connections is not null && 
                     i < entry.connections.Length; 
                     i++)
                {
                    for (int j = 0; j < entry.connections[i].Length; 
                         j++)
                    {
                        MicrosceneNodeEntry connectedEntry = entry.connections[i][j];
                        var target = entryMap[connectedEntry];
                        var edge = connectableChildren[i].ConnectOutputTo(target as IConnectable);
                        AddElement(edge);
                    }
                }
            }
        }
        
        private static DefaultStackBehvaiour                         poolStackBehaviour = new();
        private static MicrosceneGraphMetadata                       pooledGraphMeta    = new();
        private static Dictionary<GraphElement, MicrosceneNodeEntry> pooledNodesMap     = new(64);
        private static List<MicrosceneNode>                          pooledNodeList     = new(64);
        private static HashSet<GraphElement>                         pooledElementSet   = new(64);
        private static List<MicrosceneNodeEntry>                     pooledConnections  = new(16);
        public void Serialize()
        {
            if(!microsceneComponent)
                return;
            
            serializedMicroscene.Update();
            Undo.RecordObject(microsceneComponent, "Save microscene component");
            // This is shared by all single nodes, behvaiour that just updates it and completes when 
            // node[0] is completed
            DefaultStackBehvaiour defaultStackBehvaiour = poolStackBehaviour;
            
            MicrosceneGraphMetadata graphMeta = pooledGraphMeta;
            graphMeta.Clear();
            graphMeta.cameraPosition = base.viewTransform.position;
            graphMeta.cameraScale    = base.viewTransform.scale;
            
            // Associate a node entry with graph element
            Dictionary<GraphElement, MicrosceneNodeEntry> nodesMap = pooledNodesMap;
            nodesMap.Clear();
            
            pooledNodeList.Clear();
            pooledElementSet.Clear();
            pooledConnections.Clear();

            // Since graph may have loops and recursion we need to first create base building blocks
            // e.g. individual nodes, next step is going to be making them reference each other
            List<GraphElement> allGraphElements = graphElements.ToList();
            foreach (var element in allGraphElements)
            {
                if(element is StickyNote note)
                {
                    graphMeta.stickyNotes.Add(new StickyNoteMetadata(note));
                    continue;
                }

                if (element is MicrosceneStackNodeView stack)
                {
                    IConnectable connectableStack = stack;
                    pooledNodeList.Clear();

                    foreach (var stackNode in stack.Children())
                    {
                        if(stackNode is MicrosceneNodeView node)
                            pooledNodeList.Add(node.binding as MicrosceneNode);
                    }
                    
                    int connectionsCount = CountConnectedStacks(pooledElementSet, connectableStack);

                    MicrosceneNodeEntry nodeEntry = new()
                    {
                        stackBehaviour            = (MicrosceneStackBehaviour)stack.wrapper.binding,
                        NodeID                    = stack.NodeID,
                        InputPortsConnectionCount = connectionsCount,
                        nodeStack                 = pooledNodeList.ToArray(),
                    };
                    
                    nodesMap.Add(stack, nodeEntry);
                    graphMeta.stackNodes.Add(new(stack)
                    {
                        nodeID = nodeEntry.NodeID,
                    });
                    
                    continue;
                }

                if (element is MicrosceneNodeView nodeView && nodeView.ownerStack is null && nodeView.binding)
                {
                    pooledNodeList.Clear();
                    pooledNodeList.Add(nodeView.binding);
                    int connectionsCount = CountConnectedStacks(pooledElementSet, nodeView);
                    
                    MicrosceneNodeEntry nodeEntry = new()
                    {
                        stackBehaviour = defaultStackBehvaiour,
                        NodeID    = nodeView.NodeID,
                        nodeStack = pooledNodeList.ToArray(),
                        InputPortsConnectionCount = connectionsCount,
                    };
                    
                    nodesMap.Add(nodeView, nodeEntry);
                    graphMeta.nodes.Add(new(nodeView)
                    {
                        nodeID = nodeEntry.NodeID,
                    });
                }
            }
            
            foreach (var element in allGraphElements)
            {
                if (nodesMap.TryGetValue(element, out var entry) && element is IConnectable connectable)
                {
                    var connectionMatrix = ExtractConnectedEntries(connectable);
                    entry.connections   = connectionMatrix;
                }
                
                if (element is EntryMicrosceneNodeView entryNode)
                {
                    var connectionMatrix = ExtractConnectedEntries(entryNode);
                    
                    MicrosceneNodeEntry root = new()
                    {
                        nodeStack = null,
                        connections = connectionMatrix,
                    };
                    
                    // FIXME: This is stupid
                    if (entryNode.ID == "Entry")
                    {
                        graphMeta.entryPosition = entryNode.NodePosition;
                        microsceneComponent.m_Root = root;
                    }
                    else
                    {
                        graphMeta.quitEntryPosition = entryNode.NodePosition;
                        microsceneComponent.m_QuitRoot = root;
                    }
                }
            }
            
            microsceneComponent.m_MetadataJson = EditorJsonUtility.ToJson(graphMeta, prettyPrint: true);
            microsceneComponent.m_AllEntries   = nodesMap.Values.ToArray();
            
            EditorUtility.SetDirty(microsceneComponent);
            serializedMicroscene.Update();
            serializedMicroscene.ApplyModifiedProperties();
            
            MicrosceneNodeEntryConnections[] ExtractConnectedEntries(IConnectable connectable)
            { 
                var childrenArray = connectable.Children.ToArray();
                MicrosceneNodeEntryConnections[] output = new MicrosceneNodeEntryConnections[childrenArray.Length];
                
                for (int i = 0; i < childrenArray.Length; i++)
                {
                    IConnectable childAt = childrenArray[i];
                    pooledConnections.Clear();

                    foreach (var outputEdge in childAt.OutputEdges())
                    {
                        if (outputEdge is null || outputEdge.input is null || outputEdge.input.node is null)
                            continue;

                        var connectedNode = nodesMap[outputEdge.input.node];
                        pooledConnections.Add(connectedNode);
                    }

                    output[i] = pooledConnections.ToArray();
                }

                return output;
            }
        }

        public static int CountConnectedStacks(HashSet<GraphElement> pooledElementSet, IConnectable connectableStack)
        {
            var connectionsCount = 0;
            pooledElementSet.Clear();

            // It's important to count unique stacks node is connected to, because otherwise 
            // stacks that have multiple output may break,
            // since only one of those outputs is ever taken and if 2 inputs connect to same node then we're screwed,
            // as condition will require 2 nodes to reach same input port, which will never happen
            foreach (var connection in connectableStack.input.connections)
            {
                var outputNode = connection.output.node;
                if (outputNode is null)
                    continue;

                if (outputNode is MicrosceneStackNodeView s && pooledElementSet.Add(s))
                    connectionsCount++;
                else if (outputNode is MicrosceneNodeView connectedNodeView)
                {
                    if (connectedNodeView.ownerStack is not null)
                    {
                        if (pooledElementSet.Add(connectedNodeView.ownerStack))
                        {
                            connectionsCount++;
                        }
                    }
                    else connectionsCount++;
                }
            }

            return connectionsCount;
        }

        public void UpdateNodesFromReport()
        {
            if (!Application.isPlaying)
            {
                foreach (var node in base.nodes)
                {
                    if (node is MicrosceneStackNodeView stackView)
                    {
                        foreach (var child in stackView.Children())
                        {
                            SetVisualElementStyleFromReport(child, default);
                        }
                    }
                    else if (node is MicrosceneNodeView nodeView)
                    {
                        SetVisualElementStyleFromReport(nodeView, default);
                    }
                }

                return;
            }

            foreach (var report in microsceneComponent._nodeReports)
            {
                foreach (var node in base.nodes)
                {
                    if (node is MicrosceneNodeView nodeView)
                    {
                        if (nodeView.binding == report.Key)
                        {
                            SetVisualElementStyleFromReport(nodeView, report.Value);
                            continue;
                        }
                    }
                }
            }
        }

        void SetVisualElementStyleFromReport(VisualElement node, in Microscene.NodeReport report)
        {
            switch (report.result)
            {
                case Microscene.NodeReportResult.None: {
                    node.RemoveFromClassList("node-updating");
                    node.RemoveFromClassList("node-finished");
                    node.RemoveFromClassList("node-crashed");
                }
                    break;
                
                case Microscene.NodeReportResult.Updating: {
                    node.AddToClassList("node-updating");
                    node.RemoveFromClassList("node-finished");
                    node.RemoveFromClassList("node-crashed");
                }
                    break;

                case Microscene.NodeReportResult.Succeded:
                    node.RemoveFromClassList("node-updating");
                    node.AddToClassList("node-finished");
                    node.RemoveFromClassList("node-crashed");

                    break;
                
                case Microscene.NodeReportResult.Crashed:
                    node.RemoveFromClassList("node-updating");
                    node.RemoveFromClassList("node-finished");
                    node.AddToClassList("node-crashed");
                    break;
                    
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            if(node is MicrosceneNodeView view)
                view.LastException = report.exception;
        }
    }
}
