using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    internal class MicrosceneGraphView : AutoGraphView
    {
        private Microscene scene;
        private SerializedObject serializedMicroscene;

        public EditorWindow parentWindow;
        
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

        void CreateNode(NodeCreationContext creationContext)
        {
            nodesSearcher.ClearEntries();

            if (!(creationContext.target is StackNode))
            {
                AddDerivedFrom<MicrosceneNode>(nodesSearcher, "Hybdrids/",   MicrosceneNodeType.Hybrid);
                AddDerivedFrom<MicrosceneNode>(nodesSearcher, "Actions/",    MicrosceneNodeType.Action);
                AddDerivedFrom<MicrosceneNode>(nodesSearcher, "Conditions/", MicrosceneNodeType.Precondition);
                
                nodesSearcher.AddEntry("Preconditions Stack", userData: typeof(PreconditionStackNode),
                    icon: new EditorIcon(MicrosceneGraphViewResources.CONDITION_STACK_ICON_PATH));
                nodesSearcher.AddEntry("Actions Stack", userData: typeof(ActionStackNode), 
                    icon: new EditorIcon(MicrosceneGraphViewResources.ACTION_STACK_ICON_PATH));

                if(creationContext.target is null)
                {
                    nodesSearcher.AddEntry("Sticky Note", userData: typeof(StickyNote), icon: new EditorIcon("Tile Icon"));
                }
            }
            else if(creationContext.target is PreconditionStackNode precondStack)
            {
                AddDerivedFrom<MicrosceneNode>(nodesSearcher, "", MicrosceneNodeType.Precondition);
            }
            else if(creationContext.target is ActionStackNode actionStack)
            {
                AddDerivedFrom<MicrosceneNode>(nodesSearcher, "", MicrosceneNodeType.Action);
            }

            nodesSearcher.Build();
            nodesSearcher.OnSelected = (entry, ctx) =>
            {
                var nodeType = entry.userData as Type;
                Node node = null;

                if (nodeType.IsSubclassOf(typeof(MicrosceneNode)))
                {
                    var action = (MicrosceneNode)Activator.CreateInstance(nodeType);
                    node = new MicrosceneNodeView(action, this);
                }
                else if(nodeType == typeof(ActionStackNode))
                {
                    var actionStack = new ActionStackNode(this);

                    node = actionStack;
                }
                else if(nodeType == typeof(PreconditionStackNode))
                {
                    var precondStack = new PreconditionStackNode(this);

                    node = precondStack;
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

                node.SetGraphPosition(graphMousePosition);

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

        void AddDerivedFrom<T>(GenericSearchBuilder builder, string prefix, MicrosceneNodeType typeCapability)
        {
            var microprecondTypes = TypeCache.GetTypesDerivedFrom<T>();
            var ctxs = this.scene.context;

            foreach (var type in microprecondTypes)
            {
                if (type is null || type.IsAbstract)
                    continue;

                var nodeAttr = type.GetCustomAttribute<MicrosceneNodeTypeAttribute>(inherit: true);
                if (nodeAttr is null || (nodeAttr.NodeTypeCapabilities & typeCapability) != typeCapability)
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
            if (typeIcon != null)
            {
                if (typeIcon.Type is null)
                {
                    return new EditorIcon(typeIcon.Name);
                }
                else return new EditorIcon(typeIcon.Type);
            }
            else
            {
                return null;
            }
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

            var entry = new EntryMicrosceneNode(this);
            AddElement(entry);

            MicrosceneGraphMetadata graphMeta = new MicrosceneGraphMetadata();
            if (!string.IsNullOrEmpty(scene.MetadataJson_Editor))
                EditorJsonUtility.FromJsonOverwrite(scene.MetadataJson_Editor, graphMeta);

            if (graphMeta.cameraPosition != Vector3.zero)
            {
                base.UpdateViewTransform(graphMeta.cameraPosition, graphMeta.cameraScale);
            }

            var stackNodes    = new List<StackNode>(graphMeta.stackNodes.Count);
            var nodeToElement = new Dictionary<int, GraphElement> ();

            // First we restore all stack nodes
            foreach (StackNodeMetadata stackMeta in graphMeta.stackNodes)
            {
                var n = stackMeta.Instantiate(this);
                stackNodes.Add(n);
                AddElement(n);
            }

            // Then all nodes
            for (int i = 0; i < scene.Nodes_Editor.Length; i++)
            {
                var micronode = scene.Nodes_Editor[i];
                var meta      = graphMeta.nodes[i];

                var node = new MicrosceneNodeView(micronode, this);
                AddElement(node);

                meta.ApplyToNode(node, stackNodes); // This adds node to a stack if needed
                nodeToElement[i] = node; // Also remember what index corresponds to which node
            }

            ConnectRootNodeToItsOutputs(scene, entry, nodeToElement, scene.RootActions_Editor, MicrosceneNodeType.Action);
            ConnectRootNodeToItsOutputs(scene, entry, nodeToElement, scene.RootBranch_Editor,  MicrosceneNodeType.Precondition);

            // Doing same thing but now for all nodes instead of just root ones
            for (int i = 0; i < scene.AllNodes_Editor.Length; i++)
            {
                MicrosceneNodeData microsceneNode = scene.AllNodes_Editor[i];
                if (microsceneNode.myNodeStack is null || microsceneNode.myNodeStack.Length == 0)
                    continue;

                var targetNodeIndex = microsceneNode.myNodeStack[0];
                var nodeViewElement = nodeToElement[targetNodeIndex];

                ConnectNodeToItsOutputs(scene, nodeToElement, nodeViewElement, microsceneNode.actionConnections, MicrosceneNodeType.Action);
                ConnectNodeToItsOutputs(scene, nodeToElement, nodeViewElement, microsceneNode.branchConnections, MicrosceneNodeType.Precondition);
            }

            foreach (var stickyNote in graphMeta.stickyNotes)
                AddElement(stickyNote.CreateFromData());

            foreach (var node in nodes.ToList())
            {
                node.RefreshPorts();
                node.RefreshExpandedState();
            }
        }

        private void ConnectRootNodeToItsOutputs(Microscene scene, EntryMicrosceneNode entry, 
                                                 Dictionary<int, GraphElement> nodeToElement, 
                                                 int[] rootBranch_Editor, MicrosceneNodeType expectedType)
        {
            // We go through each connection
            foreach (var connectionIndex in rootBranch_Editor)
            {
                var targetNode = scene.AllNodes_Editor[connectionIndex];

                if (targetNode.myNodeStack is null || targetNode.myNodeStack.Length == 0)
                    continue;

                // And find node it's supposed to connect to using previously generated dictionary
                // We only need first one because if there are more nodeIndicies than one it means they are in a stack
                // And if they are in a stack, IConnectable will connect to a stack
                var targetElement = nodeToElement[targetNode.myNodeStack[0]];

                if (targetElement is MicrosceneNodeView nodeView)
                    nodeView.TrySetActualType(expectedType); // First set type then connect, otherwise ports break

                if (targetElement is IConnectable c1)
                {
                    AddElement(((IConnectable)entry).ConnectOutputTo(c1));
                }
            }
        }

        private void ConnectNodeToItsOutputs(Microscene scene, Dictionary<int, GraphElement> nodeToElement, 
                                             GraphElement nodeViewElement, int[] connectionIndicies, MicrosceneNodeType expectedType)
        {
            if (connectionIndicies != null)
            {
                foreach (var connectionIndex in connectionIndicies)
                {
                    var targetNode = scene.AllNodes_Editor[connectionIndex];
                    var targetElement = nodeToElement[targetNode.myNodeStack[0]];

                    if (targetElement is MicrosceneNodeView nodeView)
                        nodeView.TrySetActualType(expectedType);

                    if (nodeViewElement is IConnectable c0 && targetElement is IConnectable c1)
                    {
                        AddElement(c0.ConnectOutputTo(c1));
                    }
                }
            }
        }

        public void Serialize()
        {
            if (!scene)
                return;

            MicrosceneGraphMetadata graphMeta = new MicrosceneGraphMetadata();
            graphMeta.cameraPosition = base.viewTransform.position;
            graphMeta.cameraScale    = base.viewTransform.scale;
            
            List<MicrosceneNode>     nodes    = new List<MicrosceneNode>    (16);
            List<MicrosceneNodeData> nodeData = new List<MicrosceneNodeData>(16);

            Dictionary<GraphElement, int> elementToActualNode = new Dictionary<GraphElement, int>(36);
            Dictionary<int, GraphElement> nodeToOwner         = new Dictionary<int, GraphElement>(36);  // Microscene node index points to graph element that owns it (stack if multiple)
            
            // First we save all stack nodes and nodes that are their children
            foreach (var element in base.graphElements.ToList())
            {
                if(element is StackNode stack)
                {
                    var index = graphMeta.stackNodes.Count;
                    graphMeta.stackNodes.Add(new StackNodeMetadata(stack));

                    foreach(var stackElement in stack.Children())
                    {
                        if (stackElement is MicrosceneNodeView actionNode)
                        {
                            nodes.Add((MicrosceneNode)actionNode.wrapper.binding);
                            graphMeta.nodes.Add(new MicrosceneNodeMetadata(actionNode, index)); // We remember which stack node node belongs to
                        }
                    }
                }
                else if(element is StickyNote note)
                {
                    graphMeta.stickyNotes.Add(new StickyNoteMetadata(note));
                }
            }

            // Then add all metadata for nodes that are not part of any stack + entry
            EntryMicrosceneNode entryNode = null;
            foreach (var node in base.nodes.ToList())
            {
                if(node is MicrosceneNodeView actionNode && !nodes.Contains(actionNode.binding))
                {
                    nodes.Add((MicrosceneNode)actionNode.wrapper.binding);
                    graphMeta.nodes.Add(new MicrosceneNodeMetadata(actionNode));
                }
                else if(node is EntryMicrosceneNode _entryNode)
                {
                    graphMeta.entryPosition = node.GetPosition().position;
                    entryNode = _entryNode;
                }
            }

            var indicies = new List<int>();

            foreach (var graphElement in graphElements.ToList())
            {
                SerializeStackNode<ActionStackNode, MicrosceneNodeView, MicrosceneNode>
                    (nodes, nodeData, elementToActualNode, 
                     nodeToOwner, indicies, graphElement, MicrosceneNodeType.Action);

                SerializeStackNode<PreconditionStackNode, MicrosceneNodeView, MicrosceneNode>
                    (nodes, nodeData, elementToActualNode,
                     nodeToOwner, indicies, graphElement, MicrosceneNodeType.Precondition);
            }

            foreach (var node in base.nodes.ToList())
            {
                if(node is GenericMicrosceneNodeView generic && !(node is EntryMicrosceneNode))
                {
                    if (elementToActualNode.ContainsKey(generic))
                        continue;

                    if (generic is MicrosceneNodeView nodeView)
                    {
                        elementToActualNode[generic] = nodeData.Count;
                        nodeToOwner[nodeData.Count] = generic;

                        nodeData.Add(new MicrosceneNodeData()
                        {
                            nodeType    = nodeView.ActualType,
                            myNodeStack = new int[] { nodes.IndexOf(nodeView.binding) }
                        });
                    }
                }
            }

            List<int> actionNodeConnections = new List<int>();
            List<int> branchNodeConnections = new List<int>();

            {
                var rootPort = entryNode.outputContainer.Q<Port>();

                foreach(var connection in rootPort.connections)
                {
                    var node = connection.input.node;
                    if (node is null)
                        continue;

                    if(elementToActualNode.TryGetValue(node, out var index))
                    {
                        if (nodeData[index].nodeType == MicrosceneNodeType.Action)
                            actionNodeConnections.Add(index);
                        else
                            branchNodeConnections.Add(index);
                    }
                }

                scene.RootActions_Editor = actionNodeConnections.ToArray();
                scene.RootBranch_Editor  = branchNodeConnections.ToArray();
            }

            // Here we find all connections and store them
            HashSet<GraphElement> processed = new HashSet<GraphElement>();
            foreach(var graphElement in graphElements.ToList())
            {
                if(elementToActualNode.TryGetValue(graphElement, out var myNodeIndex))
                {
                    var owner = nodeToOwner[myNodeIndex] as IConnectable;
                    if (owner is null || processed.Contains(graphElement))
                        continue;

                    processed.Add(graphElement);
                    actionNodeConnections.Clear(); 
                    branchNodeConnections.Clear();

                    foreach (var edge in owner.OutputEdges())
                    {
                        if (edge is null || edge.input is null || edge.input.node is null)
                            continue; 

                        if(elementToActualNode.TryGetValue(edge.input.node, out var connectionNodeIndex))
                        {
                            if ((nodeData[connectionNodeIndex].nodeType == MicrosceneNodeType.Action) && !actionNodeConnections.Contains(connectionNodeIndex))
                                actionNodeConnections.Add(connectionNodeIndex);
                            else if(!branchNodeConnections.Contains(connectionNodeIndex))
                                branchNodeConnections.Add(connectionNodeIndex);
                        }
                    }

                    var node = nodeData[myNodeIndex];
                    node.actionConnections = actionNodeConnections.ToArray();
                    node.branchConnections = branchNodeConnections.ToArray();
                    nodeData[myNodeIndex] = node;
                }
            }

            scene.Nodes_Editor    = nodes.ToArray();
            scene.AllNodes_Editor = nodeData.ToArray();

            scene.MetadataJson_Editor = EditorJsonUtility.ToJson(graphMeta, prettyPrint: true);

            EditorUtility.SetDirty(scene);
            serializedMicroscene.Update();
            serializedMicroscene.ApplyModifiedProperties();
        }

        private static void SerializeStackNode<TStack, TNode, TRealType>(List<TRealType> serializedNodeData, List<MicrosceneNodeData> nodes, 
                                                                         Dictionary<GraphElement, int> elementToActualNode, Dictionary<int, GraphElement> nodeToOwner, 
                                                                         List<int> indicies, GraphElement graphElement, MicrosceneNodeType nodeType)
            where TStack : StackNode
            where TNode  : GenericMicrosceneNodeView<TRealType>
        {
            if (graphElement is TStack actionStack)
            {
                elementToActualNode[actionStack] = nodes.Count;
                nodeToOwner[nodes.Count] = actionStack;

                indicies.Clear();
                foreach (var node in actionStack.Children())
                {
                    var n = (TNode)node;
                    indicies.Add(serializedNodeData.IndexOf(n.binding));
                    elementToActualNode[n] = nodes.Count;
                }

                nodes.Add(new MicrosceneNodeData()
                {
                    nodeType    = nodeType,
                    myNodeStack = indicies.ToArray()
                });
            }
        }
    }
}
