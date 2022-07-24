using UnityEngine;

namespace Microscenes
{
    public ref struct MicrosceneContext
    {
        public MicrosceneContext(Component caller, object customData)
        {
            this.caller = caller;
            this.customData = customData;
        }

        public Component caller;
        public object customData;
    }
}
