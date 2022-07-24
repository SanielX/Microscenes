
namespace Microscenes
{
    public enum MicroActionExecutionMode : byte
    {
        WaitToComplete,
        WaitForQueuedAndThisToComplete,
        PutInQueueAndMove
    }

    public enum MicroActionState : byte
    {
        None,
        Running,
        Completed
    }

    [System.Serializable]
    public abstract class MicroAction
    {
        private MicroActionState state;

        public void Reset(MicrosceneContext ctx)
        {
            state = MicroActionState.None;
        }

        public bool UpdateExecute(MicrosceneContext ctx)
        {
            if(state == MicroActionState.None)
            {
                state = MicroActionState.Running;
                OnStartExecute(ctx);
                return state == MicroActionState.Completed;
            }

            OnUpdateExecution(ctx);

            return state == MicroActionState.Completed;
        }

        protected void Complete() => state = MicroActionState.Completed;

        protected virtual void OnStartExecute(MicrosceneContext ctx) { }
        protected virtual void OnUpdateExecution(MicrosceneContext ctx) { }

        public virtual void OnValidate() { }
    }

    [SerializeReferencePath(SRPathType.Abstract, "Empty")]
    public class EmptyAction : MicroAction
    {
        protected override void OnStartExecute(MicrosceneContext ctx)
        {
            Complete();
        }
    }
}
