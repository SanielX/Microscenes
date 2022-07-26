using UnityEngine;

namespace Microscenes
{
    [MicrosceneNodeType(MicrosceneNodeType.Action)]
    [SerializeReferencePath(SRPathType.Abstract, "Print To Console")]
    public class PrintToConsoleAction : MicrosceneNode
    {
        [SerializeField] string m_Text;
        [SerializeField] LogType m_LogType = LogType.Log;

        protected override void OnStart(in MicrosceneContext ctx)
        {
            switch (m_LogType)
            {
            case LogType.Log:
                Debug.Log(m_Text);
                break;
            case LogType.Warning:
                Debug.LogWarning(m_Text);
                break;
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                Debug.LogError(m_Text);
                break;
            }

            Complete();
        }
    }
}