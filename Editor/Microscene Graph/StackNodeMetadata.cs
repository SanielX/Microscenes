using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Microscenes.Editor
{
    [Serializable]
    struct StackNodeMetadata
    {
        public StackNodeMetadata(StackNode node)
        {
            if (node is ActionStackNode actionStack)
                type = MicrosceneNodeType.Action;
            else
                type = MicrosceneNodeType.Precondition;

            position = node.GetPosition().position;
        }

        public StackNode Instantiate(GraphView view)
        {
            StackNode stackNode;
            if(type == MicrosceneNodeType.Precondition)
            {
                stackNode = new PreconditionStackNode(view);
            }
            else
            {
                stackNode = new ActionStackNode(view);
            }

            stackNode.SetGraphPosition(position);
            return stackNode;
        }

        public MicrosceneNodeType type;
        public Vector2            position;
    }
}
