using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    internal class GenericMicrosceneNodeView<T> : GenericMicrosceneNodeView, IStackListener, IConnectable, IResizable
    {
        private IMGUIContainer imgui;
        private StackNode      ownerStack;

        /// <summary>
        /// Allows to create SerializedObject from it to easily draw SerializedReference editor!
        /// </summary>
        public ScriptableWrapper wrapper;
        public SerializedObject  wrapperSerialized;

        public T binding => (T)wrapper.binding;

        public GenericMicrosceneNodeView(T binding, GraphView view) : base(view)
        {
            this.wrapper = ScriptableObject.CreateInstance<ScriptableWrapper>();
            this.wrapper.binding = binding;
            wrapperSerialized = new SerializedObject(wrapper);

            SetIcon(binding.GetType());
            title = NameForNode(binding.GetType());
            AddPorts(view);

            VisualElement container = topContainer.parent;
            container.Add(CreateDivider()); 
            
            if(wrapper.binding is INameableNode nameable)
            {
                title = "";
                title = nameable.GetNiceNameString();
            }

            imgui = new IMGUIContainer(() =>
            {
                var prop = wrapperSerialized.FindProperty("binding");
                if (prop is null)
                    return;

                var height = SerializeReferenceUIDrawer.CalculateSPropertyHeight(prop);
                if(height == 0)
                    return;

                EditorGUILayout.Space(2);

                var labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 100;
                var rect = EditorGUILayout.GetControlRect(true, height, GUILayout.MinWidth(220));

                EditorGUI.BeginChangeCheck();
                SerializeReferenceUIDrawer.DrawSPropertyGUI(rect, prop);

                if (Event.current.type == EventType.Repaint && wrapper.binding is INameableNode nameable)
                    title = nameable.GetNiceNameString();

                if (EditorGUI.EndChangeCheck())
                {
                    prop.serializedObject.ApplyModifiedProperties();
                }

                EditorGUIUtility.labelWidth = labelWidth;
            });

            imgui.AddToClassList("imgui-container");
            container.Add(imgui);
        }

        protected void AddPorts(GraphView view)
        {
            AutoPort inputPort, outputPort;
            CreatePorts(view, out inputPort, out outputPort);

            inputContainer.Add(inputPort);
            outputContainer.Add(outputPort);

            RefreshPorts();
            RefreshExpandedState();

            inputPort.OnConnected += UnfreezeExpandButton;
            inputPort.OnDisconnected += UnfreezeExpandButton;

            outputPort.OnConnected += UnfreezeExpandButton;
            outputPort.OnDisconnected += UnfreezeExpandButton;
        }

        protected virtual void CreatePorts(GraphView view, out AutoPort inputPort, out AutoPort outputPort)
        {
            inputPort  = AutoPort.Create<Edge>(Orientation.Horizontal, Direction.Input,  Port.Capacity.Multi, typeof(Microscene), view, true);
            outputPort = AutoPort.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(Microscene), view, true);
        }

        public void OnAddedToStack(StackNode node, int index)
        {
            ownerStack = node;
            RemovePorts();
        }

        public void OnRemovedFromStack(StackNode node)
        {
            ownerStack = null;
            AddPorts(view);
        }

        Port IConnectable.input
        {
            get
            {
                if (ownerStack != null && ownerStack is IConnectable connectable)
                {
                    return connectable.input;
                }

                return inputContainer.Q<Port>();
            }
        }

        Port IConnectable.output
        {
            get
            {
                if (ownerStack != null && ownerStack is IConnectable connectable)
                {
                    return connectable.output;
                }

                return outputContainer.Q<Port>();
            }
        }

        Edge IConnectable.ConnectInputTo(Port p)
        {
            if (ownerStack != null && ownerStack is IConnectable connectable)
            {
                return connectable.ConnectInputTo(p);
            }

            return inputContainer.Q<Port>().ConnectTo(p);
        }

        Edge IConnectable.ConnectOutputTo(Port p)
        {
            if (ownerStack != null && ownerStack is IConnectable connectable)
            {
                return connectable.ConnectOutputTo(p);
            }

            return outputContainer.Q<Port>().ConnectTo(p);
        }

        IEnumerable<Edge> IConnectable.OutputEdges()
        {
            if (ownerStack != null && ownerStack is IConnectable connectable)
            {
                return connectable.OutputEdges();
            }

            return outputContainer.Q<Port>().connections;
        }

        protected void RemovePorts()
        {
            foreach (var port in inputContainer.Query<Port>().Build().ToList())
            {
                port.DisconnectAll();
                inputContainer.Remove(port);
            }

            foreach (var port in outputContainer.Query<Port>().Build().ToList())
            {
                port.DisconnectAll();
                outputContainer.Remove(port);
            }

            RefreshPorts();
            RefreshExpandedState();
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

        private void UnfreezeExpandButton()
        {
            var collapseButton = this.Q("collapse-button");
            var state = (PseudoStates)(int)pseudoState.GetValue(collapseButton);
            state &= ~PseudoStates.Disabled;

            pseudoState.SetValue(collapseButton, (int)state);
        }

        public void OnStartResize()
        {
        }

        public void OnResized()
        {
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
    }
}
