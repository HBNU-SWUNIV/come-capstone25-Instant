using Unity.MLAgents;
using UnityEngine;

namespace AI
{
    public class TimeManager : MonoBehaviour
    {
        [Tooltip("학습 중 사용할 Time.timeScale")]
        public float trainingTimeScale = 20f;

        [Tooltip("데모(추론) 플레이 시 Time.timeScale")]
        public float inferenceTimeScale = 1f;

        private void Start()
        {
            // Python 학습 프로세스가 연결되어 있는지 확인
            if (Academy.Instance.IsCommunicatorOn)
            {
                print("<color=green>[TimeManager]</color> 학습 모드: Time.timeScale = " + trainingTimeScale);
                Time.timeScale = trainingTimeScale;
                Application.targetFrameRate = -1; // 프레임 무제한
                Application.runInBackground = true;
            }
            else
            {
                print("<color=cyan>[TimeManager]</color> 데모 모드: Time.timeScale = " + inferenceTimeScale);
                Time.timeScale = inferenceTimeScale;
                Application.targetFrameRate = 60; // 안정적인 프레임 유지
            }
        }
    }
}