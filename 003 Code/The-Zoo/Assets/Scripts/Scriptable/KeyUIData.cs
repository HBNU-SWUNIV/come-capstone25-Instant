using UnityEngine;

namespace Scriptable
{
    [CreateAssetMenu(fileName = "KeyUIData", menuName = "UI/InGame/KeyUI", order = 0)]
    public class KeyUIData : ScriptableObject
    {
        public Color enableColor;
        public Color unableColor;
    }
}