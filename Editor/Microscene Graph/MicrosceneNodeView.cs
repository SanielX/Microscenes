using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    internal class MicrosceneNodeView : GenericMicrosceneNodeView<MicrosceneNode>
    {
        public MicrosceneNodeView(MicrosceneNode binding, GraphView view) : base(binding, view)
        {
            var nodeAttribute = binding.GetType().GetCustomAttribute<MicrosceneNodeTypeAttribute>();
            //divider = this.Q<VisualElement>("contents").Q<VisualElement>("divider");
            divider = this.titleContainer;

            NodeTypeCapabilities = nodeAttribute.NodeTypeCapabilities;

            if ((NodeTypeCapabilities & MicrosceneNodeType.Action) != 0)
                ActualType = MicrosceneNodeType.Action;
            else
                ActualType = MicrosceneNodeType.Precondition;

        }

        public void TrySetActualType(MicrosceneNodeType type)
        {
            if ((NodeTypeCapabilities & type) != 0)
                ActualType = type;
        }

        public VisualElement divider { get; }
        public Color dividerColor { get => divider.style.backgroundColor.value; set => divider.style.backgroundColor = value; }

        private MicrosceneNodeType _actualType;
        public MicrosceneNodeType ActualType
        {
            get => _actualType;
            private set
            {
                _actualType = value;

                if (ownerStack is null)
                {
                    RemovePorts();
                    AddPorts(view);
                }

                // Color finalDividerColor = default;
                // switch (ActualType)
                // {
                // case MicrosceneNodeType.Precondition:
                //     finalDividerColor = ColorUtils.FromHEX(0x233838);
                //     finalDividerColor.a = 1f;
                //     break;
                // case MicrosceneNodeType.Action:
                //     finalDividerColor = ColorUtils.FromHEX(0x313F32);
                //     finalDividerColor.a = 1f;
                //     break;
                // case MicrosceneNodeType.Hybrid:
                //     finalDividerColor = ColorUtils.FromHEX(0x68A3F7);
                //     finalDividerColor.a = 1f;
                //     break;
                // }
                // finalDividerColor.a = 0.8f;
                // dividerColor = finalDividerColor;
                // divider.style.height = 2;
            }
        }
        public MicrosceneNodeType NodeTypeCapabilities { get; }

        protected override void CreatePorts(GraphView view, out AutoPort inputPort, out AutoPort outputPort)
        {
            base.CreatePorts(view, out inputPort, out outputPort);

            if (ActualType == MicrosceneNodeType.Action)
            {
                inputPort.portColor  = ColorUtils.FromHEX(0xC9F774);
                outputPort.portColor = ColorUtils.FromHEX(0x26D9D9);
            }
            else
            {
                inputPort.portColor  = ColorUtils.FromHEX(0x26D9D9);
                outputPort.portColor = ColorUtils.FromHEX(0xC9F774);
            }

            if (NodeTypeCapabilities == MicrosceneNodeType.Hybrid)
                inputPort.portName = "Hybrid";
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (NodeTypeCapabilities == MicrosceneNodeType.Hybrid && ownerStack is null)
            {
                if(ActualType == MicrosceneNodeType.Precondition)
                    evt.menu.AppendAction("Make Action", (_) => ActualType = MicrosceneNodeType.Action);
                else 
                    evt.menu.AppendAction("Make Condition", (_) => ActualType = MicrosceneNodeType.Precondition);
            }
        }

        public override void OnAddedToStack(StackNode node, int index)
        {
            base.OnAddedToStack(node, index);
            if (node is ActionStackNode)
                ActualType = MicrosceneNodeType.Action;
            else if(node is PreconditionStackNode)
                ActualType = MicrosceneNodeType.Precondition;
        }
    }
}
