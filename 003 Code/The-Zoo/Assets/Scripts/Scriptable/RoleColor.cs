using UnityEngine;

namespace Scriptable
{
    [CreateAssetMenu(fileName = "RoleColorData", menuName = "UI/Player/RoleColors", order = 0)]
    public class RoleColor : ScriptableObject
    {
        [Header("역할 별 색상")]
        public Color defaultColor;
        public Color hiderColor;
        public Color seekerColor;
        public Color fighterColor;
    }
}