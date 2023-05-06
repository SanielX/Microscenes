using System;

namespace Microscenes
{
    [Serializable]
    public abstract class MicrosceneStackBehaviour
    {
        /// <summary>
        /// Called when graph execution gets to this stack. Not that Start may be called multiple times if Microscene is activated multiple times
        /// </summary>
        public virtual void Start(ref MicrosceneStackContext ctx) { }
        
        /// <summary>
        /// Called every frame stack is updated
        /// </summary>
        /// <remarks>See <see cref="FinishIf"/>, <see cref="Finish"/>, <see cref="Continue"/> and <see cref="FinishAndSelect"/> methods</remarks>
        public abstract MicrosceneStackResult Update(ref MicrosceneStackContext ctx);

        protected MicrosceneStackResult FinishAndSelect(int index)
        {
            return new MicrosceneStackResult(true, index);
        }
        
        protected MicrosceneStackResult Finish()                 => new(true, 0);
        protected MicrosceneStackResult FinishIf(bool condition) => new(condition, 0);
        protected MicrosceneStackResult FinishIfAndSelect(bool condition, int index) => new(condition, index);
        
        protected MicrosceneStackResult Continue() => default;
    }
}