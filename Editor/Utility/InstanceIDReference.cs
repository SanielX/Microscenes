using UnityEditor;
using UnityEngine;

namespace Microscenes.Editor
{
    [System.Serializable]
    public struct InstanceIDReference<T> where T : UnityEngine.Object
    {
        [SerializeField]
        private int instanceID;

        public T value
        {
            get => EditorUtility.InstanceIDToObject(instanceID) as T;
            set => instanceID = value ? value.GetInstanceID() : 0;
        }

        public static implicit operator T(InstanceIDReference<T> re) => re.value;
        public static implicit operator InstanceIDReference<T>(T re) => new InstanceIDReference<T>() { value = re };
    }
}