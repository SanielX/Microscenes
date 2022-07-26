using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microscenes.Editor
{
    [Serializable]
    class MicrosceneGraphMetadata
    {
        public Vector2 entryPosition;
        public Vector3 cameraPosition, cameraScale = Vector3.one;

        public List<MicrosceneNodeMetadata> preconditions = new List<MicrosceneNodeMetadata>();
        public List<MicrosceneNodeMetadata> nodes       = new List<MicrosceneNodeMetadata>();
        public List<StackNodeMetadata>      stackNodes    = new List<StackNodeMetadata>     ();
        public List<StickyNoteMetadata>     stickyNotes   = new List<StickyNoteMetadata>    ();
    }
}
