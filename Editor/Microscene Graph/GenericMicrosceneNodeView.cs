using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    internal class GenericMicrosceneNodeView : Node, IConnectable
    {
        public GenericMicrosceneNodeView(GraphView view) : base()
        {
            var label = titleContainer.Q<Label>();
            this.view = view;
            titleContainer.Remove(label);

            image = new Image();
            image.style.paddingLeft = 5;
            var ve = new VisualElement();
            ve.style.flexDirection = FlexDirection.Row;
            ve.Add(image);
            ve.Add(label);

            titleContainer.Insert(0, ve);
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

        Port IConnectable.input  => inputContainer.Q<Port>();
        Port IConnectable.output => outputContainer.Q<Port>();

        public void SetIcon(Type t)
        {
            var typeIcon = t.GetCustomAttribute<TypeIconAttribute>(inherit: true);
            if (typeIcon != null)
            {if (typeIcon.Type is null)
                {
                    icon = new EditorIcon(typeIcon.Name);
                }
                else icon = new EditorIcon(typeIcon.Type);
            }
            else
            {
                icon = null;
            }
        }

        public string NameForNode(Type t)
        {
            var name = t.Name.Replace("Action", "").Replace("Precondition", "");

            return ObjectNames.NicifyVariableName(name);
        }

        public static VisualElement CreateDivider()
        {
            var ve = new VisualElement() { name="divider" };
            ve.AddToClassList("horizontal");

            return ve;
        }

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
            return ConnectInputTo(connectable.output);
        }

        public Edge ConnectOutputTo(IConnectable connectable)
        {
            return ConnectOutputTo(connectable.input);
        }
    }
}
