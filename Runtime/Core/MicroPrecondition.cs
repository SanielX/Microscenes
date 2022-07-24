using UnityEngine;

namespace Microscenes
{

    [System.Serializable]
    public abstract class MicroPrecondition
    {
        public abstract bool Update(in MicrosceneContext info);
        public virtual void  Start(in MicrosceneContext info) { }

        public virtual void DrawSceneGizmo(bool selected, Microscene owner) { }

        public virtual void OnValidate() { }
    }
}
