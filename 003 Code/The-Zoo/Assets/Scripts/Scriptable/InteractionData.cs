using Unity.Netcode;
using UnityEngine;

namespace Scriptable
{
    [CreateAssetMenu(fileName = "Interaction Data", menuName = "Object Data/Interactions", order = 0)]
    public class InteractionData : ScriptableObject
    {
        public NetworkObject obj;
        public int count;
    }
}