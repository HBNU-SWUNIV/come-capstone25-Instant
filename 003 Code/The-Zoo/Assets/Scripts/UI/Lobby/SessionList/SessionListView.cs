using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Networks;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace UI.Lobby.SessionList
{
    public class SessionListView : MonoBehaviour
    {
        [SerializeField] private GameObject sessionViewPrefab;
        [SerializeField] private Transform contentParent;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button createButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private PasswordPanel passwordPanel;

        private readonly LinkedList<SessionView> activeViews = new();
        private IObjectPool<SessionView> pool;

        private CanvasGroup canvasGroup;

        private ISessionInfo selectedSession;

        private void Awake()
        {
            pool = new ObjectPool<SessionView>
            (
                OnCreatePooledObjects,
                OnGetPooledObjects,
                OnReturnPooledObjects,
                OnDestroyPooledObjects,
                true, 5, 100
            );

            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            if (!UnityServices.State.Equals(ServicesInitializationState.Initialized)) return;

            joinButton.interactable = false;
            RefreshAsync();
            
            closeButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            joinButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            createButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            refreshButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);

            closeButton.onClick.AddListener(Hide);
            joinButton.onClick.AddListener(OnJoinButtonClick);
            createButton.onClick.AddListener(OnCreateButtonClick);
            refreshButton.onClick.AddListener(OnRefreshButtonClick);
        }

        private void OnDisable()
        {
            closeButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);
            joinButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);
            createButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);
            refreshButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);

            closeButton.onClick.RemoveListener(Hide);
            joinButton.onClick.RemoveListener(OnJoinButtonClick);
            createButton.onClick.RemoveListener(OnCreateButtonClick);
            refreshButton.onClick.RemoveListener(OnRefreshButtonClick);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private async void OnCreateButtonClick()
        {
            var data = new ConnectionData(ConnectionData.ConnectionType.Create);

            await ConnectionManager.Instance.ConnectAsync(data);
        }

        private async void OnJoinButtonClick()
        {
            if (selectedSession == null) return;
            if (passwordPanel.IsVisible) return;

            if (selectedSession.HasPassword)
            {
                passwordPanel.SetVisible(true);

                passwordPanel.SetSession(selectedSession);

                return;
            }

            var data = new ConnectionData(ConnectionData.ConnectionType.JoinById,
                selectedSession.Id);

            await ConnectionManager.Instance.ConnectAsync(data);

            selectedSession = null;
            joinButton.interactable = false;

            Hide();
        }

        private void OnRefreshButtonClick()
        {
            RefreshAsync();
        }

        private async void RefreshAsync()
        {
            try
            {
                foreach (var view in activeViews) pool.Release(view);

                activeViews.Clear();

                var infos = await ConnectionManager.Instance.QuerySessionsAsync();

                infos = infos.OrderBy<ISessionInfo, object>(s => s.HasPassword).ToList();

                foreach (var info in infos)
                {
                    if (info.IsLocked) continue;

                    var view = pool.Get();

                    activeViews.AddLast(view);

                    view.Bind(info);
                }
            }
            catch (Exception e)
            {
                print(e.Message);
            }
        }

        private void OnSelect(ISessionInfo sessionInfo)
        {
            selectedSession = sessionInfo;

            if (selectedSession != null)
                joinButton.interactable = true;
        }

        private SessionView OnCreatePooledObjects()
        {
            return Instantiate(sessionViewPrefab, contentParent).GetComponent<SessionView>();
        }

        private void OnGetPooledObjects(SessionView sessionView)
        {
            sessionView.gameObject.SetActive(true);
            sessionView.selected.AddListener(OnSelect);
            sessionView.transform.SetAsLastSibling();
        }

        private void OnReturnPooledObjects(SessionView sessionView)
        {
            sessionView.gameObject.SetActive(false);
            sessionView.selected.RemoveAllListeners();
        }

        private void OnDestroyPooledObjects(SessionView sessionView)
        {
            sessionView.selected.RemoveAllListeners();
            
            Destroy(sessionView.gameObject);
        }
    }
}