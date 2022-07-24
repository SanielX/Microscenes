using UnityEngine;

namespace Microscenes.Editor
{
    internal class ScriptableWrapper : ScriptableObject
    {
        [SerializeReference]
        public object binding;
    }
}
