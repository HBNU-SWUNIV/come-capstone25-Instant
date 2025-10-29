using UnityEngine;

namespace Scriptable
{
    [CreateAssetMenu(fileName = "Environment Data", menuName = "Object Data/Environments", order = 0)]
    public class EnvironmentData : ScriptableObject
    {
        public GameObject obj;
        public int count;
    }
}