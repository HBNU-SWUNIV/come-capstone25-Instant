using System;
using Networks;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Lobby.SessionList
{
    public class PasswordPanel : MonoBehaviour
    {
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private Button okButton;
        [SerializeField] private Button cancelButton;

        private CanvasGroup canvasGroup;
        private ISessionInfo selectedSession;
        public bool IsVisible { get; private set; }

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            okButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            cancelButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);

            okButton.onClick.AddListener(OnOkButtonClick);
            cancelButton.onClick.AddListener(OnCancelButtonClick);
            SetVisible(false);
        }

        private void OnDestroy()
        {
            okButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);
            cancelButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);

            okButton.onClick.RemoveListener(OnOkButtonClick);
            cancelButton.onClick.RemoveListener(OnCancelButtonClick);
        }

        public void SetSession(ISessionInfo session)
        {
            selectedSession = session;
        }

        private async void OnOkButtonClick()
        {
            var password = passwordInput.text;

            var data = new ConnectionData(
                ConnectionData.ConnectionType.JoinById, selectedSession.Id, password);

            await ConnectionManager.Instance.ConnectAsync(data);

            SetVisible(false);
        }

        private void OnCancelButtonClick()
        {
            passwordInput.text = "";

            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1 : 0;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;

            IsVisible = visible;
        }
    }
}