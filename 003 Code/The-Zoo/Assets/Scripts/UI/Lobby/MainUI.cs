using System;
using EventHandler;
using Networks;
using TMPro;
using UI.Lobby.SessionList;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using WebSocketSharp;

namespace UI.Lobby
{
    public class MainUI : MonoBehaviour
    {
        [SerializeField] private Button quickStartButton;
        [SerializeField] private TMP_InputField codeInput;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button sessionListButton;
        [SerializeField] private SessionListView sessionsList;


        private void OnEnable()
        {
            sessionsList.Hide();

            codeInput.onSelect.AddListener((s) => AudioManager.Instance.PlayUISfx());
            joinButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            quickStartButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            sessionListButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);

            joinButton.onClick.AddListener(OnJoinButtonClick);
            quickStartButton.onClick.AddListener(OnQuickStartButtonClick);
            sessionListButton.onClick.AddListener(sessionsList.Show);

            GamePlayEventHandler.OnUIChanged(Util.TitleSceneName);
        }

        private void OnDisable()
        {
            codeInput.onSelect.RemoveListener((s) => AudioManager.Instance.PlayUISfx());
            joinButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);
            quickStartButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);
            sessionListButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);

            joinButton.onClick.RemoveListener(OnJoinButtonClick);
            quickStartButton.onClick.RemoveListener(OnQuickStartButtonClick);
            sessionListButton.onClick.RemoveListener(sessionsList.Show);
        }

        private async void OnQuickStartButtonClick()
        {
            try
            {
                var playerName = Util.GetPlayerNameWithoutHash(AuthenticationService.Instance.PlayerName);;

                var data = new ConnectionData(ConnectionData.ConnectionType.Quick, null, null,
                    Util.GetDefaultSessionName(playerName));

                await ConnectionManager.Instance.ConnectAsync(data);
            }
            catch (Exception ex)
            {
                InformationPopup.InformationPopup.instance.ShowPopup(ex.Message);
            }
        }

        private async void OnJoinButtonClick()
        {
            try
            {
                var code = codeInput.text;

                if (code.IsNullOrEmpty())
                {
                    InformationPopup.InformationPopup.instance.ShowPopup("코드를 입력해주세요");

                    return;
                }

                var data = new ConnectionData(ConnectionData.ConnectionType.JoinByCode, codeInput.text);

                await ConnectionManager.Instance.ConnectAsync(data);
            }
            catch (Exception ex)
            {
                InformationPopup.InformationPopup.instance.ShowPopup(ex.Message);
            }
        }
    }
}