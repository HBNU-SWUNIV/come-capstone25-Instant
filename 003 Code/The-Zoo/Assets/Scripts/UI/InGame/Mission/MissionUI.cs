using DG.Tweening;
using Scriptable;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InGame.Mission
{
    public class MissionUI : MonoBehaviour
    {
        [SerializeField] private RectTransform viewRect;
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
        private float currentTargetValue;

        private Tween showTween;

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

        internal void SetVisible(bool show)
        {
            canvasGroup.alpha = show ? 1 : 0;
            canvasGroup.interactable = show;
            canvasGroup.blocksRaycasts = show;
        }

        internal void AnimateShow()
        {
            showTween?.Kill(); // 이전 트윈 정리

            // 🔹 시작 크기를 살짝 작게 (0.8배)
            viewRect.localScale = Vector3.one * 0.8f;

            // 🔹 크기 확대 + 흔들림 + 복귀 시퀀스
            showTween = DOTween.Sequence()
                .Append(viewRect.DOScale(1.15f, 0.25f).SetEase(Ease.OutBack)) // 팝!
                .Append(viewRect.DOScale(1f, 0.15f).SetEase(Ease.OutQuad)) // 자연스럽게 복귀
                .Play();
        }

        internal void PlaySuccessEffect()
        {
            showTween?.Kill();

            DOTween.Sequence()
                .Append(background.DOColor(succeedColor, 0.15f))
                .Join(viewRect.DOScale(1.2f, 0.25f).SetEase(Ease.OutBack))
                .Append(viewRect.DOScale(0.9f, 0.2f).SetEase(Ease.InOutSine))
                .Append(viewRect.DOScale(1f, 0.15f))
                .Join(background.DOColor(originColor, 0.3f))
                .AppendInterval(0.3f)
                .Append(canvasGroup.DOFade(0, 0.4f))
                .OnComplete(() => { SetVisible(false); })
                .Play();

            AudioManager.Instance.PlayOneShot(successSfx.clip);
        }

        // ✅ 미션 실패 시 연출
        internal void PlayFailEffect()
        {
            showTween?.Kill();

            DOTween.Sequence()
                .Append(background.DOColor(failColor, 0.1f))
                .Join(viewRect.DOShakePosition(0.4f, 10f, 15))
                .Append(viewRect.DOScale(0.95f, 0.15f).SetEase(Ease.OutSine))
                .Append(viewRect.DOScale(1f, 0.2f))
                .Join(background.DOColor(originColor, 0.3f))
                .AppendInterval(0.3f)
                .Append(canvasGroup.DOFade(0, 0.4f))
                .OnComplete(() => { SetVisible(false); })
                .Play();

            AudioManager.Instance.PlayOneShot(failSfx.clip);
        }
    }
}