using EventHandler;
using Networks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameObject backgroundCanvas;
        [SerializeField] private GameObject titleCanvas;
        [SerializeField] private GameObject mainCanvas;
        [SerializeField] private GameObject lobbyCanvas;
        [SerializeField] private GameObject loadingCanvas;
        [SerializeField] private GameObject popupCanvas;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            GamePlayEventHandler.PlayerLogin += PlayerLogin;
            ConnectionEventHandler.SessionConnectStart += OnSessionConnectStart;
            ConnectionEventHandler.ConnectionFailed += OnConnectionFailed;

            NetworkManager.OnDestroying += OnDestroying;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;

            UnityServices.Initialized += UnityServicesOnInitialized;

            if (popupCanvas != null)
                popupCanvas.SetActive(true);
        }

        private void OnDestroying(NetworkManager obj)
        {
            NetworkManager.OnDestroying -= OnDestroying;

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            AuthenticationService.Instance.SignedIn -= OnSignedIn;

            ConnectionEventHandler.SessionConnectStart -= OnSessionConnectStart;
            ConnectionEventHandler.ConnectionFailed -= OnConnectionFailed;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            if (scene.name != "Lobby") return;

            if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
            {
                SwitchUI(UIType.Title);
                return;
            }

            if (ConnectionManager.Instance && ConnectionManager.Instance.CurrentSession != null)
                SwitchUI(UIType.Lobby);
            else if (AuthenticationService.Instance != null &&
                     AuthenticationService.Instance.IsSignedIn)
                SwitchUI(UIType.Main);
            else
                SwitchUI(UIType.Title);
        }

        private void UnityServicesOnInitialized()
        {
            UnityServices.Initialized -= UnityServicesOnInitialized;

            AuthenticationService.Instance.SignedIn += OnSignedIn;
        }

        private void SwitchUI(UIType uiType)
        {
            if (!titleCanvas || !mainCanvas || !lobbyCanvas || !loadingCanvas || !popupCanvas ||
                !backgroundCanvas) return;

            titleCanvas.SetActive(uiType == UIType.Title);
            mainCanvas.SetActive(uiType == UIType.Main);
            lobbyCanvas.SetActive(uiType == UIType.Lobby);
            backgroundCanvas.SetActive(uiType != UIType.Lobby);
            loadingCanvas.SetActive(false);
        }

        private void OnSignedIn()
        {
            SwitchUI(UIType.Main);
        }

        private void PlayerLogin()
        {
            loadingCanvas.SetActive(true);
        }

        private void OnSessionConnectStart()
        {
            loadingCanvas.SetActive(true);
        }

        private void OnConnectionFailed()
        {
            loadingCanvas.SetActive(false);
        }

        private void OnClientConnectedCallback(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId) return;

            SwitchUI(UIType.Lobby);
        }

        private void OnClientDisconnectCallback(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId) return;

            SwitchUI(UIType.Main);
        }
    }

    internal enum UIType
    {
        Title,
        Main,
        Lobby
    }
}
