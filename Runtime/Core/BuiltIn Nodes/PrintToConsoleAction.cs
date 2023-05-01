using UnityEngine;
using Microscenes;

namespace Microscenes.Nodes
{
    [NodePath(NodeFolder.Abstract + "Print To Console")]
    [System.Serializable, MicrosceneNode]
    public class PrintToConsoleAction : MicrosceneNode, INameableNode
    {
        enum LogType { Log, Warning, Error };

        [SerializeField] string  m_Message = "Hello world";
        [SerializeField] LogType m_LogType;

        protected override void OnStart(in MicrosceneContext ctx)
        {
            switch (m_LogType)
            {
            case LogType.Error:
                Debug.LogError(m_Message);
                break;
            case LogType.Warning:
                Debug.LogWarning(m_Message);
                break;
            case LogType.Log:
                Debug.Log(m_Message);
                break;
            }

            Complete();
        }

        public string GetNiceNameString()
        {
            return "Print " + (m_LogType == LogType.Error? "error " : (m_LogType == LogType.Warning? "warning " : "")) + $"\"{m_Message}\"";
        }
    }
}
