using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    [Serializable]
    struct StackNodeMetadata
    {
        public StackNodeMetadata(MicrosceneStackNodeView nodeView) : this()
        {
            position = nodeView.NodePosition;
        }
        
        public int  nodeID;
        public Rect position;

        public void ApplyToNode(MicrosceneStackNodeView nodeView)
        {
            EventCallback<GeometryChangedEvent> del = null;
            
            var position = this.position;
            nodeView.NodePosition = this.position;
            
            del = (GeometryChangedEvent evt) =>
            {
                nodeView.SetGraphPosition(position);
                nodeView.UnregisterCallback<GeometryChangedEvent>(del);
            };
            
            nodeView.RegisterCallback<GeometryChangedEvent>(del);
        }
    }
}
