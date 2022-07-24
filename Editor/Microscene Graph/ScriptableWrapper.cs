using UnityEngine;

namespace Microscenes.Editor
{
    class ScriptableWrapper : ScriptableObject
    {
        [SerializeReference]
        public object binding;
    }
}
