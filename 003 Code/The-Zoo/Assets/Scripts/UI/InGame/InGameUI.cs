using DG.Tweening;
using EventHandler;
using GamePlay;
using Players;
using Scriptable;
using TMPro;
using UI.Lobby.Preferences;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI.InGame
{
    public class InGameUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private PlayBoard.PlayBoard playBoard;
        [SerializeField] private Preferences preferences;
        [SerializeField] private Image[] redHealth;
        [SerializeField] private HpImageData hpImageData;
        [SerializeField] private KeyUI keyUI;

        private bool preferencesIsOpen = false;

        private void Start()
        {
            PlayManager.Instance.currentTime.OnValueChanged += OnTimerChanged;

            var player = PlayerLocator.LocalPlayer;

            player.hBody.healthPoint.OnValueChanged += OnPlayerHealthChanged;
            player.playerInput.InputActions.UI.Tab.performed += OnTabKeyPressed;
            player.playerInput.InputActions.UI.Tab.canceled += OnTabKeyPressed;
            player.playerInput.InputActions.UI.Escape.performed += OnESCKeyPressed;

            GamePlayEventHandler.CheckInteractable += OnKeyUI;

            keyUI.SetVisible(false);

            GamePlayEventHandler.OnUIChanged("InGame");
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnKeyUI(bool value, bool active)
        {
            keyUI.SetVisible(value);

            if (active) keyUI.Enable();
            else keyUI.Unable();
        }

        private void OnTimerChanged(int previousValue, int newValue)
        {
            timerText.text = $"{newValue / 60:00}:{newValue % 60:00}";
        }

        private void OnTabKeyPressed(InputAction.CallbackContext ctx)
        {
            playBoard.SetVisible(ctx.performed);
        }

        internal void Unsubscribe()
        {
            PlayManager.Instance.currentTime.OnValueChanged -= OnTimerChanged;
            GamePlayEventHandler.CheckInteractable -= OnKeyUI;

            var player = PlayerLocator.LocalPlayer;

            player.hBody.healthPoint.OnValueChanged -= OnPlayerHealthChanged;
            player.playerInput.InputActions.UI.Tab.performed -= OnTabKeyPressed;
            player.playerInput.InputActions.UI.Tab.canceled -= OnTabKeyPressed;
            player.playerInput.InputActions.UI.Escape.performed -= OnESCKeyPressed;
        }

        private void OnESCKeyPressed(InputAction.CallbackContext ctx)
        {
            preferencesIsOpen = !preferencesIsOpen;
            preferences.SetVisible(preferencesIsOpen);

            if(!preferencesIsOpen)
                PlayerLocator.LocalPlayer.playerInput.HideCursor();
        }

        private void OnPlayerHealthChanged(int oldValue, int newValue)
        {
            var value = newValue;

            for (var i = 0; i < redHealth.Length; i++)
            {
                var img = redHealth[i];
                img.sprite = value-- > 0 ? hpImageData.hpSprites[1] : hpImageData.hpSprites[0];

                // 🔹 체력이 깎인 경우 (oldValue > newValue)
                if (i < oldValue && i >= newValue)
                {
                    AnimateDamage(img);
                }
                // 🔹 체력이 회복된 경우
                else if (i >= oldValue && i < newValue)
                {
                    AnimateHeal(img);
                }
            }
        }

        private void AnimateDamage(Image img)
        {
            img.transform.DOKill();
            img.color = Color.white;
            img.transform.localScale = Vector3.one;

            DOTween.Sequence()
                .Append(img.transform.DOScale(0.8f, 0.1f))
                .Append(img.DOColor(Color.red, 0.1f))
                .Append(img.transform.DOScale(1.0f, 0.2f).SetEase(Ease.OutBack))
                .Join(img.DOColor(Color.white, 0.2f))
                .Play();
        }

        private void AnimateHeal(Image img)
        {
            img.transform.DOKill();
            img.color = Color.white;
            img.transform.localScale = Vector3.one;

            DOTween.Sequence()
                .Append(img.transform.DOScale(1.3f, 0.15f).SetEase(Ease.OutBack))
                .Append(img.transform.DOScale(1f, 0.25f).SetEase(Ease.OutQuad))
                .Play();
        }
    }
}