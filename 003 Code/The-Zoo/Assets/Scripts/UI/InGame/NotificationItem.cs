using DG.Tweening;
using TMPro;
using UnityEngine;

namespace UI.InGame
{
    public class NotificationItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private CanvasGroup canvasGroup;

        public void Setup(string text)
        {
            messageText.text = text;
            canvasGroup.alpha = 0f; // 처음에 투명하게 시작

            // 등장 -> 대기 -> 퇴장 -> 파괴 시퀀스
            DOTween.Sequence()
                .Append(canvasGroup.DOFade(1f, 0.3f)) // 0.3초간 페이드 인
                .Join(transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack)) // 팝업 효과
                .Append(transform.DOScale(1f, 0.1f)) // 크기 원복
                .AppendInterval(3f) // 3초간 대기 (메시지 읽는 시간)
                .Append(canvasGroup.DOFade(0f, 0.5f)) // 0.5초간 페이드 아웃
                .Join(transform.DOScale(0.5f, 0.5f)) // 작아지면서 사라짐
                .OnComplete(() => Destroy(gameObject)); // 애니메이션 끝나면 오브젝트 삭제
        }
    }
}