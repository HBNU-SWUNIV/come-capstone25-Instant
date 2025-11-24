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
using UI.Lobby.InformationPopup;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

public class GameManager : NetworkBehaviour
{
    public enum GameMode
    {
        HideSeek,
        LastStand,
    }

    [SerializeField] internal RoleColor roleColor;

    public NetworkVariable<GameMode> gameMode = new();
    public NetworkVariable<int> readyCount = new();

    internal readonly Dictionary<ulong, (string name, AnimalType type, Role role)> playerDict = new();

    public readonly NetworkList<PlayerData> players = new();
    public NetworkVariable<int> hiderCount = new(0);
    public NetworkVariable<int> seekerCount = new(0);
    public NetworkVariable<int> fighterCount = new(0);

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
            readyCount.Value = 0;
            players.OnListChanged += OnPlayerListChanged;
        }

        players.OnListChanged += OnPlayersChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        players.OnListChanged -= OnPlayerListChanged;
        players.OnListChanged -= OnPlayersChanged;
    }

    private void OnPlayerListChanged(NetworkListEvent<PlayerData> change)
    {
        if (!IsSessionOwner) return;

        switch (change.Type)
        {
            case NetworkListEvent<PlayerData>.EventType.Add:
                AddRoleCount(change.Value.role);
                break;

            case NetworkListEvent<PlayerData>.EventType.Remove:
                RemoveRoleCount(change.Value.role);
                break;

            case NetworkListEvent<PlayerData>.EventType.Value:
                RemoveRoleCount(change.PreviousValue.role);
                AddRoleCount(change.Value.role);
                break;
        }
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

    internal int GetMyOrder(ulong clientId)
    {
        for (var i = 0; i < players.Count; i++)
        {
            if (players[i].clientId == clientId)
                return i;
        }

        return -1;
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

    private void AddRoleCount(Role role)
    {
        switch (role)
        {
            case Role.Hider:
                hiderCount.Value++;
                break;
            case Role.Seeker:
                seekerCount.Value++;
                break;
            case Role.Fighter:
                fighterCount.Value++;
                break;
        }
    }

    private void RemoveRoleCount(Role role)
    {
        switch (role)
        {
            case Role.Hider:
                hiderCount.Value--;
                break;
            case Role.Seeker:
                seekerCount.Value--;
                break;
            case Role.Fighter:
                fighterCount.Value--;
                break;
        }
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

            LoadSceneRpc(Util.InGameSceneName);

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

        LoadSceneRpc(Util.LobbySceneName);

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
}