using System;
using EventHandler;
using Networks;
using UI.Lobby.PlayerList;
using Unity.Netcode;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace UI.Lobby
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private PlayerListView playerListView;
        [SerializeField] private GameObject gameSetup;

        [SerializeField] private Button quitButton;
        [SerializeField] private Button gameStartButton;
        [SerializeField] private Button gameReadyButton;

        private void OnEnable()
        {
            if (!UnityServices.State.Equals(ServicesInitializationState.Initialized)) return;

            var session = ConnectionManager.Instance.CurrentSession;
            session.SessionHostChanged += OnSessionHostChanged;
            session.PlayerJoined += OnPlayerChanged;
            session.PlayerHasLeft += OnPlayerChanged;
            GameManager.Instance.readyCount.OnValueChanged += OnValueChanged;

            quitButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            gameStartButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            gameReadyButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);

            quitButton.onClick.AddListener(OnQuitButtonClick);
            gameStartButton.onClick.AddListener(OnGameStartButtonClick);
            gameReadyButton.onClick.AddListener(OnGameReadyButtonClick);

            SwitchUI(session.IsHost);

            GamePlayEventHandler.OnUIChanged(Util.LobbySceneName);
#if UNITY_EDITOR
            gameStartButton.interactable = true;
#else
            gameStartButton.interactable = false;
#endif
        }

        private void OnDisable()
        {
            var session = ConnectionManager.Instance.CurrentSession;

            if (session == null) return;

            session.SessionHostChanged -= OnSessionHostChanged;
            session.PlayerJoined -= OnPlayerChanged;
            session.PlayerHasLeft -= OnPlayerChanged;
            GameManager.Instance.readyCount.OnValueChanged -= OnValueChanged;

            quitButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);
            gameStartButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);
            gameReadyButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);

            quitButton.onClick.RemoveListener(OnQuitButtonClick);
            gameStartButton.onClick.RemoveListener(OnGameStartButtonClick);
            gameReadyButton.onClick.RemoveListener(OnGameReadyButtonClick);
        }

        private void OnPlayerChanged(string obj)
        {
            OnValueChanged(0, GameManager.Instance.readyCount.Value);
        }

        private void OnValueChanged(int previousValue, int newValue)
        {
            gameStartButton.interactable =
                NetworkManager.Singleton.ConnectedClientsIds.Count > 1 &&
                newValue >= NetworkManager.Singleton.ConnectedClientsIds.Count - 1;
        }

        private void OnSessionHostChanged(string obj)
        {
            SwitchUI(ConnectionManager.Instance.CurrentSession.IsHost);
        }

        private async void OnQuitButtonClick()
        {
            try
            {
                await ConnectionManager.Instance.DisconnectSessionAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void OnGameStartButtonClick()
        {
            GameManager.Instance.GameStart();
        }

        private void OnGameReadyButtonClick()
        {
            GameManager.Instance.Ready();
        }

        private void SwitchUI(bool isHost)
        {
            gameSetup.gameObject.SetActive(isHost);
            gameStartButton.gameObject.SetActive(isHost);
            gameReadyButton.gameObject.SetActive(!isHost);
        }
    }
}