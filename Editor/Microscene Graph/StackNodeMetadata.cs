using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    [Serializable]
    struct StackNodeMetadata
    {
        public StackNodeMetadata(MicrosceneStackNode node) : this()
        {
            position = node.NodePosition;
        }
        
        public int  nodeID;
        public Rect position;

        public void ApplyToNode(MicrosceneStackNode node)
        {
            EventCallback<GeometryChangedEvent> del = null;
            
            var position = this.position;
            node.NodePosition = this.position;
            
            del = (GeometryChangedEvent evt) =>
            {
                node.SetGraphPosition(position);
                node.UnregisterCallback<GeometryChangedEvent>(del);
            };
            
            node.RegisterCallback<GeometryChangedEvent>(del);
        }
    }
}
