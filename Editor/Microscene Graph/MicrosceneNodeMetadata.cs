using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Microscenes.Editor
{
    [Serializable]
    struct MicrosceneNodeMetadata
    {
        internal MicrosceneNodeMetadata(MicrosceneNodeView node) : this()
        {
            position = node.NodePosition;
            expanded = node.expanded;
        }
  
        public void ApplyToNode(MicrosceneNodeView node)
        {
            EventCallback<GeometryChangedEvent> del = null;
            
            node.NodePosition = this.position;
            var position = this.position;
            var expanded = this.expanded;
            
            del = (GeometryChangedEvent evt) =>
            {
                node.SetGraphPosition(position);
                node.expanded = expanded;
                
                node.UnregisterCallback<GeometryChangedEvent>(del);
            };
            
            node.RegisterCallback<GeometryChangedEvent>(del);
        }
        
        public int  nodeID;
        public Rect position;
        public bool expanded;
    }
}
