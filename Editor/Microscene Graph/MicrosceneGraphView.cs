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
    internal class MicrosceneGraphView : AutoGraphView
    {
        public Microscene scene;
        private SerializedObject serializedMicroscene;

        public EditorWindow parentWindow;
        
        public Action<GraphElement> OnSelectedElement;
        
        public MicrosceneGraphView(EditorWindow parent) : base()
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(MicrosceneGraphViewResources.STYLE_PATH));

            parentWindow = parent;
            nodeCreationRequest = CreateNode;

            graphViewChanged += (change) =>
            {
                if (scene)
                    EditorUtility.SetDirty(scene);

                return change;
            };
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            ;
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
                AddDerivedFrom<MicrosceneNode>(nodesSearcher, "Node/");
                AddStackTypes<MicrosceneStackBehaviour>(nodesSearcher, "Stack/");

                if(creationContext.target is null)
                {
                    nodesSearcher.AddEntry("Sticky Note", userData: typeof(StickyNote), icon: new EditorIcon("Tile Icon"));
                }
            }
            else if(creationContext.target is MicrosceneStackNode stack)
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
                    var action = (MicrosceneNode)Activator.CreateInstance(nodeType);
                    node = new MicrosceneNodeView(action, this) { NodeID = -1 };
                }
                else if (nodeType.IsSubclassOf(typeof(MicrosceneStackBehaviour)))
                {
                    var behaviour = (MicrosceneStackBehaviour)Activator.CreateInstance(nodeType);
                    node = new MicrosceneStackNode(behaviour, this) { NodeID = -1 };
                } 
                else if(nodeType == typeof(StickyNote))
                {
                    var stickyNode = new StickyNote() { title = "Note", fontSize = StickyNoteFontSize.Small };
                    AddElement(stickyNode);
                    stickyNode.SetGraphPosition(TransfromToLocal(ctx.screenMousePosition));

                    return;
                }

                if (node is null)
                    return;

                EditorUtility.SetDirty(scene);
                AddElement(node);

                Vector2 graphMousePosition = TransfromToLocal(ctx.screenMousePosition);
                Rect newNodePos = node.GetPosition();
                newNodePos.position = graphMousePosition;
                
                node.SetGraphPosition(newNodePos);

                if (creationContext.target != null)
                {
                    if (creationContext.target is StackNode stackNode)
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

        void AddStackTypes<T>(GenericSearchBuilder builder, string prefix)
        {
            var microprecondTypes = TypeCache.GetTypesDerivedFrom<T>();
            var ctxs = this.scene.context;

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
                var attr = type.GetCustomAttribute<SerializeReferencePathAttribute>();
                if (attr is null)
                {
                    path = ObjectNames.NicifyVariableName(type.Name);
                }
                else
                    path = attr.Path;

                builder.AddEntry(prefix + path, userData: type, icon: GetIconForType(type));
            }
        }

        void AddDerivedFrom<T>(GenericSearchBuilder builder, string prefix)
        {
            var microprecondTypes = TypeCache.GetTypesDerivedFrom<T>();
            var ctxs = this.scene.context;

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
                var attr = type.GetCustomAttribute<SerializeReferencePathAttribute>();
                if (attr is null)
                {
                    path = ObjectNames.NicifyVariableName(type.Name);
                }
                else
                    path = attr.Path;

                builder.AddEntry(prefix + path, userData: type, icon: GetIconForType(type));
            }
        }

        public static Texture GetIconForType(Type t)
        {
            var typeIcon = t.GetCustomAttribute<TypeIconAttribute>(inherit: true);
            if (typeIcon is not null)
            {
                if (typeIcon.Type is null)
                {
                    return new EditorIcon(typeIcon.Name);
                }
                
                return new EditorIcon(typeIcon.Type);
            }
            
            return null;
        }
 
        // TODO: Make serialization & deserialization less of a mess
        // Most of this is just dancing around serialization nesting issue
        public void GenerateMicrosceneContent(Microscene scene, SerializedObject serializedMicroscene)
        {
            ClearElements();
            this.scene = scene;
            this.serializedMicroscene = serializedMicroscene;

            if (!scene)
                return;

            MicrosceneGraphMetadata graphMeta = new MicrosceneGraphMetadata();
            if (!string.IsNullOrEmpty(scene.m_MetadataJson))
                EditorJsonUtility.FromJsonOverwrite(scene.m_MetadataJson, graphMeta);
            
            var entryView     = new EntryMicrosceneNodeView(this, "Entry") { NodePosition = graphMeta.entryPosition };
            var quitEntryView = new EntryMicrosceneNodeView(this, "Exit")  { NodePosition = graphMeta.quitEntryPosition };
            
            AddElement(entryView);
            entryView.SetGraphPosition(graphMeta.entryPosition);

            AddElement(quitEntryView);
            quitEntryView.SetGraphPosition(graphMeta.quitEntryPosition);

            if (graphMeta.cameraPosition != Vector3.zero)
            {
                base.UpdateViewTransform(graphMeta.cameraPosition, graphMeta.cameraScale);
            }

            foreach (var stickyNote in graphMeta.stickyNotes)
                AddElement(stickyNote.CreateFromData());
            
            Dictionary<MicrosceneNodeEntry, GraphElement> entryMap = new(scene.m_AllEntries.Length);
            
            for (int i = 0; i < scene.m_AllEntries.Length; i++)
            {
                var entry = scene.m_AllEntries[i];

                if (graphMeta.FindStackNodeData(entry.NodeID, out var stackMeta))
                {
                    var stackNode = new MicrosceneStackNode(entry.stackBehaviour, this);
                    stackMeta.ApplyToNode(stackNode);
                    
                    AddElement(stackNode);

                    foreach (var node in entry.nodeStack)
                    {
                        if(node is null) // Can happen if type was renamed and now SeriaizeReference is broken :(
                            continue;
                        
                        var nodeView = new MicrosceneNodeView(node, this);
                        
                        stackNode.AddElement(nodeView);
                        nodeView.OnAddedToStack(stackNode, stackNode.childCount-1);
                    }
                    
                    entryMap.Add(entry, stackNode);
                }
                else if (entry.nodeStack.Length > 0 && graphMeta.FindNodeData(entry.NodeID, out var nodeMeta))
                {
                    var node = entry.nodeStack[0];
                    var nodeView = new MicrosceneNodeView(node, this);
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
 
        public void Serialize()
        {
            if(!scene)
                return;
            
            serializedMicroscene.Update();
            // This is shared by all single nodes, behvaiour that just updates it and completes when 
            // node[0] is completed
            DefaultStackBehvaiour defaultStackBehvaiour = new();
            
            MicrosceneGraphMetadata graphMeta = new MicrosceneGraphMetadata();
            graphMeta.cameraPosition = base.viewTransform.position;
            graphMeta.cameraScale    = base.viewTransform.scale;
            
            // Associate a node entry with graph element
            Dictionary<GraphElement, MicrosceneNodeEntry> nodesMap = new();
            List<MicrosceneNode> pooledNodeList  = new(16);
            
            int nodeID = -1;
            
            HashSet<GraphElement> pooledElementSet = new(16);

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

                if (element is MicrosceneStackNode stack)
                {
                    IConnectable connectableStack = stack;
                    pooledNodeList.Clear();

                    foreach (var stackNode in stack.Children())
                    {
                        if(stackNode is MicrosceneNodeView node)
                            pooledNodeList.Add(node.wrapper.binding as MicrosceneNode);
                    }
                    
                    int connectionsCount = CountConnectedStacks(pooledElementSet, connectableStack);

                    MicrosceneNodeEntry nodeEntry = new()
                    {
                        stackBehaviour            = (MicrosceneStackBehaviour)stack.wrapper.binding,
                        NodeID                    = ++nodeID,
                        InputPortsConnectionCount = connectionsCount,
                        nodeStack                 = pooledNodeList.ToArray(),
                    };
                    
                    nodesMap.Add(stack, nodeEntry);
                    graphMeta.stackNodes.Add(new(stack)
                    {
                        nodeID = nodeID,
                    });
                    
                    continue;
                }

                if (element is MicrosceneNodeView nodeView && nodeView.ownerStack is null && nodeView.wrapper is not null)
                {
                    pooledNodeList.Clear();
                    pooledNodeList.Add(nodeView.wrapper.binding as MicrosceneNode);
                    int connectionsCount = CountConnectedStacks(pooledElementSet, nodeView);
                    
                    MicrosceneNodeEntry nodeEntry = new()
                    {
                        stackBehaviour = defaultStackBehvaiour,
                        NodeID    = ++nodeID,
                        nodeStack = pooledNodeList.ToArray(),
                        InputPortsConnectionCount = connectionsCount,
                    };
                    
                    nodesMap.Add(nodeView, nodeEntry);
                    graphMeta.nodes.Add(new(nodeView)
                    {
                        nodeID = nodeID,
                    });
                }
            }
            
            List<MicrosceneNodeEntry> pooledConnections    = new(16);
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
                    
                    // TODO: This is stupid
                    if (entryNode.ID == "Entry")
                    {
                        graphMeta.entryPosition = entryNode.NodePosition;
                        scene.m_Root = root;
                    }
                    else
                    {
                        graphMeta.quitEntryPosition = entryNode.NodePosition;
                        scene.m_QuitRoot = root;
                    }
                }
            }
            
            scene.m_MetadataJson = EditorJsonUtility.ToJson(graphMeta, prettyPrint: true);
            scene.m_AllEntries = nodesMap.Values.ToArray();
            
            EditorUtility.SetDirty(scene);
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

                if (outputNode is MicrosceneStackNode s && pooledElementSet.Add(s))
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
    }
}
