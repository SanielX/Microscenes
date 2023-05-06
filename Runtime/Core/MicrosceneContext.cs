using UnityEngine;

namespace Microscenes
{
    public ref struct MicrosceneContext
    {
        public Component caller      { get; internal set; }
        public object    customData  { get; internal set; }
        
#if UNITY_ASSERTIONS
        internal MicrosceneNode CurrentNode      { get; set; }
#else
        internal MicrosceneNode CurrentNode
        {
            get => null;
            set { }
        }
#endif 
    }
}
