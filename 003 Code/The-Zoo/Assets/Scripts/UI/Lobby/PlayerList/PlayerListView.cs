using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EventHandler;
using Networks;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace UI.Lobby.PlayerList
{
    public class PlayerListView : MonoBehaviour
    {
        [FormerlySerializedAs("playerViewPrefab")] [SerializeField] private GameObject itemPrefab;
        private readonly Dictionary<string, PlayerListItem> itemDict = new();

        private IObjectPool<PlayerListItem> pool;
        private ISession session;

        private readonly WaitForSeconds wait = new(3f);

        private void Awake()
        {
            pool = new ObjectPool<PlayerListItem>
            (
                CreatePoolObj,
                GetPoolObj,
                ReleasePoolObj,
                DestroyPoolObj,
                true, 4, 8
            );
        }

        private void OnEnable()
        {
            if (!UnityServices.State.Equals(ServicesInitializationState.Initialized)) return;

            if (!ConnectionManager.Instance) return;

            session = ConnectionManager.Instance.CurrentSession;

            foreach (var player in session.Players)
            {
                var item = pool.Get();

                item.Create(player);

                itemDict.Add(player.Id, item);

                if (player.Id == session.CurrentPlayer.Id) item.Highlight();
            }

            GamePlayEventHandler.PlayerReady += PlayerReady;
            session.PlayerJoined += OnPlayerJoined;
            session.PlayerHasLeft += OnPlayerHasLeft;
            session.SessionHostChanged += OnSessionHostChanged;
            session.SessionHostChanged += GameManager.Instance.PromotedSessionHost;

            itemDict[session.Host].Host(true);
        }

        private void OnDisable()
        {
            if (session == null) return;

            Clear();

            GamePlayEventHandler.PlayerReady -= PlayerReady;
            session.PlayerJoined -= OnPlayerJoined;
            session.PlayerHasLeft -= OnPlayerHasLeft;
            session.SessionHostChanged -= OnSessionHostChanged;
            session.SessionHostChanged -= GameManager.Instance.PromotedSessionHost;

            session = null;
        }

        private void PlayerReady(string playerId, bool value)
        {
            itemDict[playerId].Ready(value);
        }

        private IEnumerator JoinCo(string playerId)
        {
            yield return wait;

            var item = pool.Get();

            var player = session.Players.First(player => player.Id == playerId);

            item.Create(player);

            itemDict.Add(playerId, item);
        }

        private void OnPlayerJoined(string playerId)
        {
            StartCoroutine(JoinCo(playerId));
        }

        private void OnPlayerHasLeft(string playerId)
        {
            itemDict.Remove(playerId, out var player);
            pool.Release(player);
        }

        private void OnSessionHostChanged(string playerId)
        {
            foreach (var view in itemDict.Values) view.Host(false);

            itemDict[playerId].Host(true);
            itemDict[playerId].Ready(false);
        }

        private void Clear()
        {
            foreach (var kvp in itemDict) pool.Release(kvp.Value);

            pool.Clear();

            itemDict.Clear();
        }

        private PlayerListItem CreatePoolObj()
        {
            return Instantiate(itemPrefab, transform).GetComponent<PlayerListItem>();
        }

        private void GetPoolObj(PlayerListItem obj)
        {
            obj.gameObject.SetActive(true);
            obj.transform.SetAsLastSibling();
        }

        private void ReleasePoolObj(PlayerListItem obj)
        {
            obj.gameObject.SetActive(false);
        }

        private void DestroyPoolObj(PlayerListItem obj)
        {
            Destroy(obj.gameObject);
        }
    }
}