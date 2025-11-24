using DG.Tweening;
using Scriptable;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InGame.Mission
{
    public class MissionUI : MonoBehaviour
    {
        [Header("Base Mission")]
        [SerializeField] private CanvasGroup baseMissionView;
        [SerializeField] private TextMeshProUGUI baseMissionText;

        [Header("Hider Mission")]
        [SerializeField] private RectTransform missionView;
        [SerializeField] private Image background;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI missionText;
        [SerializeField] private TextMeshProUGUI targetValueText;

        [SerializeField] private Color succeedColor = Color.yellowGreen;
        [SerializeField] private Color failColor = Color.firebrick;
        [SerializeField] private Color originColor = Color.white;

        [SerializeField] private SfxData appearSfx;
        [SerializeField] private SfxData successSfx;
        [SerializeField] private SfxData failSfx;

        private Tween showTween;
        private float currentTargetValue;
        private float targetValue;

        internal void SetMission(string desc, int target)
        {
            AudioManager.Instance.PlayOneShot(appearSfx.clip);

            targetValue = target;
            missionText.text = desc;
            targetValueText.text = $"0 / {targetValue}";
        }

        internal void UpdateMission(float value)
        {
            currentTargetValue = value;

            var formattedValue = currentTargetValue % 1 == 0
                ? currentTargetValue.ToString("F0")
                : currentTargetValue.ToString("F1");

            targetValueText.text = $"{formattedValue} / {targetValue}";
        }

        internal void OnMissionSuccess()
        {
            PlaySuccessEffect();
        }

        internal void OnMissionFailed()
        {
            PlayFailEffect();
        }

        internal void SetBaseMissionText(string text)
        {
            baseMissionText.text = text;
        }

        internal void SetBaseMissionViewVisible(bool show)
        {
            baseMissionView.alpha = show ? 1 : 0;
            baseMissionView.interactable = show;
            baseMissionView.blocksRaycasts = show;
        }

        internal void SetHiderMissionViewVisible(bool show)
        {
            canvasGroup.alpha = show ? 1 : 0;
            canvasGroup.interactable = show;
            canvasGroup.blocksRaycasts = show;
        }

        internal void AnimateShow()
        {
            showTween?.Kill(); // 이전 트윈 정리

            // 🔹 시작 크기를 살짝 작게 (0.8배)
            missionView.localScale = Vector3.one * 0.8f;

            // 🔹 크기 확대 + 흔들림 + 복귀 시퀀스
            showTween = DOTween.Sequence()
                .Append(missionView.DOScale(1.15f, 0.25f).SetEase(Ease.OutBack)) // 팝!
                .Append(missionView.DOScale(1f, 0.15f).SetEase(Ease.OutQuad)) // 자연스럽게 복귀
                .Play();
        }

        internal void PlaySuccessEffect()
        {
            showTween?.Kill();

            DOTween.Sequence()
                .Append(background.DOColor(succeedColor, 0.15f))
                .Join(missionView.DOScale(1.2f, 0.25f).SetEase(Ease.OutBack))
                .Append(missionView.DOScale(0.9f, 0.2f).SetEase(Ease.InOutSine))
                .Append(missionView.DOScale(1f, 0.15f))
                .Join(background.DOColor(originColor, 0.3f))
                .AppendInterval(0.3f)
                .Append(canvasGroup.DOFade(0, 0.4f))
                .OnComplete(() => { SetHiderMissionViewVisible(false); })
                .Play();

            AudioManager.Instance.PlayOneShot(successSfx.clip);
        }

        // ✅ 미션 실패 시 연출
        internal void PlayFailEffect()
        {
            showTween?.Kill();

            DOTween.Sequence()
                .Append(background.DOColor(failColor, 0.1f))
                .Join(missionView.DOShakePosition(0.4f, 10f, 15))
                .Append(missionView.DOScale(0.95f, 0.15f).SetEase(Ease.OutSine))
                .Append(missionView.DOScale(1f, 0.2f))
                .Join(background.DOColor(originColor, 0.3f))
                .AppendInterval(0.3f)
                .Append(canvasGroup.DOFade(0, 0.4f))
                .OnComplete(() => { SetHiderMissionViewVisible(false); })
                .Play();

            AudioManager.Instance.PlayOneShot(failSfx.clip);
        }
    }
}