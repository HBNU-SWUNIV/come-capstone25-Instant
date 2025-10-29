using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Lobby
{
    public class LobbyLoadingUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image loadingIcon;
        [SerializeField] private TMP_Text loadingText;

        private Tween rotateTween;
        private Tween bounceTween;

        private void OnEnable()
        {
            StartAnimation();
        }

        private void OnDisable()
        {
            StopAnimation();
        }

        private void StartAnimation()
        {
            rotateTween?.Kill();
            bounceTween?.Kill();

            rotateTween = loadingIcon
                .rectTransform
                .DORotate(new Vector3(0, 0, -360f), 1.2f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);

            var startY = loadingText.rectTransform.anchoredPosition.y;
            bounceTween = loadingText.rectTransform
                .DOAnchorPosY(startY + 10f, 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            loadingText.DOFade(0.4f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutQuad);
        }

        private void StopAnimation()
        {
            rotateTween?.Kill();
            bounceTween?.Kill();

            loadingIcon.rectTransform.rotation = Quaternion.identity;
            var pos = loadingText.rectTransform.anchoredPosition;
            pos.y = 0;
            loadingText.rectTransform.anchoredPosition = pos;
            loadingText.alpha = 1f;
        }
    }
}
