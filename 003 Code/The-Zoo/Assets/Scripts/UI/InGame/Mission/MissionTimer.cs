using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InGame.Mission
{
    public class MissionTimer : MonoBehaviour
    {
        [SerializeField] private Slider timeGauge;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private int missionTime = 40;

        public event Action OnTimerFinished;

        private bool isStarted;
        private float elapsed;

        private void Start()
        {
            ResetTimerUI();
        }

        internal void StartTimer()
        {
            if (isStarted) return;

            isStarted = true;
            elapsed = 0f;
            StartCoroutine(TimeCo());
        }

        private IEnumerator TimeCo()
        {
            while (elapsed < missionTime)
            {
                elapsed += Time.deltaTime;

                timeGauge.value = elapsed / missionTime; // [0~1] 비율로 표현
                timeGauge.normalizedValue = elapsed / missionTime; // 같은 의미

                var remaining = Mathf.CeilToInt(missionTime - elapsed);
                timeText.text = remaining.ToString();

                yield return null; // 매 프레임 갱신
            }

            isStarted = false;
            timeGauge.value = 1f;
            timeText.text = "0";

            OnTimerFinished?.Invoke();
        }

        private void ResetTimerUI()
        {
            elapsed = 0f;
            isStarted = false;

            timeGauge.minValue = 0f;
            timeGauge.maxValue = 1f;
            timeGauge.value = 0f;

            timeText.text = missionTime.ToString();
        }

    }
}