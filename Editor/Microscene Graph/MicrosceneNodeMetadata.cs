using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Microscenes.Editor
{
    [Serializable]
    struct MicrosceneNodeMetadata
    {
        internal MicrosceneNodeMetadata(GenericMicrosceneNodeView node, int stackNode = -1)
        {
            stackNodeIndex = stackNode;
            position       = node.GetPosition().position;
            expanded       = node.expanded;
        }

        public void ApplyToNode(GenericMicrosceneNodeView node, List<StackNode> stackNodes)
        {
            node.SetGraphPosition(position);
            node.expanded = expanded;

            if(stackNodeIndex >= 0 && stackNodeIndex < stackNodes.Count)
            {
                stackNodes[stackNodeIndex].AddElement(node);
                if (node is IStackListener listener)
                    listener.OnAddedToStack(stackNodes[stackNodeIndex], 0);
            }
        }

        public int     stackNodeIndex;
        public Vector2 position;
        public bool    expanded;
    }
}
