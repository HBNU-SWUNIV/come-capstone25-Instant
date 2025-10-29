using System.Collections;
using DG.Tweening;
using GamePlay;
using Players;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InGame
{
    public class HitUI : MonoBehaviour
    {
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private Color originColor = new Color(1f, 0f, 0f, 0f);
        [SerializeField] private Color hitColor = new Color(1f, 0f, 0f, 0.5f);

        private Image hitOverlay;
        private Tween flashTween;

        private void Awake()
        {
            hitOverlay = GetComponent<Image>();
        }

        private void Start()
        {
            PlayerLocator.LocalPlayer.hBody.healthPoint.OnValueChanged += OnPlayerHealthChanged;
        }

        private void OnDestroy()
        {
            PlayerLocator.LocalPlayer.hBody.healthPoint.OnValueChanged -= OnPlayerHealthChanged;
        }

        private void OnPlayerHealthChanged(int oldValue, int newValue)
        {
            ShowHitEffect();
        }

        private void ShowHitEffect()
        {
            flashTween?.Kill();

            hitOverlay.color = hitColor;

            flashTween = hitOverlay
                .DOFade(0f, fadeDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    hitOverlay.color = originColor;
                });
        }
    }
}