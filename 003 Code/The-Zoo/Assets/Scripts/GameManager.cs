using System;
using System.Collections.Generic;
using System.Linq;
using GamePlay;
using GamePlay.Spawner;
using Networks;
using Players;
using Players.Common;
using Players.Structs;
using Scriptable;
using UI;
using UI.Lobby.InformationPopup;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

public class GameManager : NetworkBehaviour
{
    [SerializeField] internal RoleColor roleColor;

    public NetworkVariable<int> readyCount = new();

    internal readonly Dictionary<ulong, (string name, AnimalType type, Role role)> playerDict = new();

    public readonly NetworkList<PlayerData> players = new();

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsSessionOwner)
        {
            players.Clear();
            readyCount.Value = 0;
        }

        players.OnListChanged += OnPlayersChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        players.OnListChanged -= OnPlayersChanged;
    }

    private void OnPlayersChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        print($"[{changeEvent.Type}]{changeEvent.Value.clientId}:{changeEvent.Value.name}:{changeEvent.Value.type}:{changeEvent.Value.role}");

        switch (changeEvent.Type)
        {
            case NetworkListEvent<PlayerData>.EventType.Add:
            case NetworkListEvent<PlayerData>.EventType.Insert:
            case NetworkListEvent<PlayerData>.EventType.Value:
                var pName = Util.GetPlayerNameWithoutHash(changeEvent.Value.name.Value);
                playerDict[changeEvent.Value.clientId] =
                    (pName, changeEvent.Value.type, changeEvent.Value.role);
                break;
            case NetworkListEvent<PlayerData>.EventType.Remove:
            case NetworkListEvent<PlayerData>.EventType.RemoveAt:
                playerDict.Remove(changeEvent.Value.clientId);
                break;
            case NetworkListEvent<PlayerData>.EventType.Clear:
                playerDict.Clear();
                break;
        }
    }

    [Rpc(SendTo.Authority)]
    internal void AddRpc(PlayerData data)
    {
        players.Add(data);
    }

    [Rpc(SendTo.Authority)]
    internal void RemoveRpc(ulong clientId)
    {
        for (var i = 0; i < players.Count; i++)
        {
            var data = players[i];
            if (data.clientId != clientId) continue;

            players.Remove(data);
            return;
        }
    }

    [Rpc(SendTo.Authority)]
    internal void SetRoleRpc(ulong clientId, Role role)
    {
        for (var i = 0; i < players.Count; i++)
        {
            var data = players[i];
            if (data.clientId != clientId) continue;

            data.role = role;
            players[i] = data;
            return;
        }
    }

    [Rpc(SendTo.Authority)]
    internal void SetAnimalTypeRpc(ulong clientId, AnimalType type)
    {
        for (var i = 0; i < players.Count; i++)
        {
            var data = players[i];
            if (data.clientId != clientId) continue;

            data.type = type;
            players[i] = data;
            return;
        }
    }

    [Rpc(SendTo.Authority)]
    private void ReadyRpc(bool isReady)
    {
        readyCount.Value = isReady ? readyCount.Value + 1 : readyCount.Value - 1;
    }

    internal int GetRoleCount(Role role)
    {
        return players.AsNativeArray().Count(x => x.role == role);
    }


    internal void Ready()
    {
        var checker = NetworkManager.Singleton.LocalClient.PlayerObject
            .GetComponent<PlayerReadyChecker>();

        ReadyRpc(checker.Toggle());
    }

    internal void GameStart()
    {
        try
        {
            if (!ConnectionManager.Instance.CurrentSession.IsHost) return;

            if (!CanGameStart()) throw new Exception("플레이어들이 준비되지 않았습니다");

            LoadSceneRpc("InGame");

            readyCount.Value = 0;

            ConnectionManager.Instance.LockSessionAsync();
        }
        catch (Exception e)
        {
            InformationPopup.instance.ShowPopup(e.Message);
        }
    }

    internal void GameEnd()
    {
        NpcSpawner.Instance.ClearRpc();
        PlayManager.Instance.intSpawner.ClearRpc();

        LoadSceneRpc("Lobby");

        ConnectionManager.Instance.UnlockSessionAsync();
    }

    internal void PromotedSessionHost(string playerId)
    {
        if (playerId == AuthenticationService.Instance.PlayerId)
            NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerReadyChecker>().isReady
                .Value = true;
        else
            NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerReadyChecker>().Initialize();
    }

    private bool CanGameStart()
    {
        foreach (var client in NetworkManager.ConnectedClientsList)
        {
            if (client.PlayerObject == null) return false;

            if (!client.PlayerObject.TryGetComponent<PlayerReadyChecker>(out var checker))
                return false;

            if (!checker.isReady.Value) return false;
        }

        return true;
    }

    [Rpc(SendTo.Authority)]
    private void LoadSceneRpc(string sceneName)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private async void OnApplicationQuit()
    {
        Debug.Log("[ConnectionManager] Graceful session leave on quit.");

        await ConnectionManager.Instance.DisconnectSessionAsync();
    }
}