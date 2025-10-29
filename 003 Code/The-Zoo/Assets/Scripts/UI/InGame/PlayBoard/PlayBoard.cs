using System.Collections;
using System.Collections.Generic;
using GamePlay;
using Players;
using Players.Common;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace UI.InGame.PlayBoard
{
    public class PlayBoard : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform contentParent;
        [SerializeField] private GameObject itemPrefab;

        private readonly Dictionary<ulong, PlayBoardItem> items = new();
        private readonly WaitForSecondsRealtime wait = new(3f);
        private float timer;

        private void Start()
        {
            Initialize();

            SetVisible(false);

            StartCoroutine(Ping());
        }

        private IEnumerator Ping()
        {
            while (PlayManager.Instance.gameBoot.Value)
            {
                yield return wait;

                UpdatePing();
            }
        }

        private void Initialize()
        {
            foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
                AddPlayer(id);
        }

        private void AddPlayer(ulong clientId)
        {
            if (items.ContainsKey(clientId)) return;

            var go = Instantiate(itemPrefab, contentParent);
            var item = go.GetComponent<PlayBoardItem>();

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            {
                var entity = client.PlayerObject.GetComponent<PlayerEntity>();
                var pName = entity ? entity.playerName.Value.ToString() : $"Player {clientId}";
                var role = entity.role.Value;
                item.SetInfo(pName, role, 0);
            }
            else
            {
                item.SetInfo($"Player {clientId}", Role.None, 0);
            }

            items[clientId] = item;
        }

        private void UpdatePing()
        {
            if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is not UnityTransport ut)
                return;

            foreach (var item in items)
            {
                var ping = ut.GetCurrentRtt(NetworkManager.Singleton.NetworkConfig.NetworkTransport.ServerClientId);
                item.Value.SetPing(ping);
            }
        }

        public void SetVisible(bool show)
        {
            canvasGroup.alpha = show ? 1 : 0;
            canvasGroup.interactable = show;
            canvasGroup.blocksRaycasts = show;
        }
    }
}