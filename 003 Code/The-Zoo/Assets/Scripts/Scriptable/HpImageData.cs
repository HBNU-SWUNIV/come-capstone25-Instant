using UnityEngine;

namespace Scriptable
{
    [CreateAssetMenu(fileName = "HPImageData", menuName = "UI/InGame/HPImages", order = 0)]
    public class HpImageData : ScriptableObject
    {
        [Header("체력 단계별 이미지")]
        public Sprite[] hpSprites;
    }
}