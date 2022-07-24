using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    class MicrosceneGraphView : AutoGraphView
    {
        public EditorWindow parentWindow;

        private const string STYLE_PATH = "Packages/com.alexk.microscenes/Editor/Microscene Graph/MicrosceneGraphStyles.uss";
        private const string CONDITION_STACK_ICON_PATH = "Packages/com.alexk.microscenes/Editor/Icons/ConditionStack.png";
        private const string ACTION_STACK_ICON_PATH    = "Packages/com.alexk.microscenes/Editor/Icons/ActionStack.png";

        public MicrosceneGraphView(EditorWindow parent) : base()
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(STYLE_PATH));

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
                AddDerivedFrom<MicroAction>(nodesSearcher, "Actions/");
                AddDerivedFrom<MicroPrecondition>(nodesSearcher, "Conditions/");
                
                nodesSearcher.AddEntry("Preconditions Stack", userData: typeof(PreconditionStackNode),
                    icon: new EditorIcon(CONDITION_STACK_ICON_PATH));
                nodesSearcher.AddEntry("Actions Stack", userData: typeof(ActionStackNode), 
                    icon: new EditorIcon(ACTION_STACK_ICON_PATH));

                if(creationContext.target is null)
                {
                    nodesSearcher.AddEntry("Sticky Note", userData: typeof(StickyNote), icon: new EditorIcon("Tile Icon"));
                }
            }
            else if(creationContext.target is PreconditionStackNode precondStack)
            {
                AddDerivedFrom<MicroPrecondition>(nodesSearcher, "");
            }
            else if(creationContext.target is ActionStackNode actionStack)
            {
                AddDerivedFrom<MicroAction>(nodesSearcher, "");
            }

            nodesSearcher.Build();
            nodesSearcher.OnSelected = (entry, ctx) =>
            {
                var nodeType = entry.userData as Type;
                Node node = null;

                if (nodeType.IsSubclassOf(typeof(MicroAction)))
                {
                    var action = (MicroAction)Activator.CreateInstance(nodeType);
                    node = new ActionMicrosceneNodeView(action, this);
                }
                else if (nodeType.IsSubclassOf(typeof(MicroPrecondition)))
                {
                    var precond = (MicroPrecondition)Activator.CreateInstance(nodeType);
                    node = new PreconditionMicrosceneNodeView(precond, this);
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

        void AddDerivedFrom<T>(GenericSearchBuilder builder, string prefix)
        {
            var microprecondTypes = TypeCache.GetTypesDerivedFrom<T>();
            var ctxs = this.scene.context;

            foreach (var type in microprecondTypes)
            {
                if (type is null || type.IsAbstract)
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
            if(typeIcon != null)
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

        Microscene scene;
        SerializedObject serializedMicroscene;

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
            if(!string.IsNullOrEmpty(scene.MetadataJson_Editor))
                EditorJsonUtility.FromJsonOverwrite(scene.MetadataJson_Editor, graphMeta);

            if(graphMeta.cameraPosition != Vector3.zero)
            {
                base.UpdateViewTransform(graphMeta.cameraPosition, graphMeta.cameraScale);
            }

            var stackNodes    = new List<StackNode>(graphMeta.stackNodes.Count);
            var nodeToElement = new Dictionary<(MicrosceneNodeType type, int index), GraphElement> ();
            
            foreach (StackNodeMetadata stackMeta in graphMeta.stackNodes)
            {
                var n = stackMeta.Instantiate(this);
                stackNodes.Add(n);
                AddElement(n);
            }

            for (int i = 0; i < scene.MicroactionsArray_Editor.Length; i++)
            {
                var microaction = scene.MicroactionsArray_Editor[i];
                var meta        = graphMeta.actions[i];

                var node = new ActionMicrosceneNodeView(microaction, this);
                AddElement(node);

                meta.ApplyToNode(node, stackNodes);
                nodeToElement[(MicrosceneNodeType.Action, i)] = node;
            }

            for (int i = 0; i < scene.PreconditionsArray_Editor.Length; i++)
            {
                var precondition = scene.PreconditionsArray_Editor[i];
                var meta         = graphMeta.preconditions[i];

                var node = new PreconditionMicrosceneNodeView(precondition, this);
                AddElement(node);

                meta.ApplyToNode(node, stackNodes);
                nodeToElement[(MicrosceneNodeType.Condition, i)] = node;
            }

            foreach (var connectionIndex in scene.RootActions_Editor)
            {
                var targetNode = scene.AllNodes_Editor[connectionIndex];

                if (targetNode.nodeIndicies.Length == 0)
                    continue;

                var targetElement = nodeToElement[(targetNode.nodeType, targetNode.nodeIndicies[0])];

                if (targetElement is IConnectable c1)
                {
                    AddElement(((IConnectable)entry).ConnectOutputTo(c1));
                }
            }

            foreach (var connectionIndex in scene.RootBranch_Editor)
            {
                var targetNode = scene.AllNodes_Editor[connectionIndex];

                if (targetNode.nodeIndicies.Length == 0)
                    continue;

                var targetElement = nodeToElement[(targetNode.nodeType, targetNode.nodeIndicies[0])];

                if (targetElement is IConnectable c1)
                {
                    AddElement(((IConnectable)entry).ConnectOutputTo(c1));
                }
            }

            for (int i = 0; i < scene.AllNodes_Editor.Length; i++)
            {
                MicrosceneNode microsceneNode = scene.AllNodes_Editor[i];
                if (microsceneNode.nodeIndicies.Length == 0)
                    continue;

                var index = microsceneNode.nodeIndicies[0];
                var nodeElement = nodeToElement[(microsceneNode.nodeType, index)];
                
                if(microsceneNode.actionConnections != null)
                foreach(var connectionIndex in microsceneNode.actionConnections)
                {
                    var targetNode = scene.AllNodes_Editor[connectionIndex];
                    var targetElement = nodeToElement[(targetNode.nodeType, targetNode.nodeIndicies[0])];

                    if(nodeElement is IConnectable c0 && targetElement is IConnectable c1)
                    {
                        AddElement(c0.ConnectOutputTo(c1));
                    }
                }

                if(microsceneNode.branchConnections != null)
                foreach (var connectionIndex in microsceneNode.branchConnections)
                {
                    var targetNode = scene.AllNodes_Editor[connectionIndex];
                    var targetElement = nodeToElement[(targetNode.nodeType, targetNode.nodeIndicies[0])];

                    if (nodeElement is IConnectable c0 && targetElement is IConnectable c1)
                    {
                        AddElement(c0.ConnectOutputTo(c1));
                    }
                }
            }

            foreach (var stickyNote in graphMeta.stickyNotes)
                AddElement(stickyNote.CreateFromData());

            foreach (var node in nodes.ToList())
            {
                node.RefreshPorts();
                node.RefreshExpandedState();
            }
        }

        public void Serialize()
        {
            if (!scene)
                return;

            MicrosceneGraphMetadata graphMeta = new MicrosceneGraphMetadata();
            graphMeta.cameraPosition = base.viewTransform.position;
            graphMeta.cameraScale    = base.viewTransform.scale;
            
            List<MicroPrecondition> preconditions = new  List<MicroPrecondition>(16);
            List<MicroAction>       microactions  = new  List<MicroAction>      (16);
            List<MicrosceneNode>    nodes         = new  List<MicrosceneNode>   (16);

            Dictionary<GraphElement, int> elementToActualNode = new Dictionary<GraphElement, int>(36);
            Dictionary<int, GraphElement> nodeToOwner         = new Dictionary<int, GraphElement>(36);
             
            foreach (var element in base.graphElements.ToList())
            {
                if(element is StackNode stack)
                {
                    var index = graphMeta.stackNodes.Count;
                    graphMeta.stackNodes.Add(new StackNodeMetadata(stack));

                    foreach(var stackElement in stack.Children())
                    {
                        if (stackElement is ActionMicrosceneNodeView actionNode)
                        {
                            microactions.Add((MicroAction)actionNode.wrapper.binding);
                            graphMeta.actions.Add(new MicrosceneNodeMetadata(actionNode, index));
                        }
                        else if (stackElement is PreconditionMicrosceneNodeView precondNode)
                        {
                            preconditions.Add((MicroPrecondition)precondNode.wrapper.binding);
                            graphMeta.preconditions.Add(new MicrosceneNodeMetadata(precondNode, index));
                        }
                    }
                }
                else if(element is StickyNote note)
                {
                    graphMeta.stickyNotes.Add(new StickyNoteMetadata(note));
                }
            }

            EntryMicrosceneNode entryNode = null;
            foreach (var node in base.nodes.ToList())
            {
                if(node is ActionMicrosceneNodeView actionNode && !microactions.Contains(actionNode.binding))
                {
                    microactions.Add((MicroAction)actionNode.wrapper.binding);
                    graphMeta.actions.Add(new MicrosceneNodeMetadata(actionNode));
                }
                else if(node is PreconditionMicrosceneNodeView precondNode && !preconditions.Contains(precondNode.binding))
                {
                    preconditions.Add((MicroPrecondition)precondNode.wrapper.binding);
                    graphMeta.preconditions.Add(new MicrosceneNodeMetadata(precondNode));
                }
                else if(node is EntryMicrosceneNode _entryNode)
                {
                    graphMeta.entryPosition = node.GetPosition().position;
                    entryNode = _entryNode;
                }
            }

            var indicies = new List<byte>();

            foreach (var graphElement in graphElements.ToList())
            {
                if (graphElement is ActionStackNode actionStack)
                {
                    elementToActualNode[actionStack] = nodes.Count;
                    nodeToOwner[nodes.Count] = actionStack;

                    indicies.Clear();
                    foreach(var node in actionStack.Children())
                    {
                        ActionMicrosceneNodeView n = (ActionMicrosceneNodeView)node;
                        indicies.Add((byte)microactions.IndexOf(n.binding));
                        elementToActualNode[n] = nodes.Count;
                    }

                    nodes.Add(new MicrosceneNode()
                    {
                        nodeType = MicrosceneNodeType.Action,
                        nodeIndicies = indicies.ToArray()
                    });
                }
                else if (graphElement is PreconditionStackNode precondStack)
                {
                    elementToActualNode[precondStack] = nodes.Count;
                    nodeToOwner[nodes.Count] = precondStack;

                    indicies.Clear();
                    foreach (var node in precondStack.Children())
                    {
                        PreconditionMicrosceneNodeView n = (PreconditionMicrosceneNodeView)node;
                        indicies.Add((byte)preconditions.IndexOf(n.binding));
                        elementToActualNode[n] = nodes.Count;
                    }

                    nodes.Add(new MicrosceneNode()
                    {
                        nodeType = MicrosceneNodeType.Condition,
                        nodeIndicies = indicies.ToArray()
                    });
                }
            }

            foreach(var node in base.nodes.ToList())
            {
                if(node is GenericMicrosceneNodeView generic && !(node is EntryMicrosceneNode))
                {
                    if (elementToActualNode.ContainsKey(generic))
                        continue;

                    if (generic is ActionMicrosceneNodeView actionStack)
                    {
                        elementToActualNode[generic] = nodes.Count;
                        nodeToOwner[nodes.Count] = generic;

                        nodes.Add(new MicrosceneNode()
                        {
                            nodeType = MicrosceneNodeType.Action,
                            nodeIndicies = new byte[] { (byte)microactions.IndexOf(actionStack.binding) }
                        });
                    }
                    else if (generic is PreconditionMicrosceneNodeView precondStack)
                    {
                        elementToActualNode[generic] = nodes.Count;
                        nodeToOwner[nodes.Count] = generic;

                        nodes.Add(new MicrosceneNode()
                        {
                            nodeType = MicrosceneNodeType.Condition,
                            nodeIndicies = new byte[] { (byte)preconditions.IndexOf(precondStack.binding) }
                        });
                    }
                }
            }

            List<int> indiciesInt  = new List<int>();
            List<int> indiciesInt2 = new List<int>();

            {
                var rootPort = entryNode.outputContainer.Q<Port>();

                foreach(var connection in rootPort.connections)
                {
                    var node = connection.input.node;
                    if(elementToActualNode.TryGetValue(node, out var index))
                    {
                        if (nodes[index].nodeType == MicrosceneNodeType.Action)
                            indiciesInt.Add(index);
                        else
                            indiciesInt2.Add(index);
                    }
                }

                scene.RootActions_Editor = indiciesInt.ToArray();
                scene.RootBranch_Editor  = indiciesInt2.ToArray();
            }

            HashSet<GraphElement> processed = new HashSet<GraphElement>();
            foreach(var graphElement in graphElements.ToList())
            {
                if(elementToActualNode.TryGetValue(graphElement, out var myNodeIndex))
                {
                    var owner = nodeToOwner[myNodeIndex] as IConnectable;
                    if (owner is null || processed.Contains(graphElement))
                        continue;

                    processed.Add(graphElement);
                    indiciesInt.Clear(); 
                    indiciesInt2.Clear();

                    foreach (var edge in owner.OutputEdges())
                    {
                        if (edge is null || edge.input is null || edge.input.node is null)
                            continue; 

                        if(elementToActualNode.TryGetValue(edge.input.node, out var connectionNodeIndex))
                        {
                            if ((nodes[connectionNodeIndex].nodeType == MicrosceneNodeType.Action) && !indiciesInt.Contains(connectionNodeIndex))
                                indiciesInt.Add(connectionNodeIndex);
                            else if(!indiciesInt2.Contains(connectionNodeIndex))
                                indiciesInt2.Add(connectionNodeIndex);
                        }
                    }

                    var node = nodes[myNodeIndex];
                    node.actionConnections = indiciesInt.ToArray();
                    node.branchConnections = indiciesInt2.ToArray();
                    nodes[myNodeIndex] = node;
                }
            }

            scene.MicroactionsArray_Editor  = microactions.ToArray();
            scene.PreconditionsArray_Editor = preconditions.ToArray();
            scene.AllNodes_Editor = nodes.ToArray();

            scene.MetadataJson_Editor = EditorJsonUtility.ToJson(graphMeta, prettyPrint: true);

            EditorUtility.SetDirty(scene);
            serializedMicroscene.Update();
            serializedMicroscene.ApplyModifiedProperties();
        }
    }
}
