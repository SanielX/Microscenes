using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    internal class MicrosceneNodeView : Node, IConnectable, IStackListener, INodeWithEditor
    {
        public int  NodeID       { get; set; }
        public Rect NodePosition { get; set; }
        
        public MicrosceneNodeView(GraphView view) : base()
        {
            var label = titleContainer.Q<Label>();
            this.view = view;
            titleContainer.Remove(label);

            image = new Image();
            image.style.paddingLeft = 5;
            image.style.maxWidth = 16 + 5;
            
            var ve = new VisualElement();
            ve.style.flexDirection = FlexDirection.Row;
            ve.Add(image);
            ve.Add(label);

            titleContainer.Insert(0, ve);
            
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

        public MicrosceneNodeView(MicrosceneNode binding, GraphView view) : this(view)
        {
            this.wrapper = ScriptableObject.CreateInstance<ScriptableWrapper>();
            this.wrapper.binding = binding;
            wrapperSerialized = new SerializedObject(wrapper);

            IconsProvider.Instance.GetIconAsync(binding.GetType(), (tex) => icon = tex);
            title = NameForNode(binding.GetType());
            
            MicrosceneNodeAttribute nodeAttribute = binding.GetType().GetCustomAttribute<MicrosceneNodeAttribute>();
            if(nodeAttribute is not null)
                tooltip = nodeAttribute.Tooltip;
            
            AddPorts(view);

            VisualElement container = topContainer.parent;
            container.Add(CreateDivider()); 
            
            if(wrapper.binding is INameableNode nameable)
            {
                title = "";
                RefreshTitle(nameable);
            }

            imgui = new IMGUIContainer(() =>
            {
                DoGUI(100);
                
                if(ownerStack is not null)
                    EditorGUILayout.Space(); // Need this so controls won't overlap when inside stack
            });
            imgui.name = "node-imgui";
            imgui.AddToClassList("imgui-container");
            imgui.AddToClassList("microscene-node-imgui-container");
            
            titleContainer.AddToClassList("microscene-title-container");
            container.Add(imgui);
        }

        public MicrosceneNode binding => (MicrosceneNode)wrapper.binding;
        
        public IMGUIContainer imgui;
        public StackNode      ownerStack;

        /// <summary>
        /// Allows to create SerializedObject from it to easily draw SerializedReference editor!
        /// </summary>
        public ScriptableWrapper wrapper;
        public SerializedObject  wrapperSerialized;

        public override void OnSelected()
        {
            ((MicrosceneGraphView)view)?.OnSelectedElement?.Invoke(this);
        }

        protected GraphView view;
        protected Image image;

        public Texture icon
        {
            get
            {
                return image.image;
            }
            set
            {
                image.image = value;
            }
        }


        protected void AddPorts(GraphView view)
        {
            var inputPort  = AutoPort.Create<Edge>(Orientation.Horizontal, Direction.Input,  Port.Capacity.Multi, typeof(Microscene), view, true);
            var outputPort = AutoPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(Microscene), view, true);

            outputPort.portColor = inputPort.portColor  = ColorUtils.FromHEX(0xC9F774);
            // outputPort.portColor = ColorUtils.FromHEX(0x26D9D9);

            inputContainer.Add(inputPort);
            outputContainer.Add(outputPort);

            RefreshPorts();
            RefreshExpandedState();
            CountConnections();
            
            inputPort.OnConnected += CountConnections;
            inputPort.OnDisconnected += CountConnections;

            inputPort.OnConnected += UnfreezeExpandButton;
            inputPort.OnDisconnected += UnfreezeExpandButton;

            outputPort.OnConnected += UnfreezeExpandButton;
            outputPort.OnDisconnected += UnfreezeExpandButton;
        }

        public void SetIcon(Type t)
        {
        }
        
        public void DoGUI(float guiLabelWidth)
        {
            if(wrapperSerialized is null)
                return;
            
            if(!wrapperSerialized.targetObject)
                return;
            
            wrapperSerialized.Update();
            var prop = wrapperSerialized.FindProperty("binding");
            if (prop is null)
                return;

            var height = SerializeReferenceUIDrawer.CalculateSPropertyHeight(prop);
            if (height == 0)
                return;

            EditorGUILayout.Space(2);

            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = guiLabelWidth;
            var rect = EditorGUILayout.GetControlRect(true, height, GUILayout.MinWidth(100));

            EditorGUI.BeginChangeCheck();
            SerializeReferenceUIDrawer.DrawSPropertyGUI(rect, prop);

            if (Event.current.type == EventType.Repaint && wrapper.binding is INameableNode nameable)
            {
                RefreshTitle(nameable);
            }

            if (EditorGUI.EndChangeCheck())
            {
                prop.serializedObject.ApplyModifiedProperties();
            }

            EditorGUIUtility.labelWidth = labelWidth;
        }

        public string NameForNode(Type t)
        {
            var name = t.Name.Replace("Action", "").Replace("Precondition", "").Replace("Node", "");

            return ObjectNames.NicifyVariableName(name).Trim();
        }

        public static VisualElement CreateDivider()
        {
            var ve = new VisualElement() { name="divider" };
            ve.AddToClassList("horizontal");

            return ve;
        }

        public Port input  => inputContainer.Q<Port>();
        Port IConnectable.output => outputContainer.Q<Port>();
        
        public Edge ConnectInputTo(Port p)
        {
            return inputContainer.Q<Port>().ConnectTo(p);
        }

        public Edge ConnectOutputTo(Port p)
        {
            return outputContainer.Q<Port>().ConnectTo(p);
        }

        public IEnumerable<Edge> OutputEdges()
        {
            return outputContainer.Q<Port>().connections;
        }

        public Edge ConnectInputTo(IConnectable connectable)
        {
            return ((IConnectable)this).ConnectInputTo(connectable.output);
        }

        public Edge ConnectOutputTo(IConnectable connectable)
        {
            return ((IConnectable)this).ConnectOutputTo(connectable.input);
        }

        // Fucking with internal unity API to make fold/unfold button work properly
        static PropertyInfo pseudoState = typeof(VisualElement).GetProperty("pseudoStates", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        public override bool expanded
        {
            get => base.expanded;
            set
            {
                if (base.expanded == value)
                    return;

                base.expanded = value;
                UnfreezeExpandButton();

                VisualElement container = topContainer.parent;
                if (value) 
                    container.Add(imgui);
                else
                    container.Remove(imgui);
            }
        }

        void CountConnections()
        {
            connectedPorts = MicrosceneGraphView.CountConnectedStacks(new(), this);
        }
        
        public int connectedPorts { get; private set; }

        protected void UnfreezeExpandButton()
        {
            var collapseButton = this.Q("collapse-button");
            var state = (PseudoStates)(int)pseudoState.GetValue(collapseButton);
            state &= ~PseudoStates.Disabled;

            pseudoState.SetValue(collapseButton, (int)state);
        }

        internal enum PseudoStates
        {
            Active = 0x1,
            Hover = 0x2,
            Checked = 0x8,
            Disabled = 0x20,
            Focus = 0x40,
            Root = 0x80
        }

        public void OnAddedToStack(StackNode node, int index)
        {
            if (node is MicrosceneStackNode stack)
            {
                ownerStack = node;

                if (stack.ConnectionType == MicrosceneStackConnectionType.SingleOutput)
                {
                    foreach (var port in outputContainer.Query<Port>().Build())
                    {
                        port.DisconnectAll();
                        outputContainer.Remove(port);
                    }
                }
            
                foreach (var port in inputContainer.Query<Port>().Build())
                {
                    port.DisconnectAll();
                    inputContainer.Remove(port);
                }

                RefreshPorts();
                RefreshExpandedState();
            }
        }

        public void OnRemovedFromStack(StackNode node)
        {
            if (node is MicrosceneStackNode stack)
            {
                ownerStack = null;

                var inputPort = AutoPort.Create<Edge>(Orientation.Horizontal,
                    Direction.Input,  Port.Capacity.Multi,
                    typeof(Microscene), view, true);

                if (stack.ConnectionType == MicrosceneStackConnectionType.SingleOutput)
                {
                    var outputPort = AutoPort.Create<Edge>(Orientation.Horizontal, Direction.Output,
                        Port.Capacity.Multi, typeof(Microscene),
                        view, true);
                    outputContainer.Add(outputPort);
                }

                inputContainer.Add(inputPort);

                RefreshPorts();
                RefreshExpandedState();

                inputPort.OnConnected += UnfreezeExpandButton;
                inputPort.OnDisconnected += UnfreezeExpandButton;
            }
        }

        void RefreshTitle(INameableNode nameable)
        {
            try
            {
                title = nameable.GetNiceNameString();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
