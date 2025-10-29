using UnityEngine;

namespace Scriptable
{
    public enum MissionType
    {
        None,
        SpinForSeconds,   // 5초 동안 회전하기
        EatApple,         // 사과 5개 먹기
        EatMushroom,      // 버섯 5개 먹기
        JumpTimes,        // 점프 5번 하기
        StayOnRock        // 바위 위에서 5초 버티기
    }

    [CreateAssetMenu(fileName = "Mission Data", menuName = "Mission", order = 0)]
    public class MissionData : ScriptableObject
    {
        public MissionType type;
        public string description;
        public int targetValue;
    }
}