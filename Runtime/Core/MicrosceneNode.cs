namespace Microscenes
{
    [System.Serializable]
    public class MicrosceneNode
    {
        public MicrosceneNodeState State { get; private set; }

        public void ResetState() => State = MicrosceneNodeState.None;

        public void Update(in MicrosceneContext ctx)
        {
            if(State != MicrosceneNodeState.Executing)
            {
                State = MicrosceneNodeState.Executing;
                OnStart(ctx);
            }

            OnUpdate(ctx);
        }

        protected void Complete() => State = MicrosceneNodeState.Finished;
        protected virtual void OnStart (in MicrosceneContext ctx) { }
        protected virtual void OnUpdate(in MicrosceneContext ctx) { }

        public virtual void OnValidate() { }
        public virtual void OnDrawSceneGizmo(bool selected, Microscene owner) { }
    }
}
