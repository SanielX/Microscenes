using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Microscenes.Editor
{
    [Serializable]
    class MicrosceneGraphMetadata
    {
        public Rect quitEntryPosition = new(0, 200, 100, 100);
        public Rect entryPosition;
        public Vector3 cameraPosition, cameraScale = Vector3.one;

        public List<MicrosceneNodeMetadata> nodes         = new List<MicrosceneNodeMetadata>();
        public List<StackNodeMetadata>      stackNodes    = new List<StackNodeMetadata>     ();
        public List<StickyNoteMetadata>     stickyNotes   = new List<StickyNoteMetadata>    ();
        
        int FindStackNodeData(int nodeID)
        {
            for (int i = 0; i < stackNodes.Count; i++)
            {
                if (stackNodes[i].nodeID == nodeID)
                {
                    return i;
                }
            }
            
            return -1;
        }

        public bool FindStackNodeData(int nodeID, out StackNodeMetadata stack)
        {
            for (int i = 0; i < stackNodes.Count; i++)
            {
                if (stackNodes[i].nodeID == nodeID)
                {
                    stack = stackNodes[i];
                    return true;
                }
            }
            
            stack = default;
            return false;
        }
        
        public bool FindNodeData(int nodeID, out MicrosceneNodeMetadata nodeMetadata)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].nodeID == nodeID)
                {
                    nodeMetadata = nodes[i];
                    return true;
                }
            }
            
            nodeMetadata = default;
            return false;
        }
    }
}
