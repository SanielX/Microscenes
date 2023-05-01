namespace Microscenes
{
    [System.Serializable]
    public abstract class MicrosceneNode
    {
        public MicrosceneNodeState State { get; private set; }

        public void ResetState() => State = MicrosceneNodeState.None;
        
        public void UpdateNode(in MicrosceneContext ctx)
        {
#if UNITY_ASSERTIONS
            try
            {
#endif
                if (State == MicrosceneNodeState.None) // Special first time execution case
                {
                    State = MicrosceneNodeState.Executing;
                    OnStart(ctx);
                    
                    // We might have called Complete() in OnStart, which should omit calling Update
                    if (State != MicrosceneNodeState.Finished)
                        OnUpdate(ctx);

                    return;
                }

                OnUpdate(ctx);

#if UNITY_ASSERTIONS
            } catch(System.Exception e) { UnityEngine.Debug.LogException(e); Complete(); }
#endif 
        }

        protected void Complete() => State = MicrosceneNodeState.Finished;
        protected virtual void OnStart (in MicrosceneContext ctx) { }
        protected virtual void OnUpdate(in MicrosceneContext ctx) { }

        public virtual void OnValidate() { }
        public virtual void OnDrawSceneGizmo(bool selected, Microscene owner) { }
    }
}
