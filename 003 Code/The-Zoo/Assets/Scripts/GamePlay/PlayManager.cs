using System;
using System.Collections;
using System.Collections.Generic;
using GamePlay.Spawner;
using Interactions;
using Mission;
using Networks;
using Planet;
using Players;
using Players.Common;
using Players.Structs;
using Scriptable;
using UI.InGame;
using UI.InGame.GameResult;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;
using Random = UnityEngine.Random;

namespace GamePlay
{
    public class PlayManager : NetworkBehaviour
    {
        [Header("UI Refs")]
        [SerializeField] private InGameUI inGame;
        [SerializeField] private GameResultUI gameResult;
        [SerializeField] private LoadingUI loading;

        [Header("Network Vars")]
        public NetworkVariable<bool> gameLoop = new();
        public NetworkVariable<int> currentTime = new();
        public NetworkVariable<int> sharedSeed = new();

        internal EnvironmentSpawner envSpawner;
        internal InteractionSpawner intSpawner;
        internal MissionManager missionManager;
        internal RoleManager roleManager;
        internal TornadoManager tornadoManager;

        private int gameTime = 300;
        private bool gameInitialized;
        private double gameStartTimeServer;
        private double lastTimeSyncServer;

        private const float SchedulerCheckInterval = 0.05f; // 20Hz
        private float schedulerAccum = 0f;

        private readonly List<ScheduledTask> schedule = new();
        public static PlayManager Instance { get; private set; }

        public void Awake()
        {
            if (!Instance) Instance = this;
            else Destroy(gameObject);
        }

        public override void OnNetworkSpawn()
        {
            envSpawner = GetComponent<EnvironmentSpawner>();
            intSpawner = GetComponent<InteractionSpawner>();
            missionManager = GetComponent<MissionManager>();
            roleManager = GetComponent<RoleManager>();
            tornadoManager = GetComponent<TornadoManager>();

            if (!IsOwner) return;

            gameTime = int.Parse(ConnectionManager.Instance.CurrentSession
                .Properties[Util.GAMETIME].Value);

            gameLoop.OnValueChanged += OnGameLoopChanged;
            currentTime.OnValueChanged += CheckTimeOut;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;

            gameLoop.OnValueChanged -= OnGameLoopChanged;
            currentTime.OnValueChanged -= CheckTimeOut;
        }

        protected override void OnInSceneObjectsSpawned()
        {
            base.OnInSceneObjectsSpawned();

            if (!IsSessionOwner) return;

            StartGameFlow();
        }

        private void StartGameFlow()
        {
            if (IsSessionOwner)
                sharedSeed.Value = Random.Range(0, 1_000_000);

            gameStartTimeServer = NetworkManager.ServerTime.Time;
            lastTimeSyncServer = gameStartTimeServer;
            gameInitialized = false;
            schedule.Clear();
            schedulerAccum = 0f;

            SetGameLoopRpc(true);

            SetupInitialSequence();
            SetupEventTimeline(GameManager.Instance.gameMode.Value);
        }

        // ----------------------------
        //  초기 연출 & 게임 시작 스텝
        // ----------------------------
        private void SetupInitialSequence()
        {
            PlayerLocator.LocalPlayer.playerInput.enabled = false;

            if (!IsSessionOwner) return;

            MoveRandomRpc();

            envSpawner.SpawnRpc(sharedSeed.Value);

            var mode = GameManager.Instance.gameMode.Value;

            switch (mode)
            {
                case GameManager.GameMode.HideSeek:
                    intSpawner.SpawnRpc();
                    roleManager.AssignRole(sharedSeed.Value);
                    missionManager.InitializeForHideSeek();
                    break;

                case GameManager.GameMode.LastStand:
                    roleManager.AssignRole();
                    missionManager.LastStandModeRpc();

                    break;
            }

            // 2) NPC 스폰, 로딩 종료, 게임 시작 스텝을 절대시간 기준으로 스케줄링
            AddOneShotTask(1.5, SpawnNpcOnce);
            AddOneShotTask(2.1, HideLoadingRpc);
            AddOneShotTask(2.2, StartGameplayStep);
        }

        private void StartGameplayStep()
        {
            if (IsSessionOwner)
            {
                GameManager.Instance.players.OnListChanged += OnPlayersChanged;
            }

            gameInitialized = true;

            PlayerLocator.LocalPlayer.playerInput.enabled = true;
        }

        private void SetupEventTimeline(GameManager.GameMode mode)
        {
            // Hide & Seek 모드: 20초 후 첫 미션, 이후 60초 주기
            if (mode == GameManager.GameMode.HideSeek && missionManager)
            {
                AddRepeatingTask(20.0, 60.0, () => missionManager.ExecuteRandomMissionServer());
            }
            else if (mode == GameManager.GameMode.LastStand)
            {
                AddRepeatingTask(20.0, 40.0, GiantEvent);
            }

            // 토네이도: 30초 후 최초, 이후 30초 주기
            if (tornadoManager)
            {
                AddRepeatingTask(30.0, 30.0, () => tornadoManager.SpawnOnceServer());
            }
        }

        private void GiantEvent()
        {
            var livingPlayers = new List<NetworkClient>();
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject.TryGetComponent<CharacterBase>(out var character) &&
                    !character.isDead.Value)
                {
                    livingPlayers.Add(client);
                }
            }

            if (livingPlayers.Count > 0)
            {
                var target = livingPlayers[Random.Range(0, livingPlayers.Count)];

                var targetRef = new NetworkObjectReference(target.PlayerObject);

                RequestSizeChangeRpc(targetRef, RpcTarget.Single(target.ClientId, RpcTargetUse.Temp));

                NotifyRpc("누군가 10초 동안 거대해집니다!");
            }

        }

        [Rpc(SendTo.SpecifiedInParams)]
         private void RequestSizeChangeRpc(NetworkObjectReference targetRef, RpcParams rpcParams = default)
        {
            if (!targetRef.TryGet(out var no) || !no.IsSpawned) return;

            if (!no.TryGetComponent<PlayerController>(out var comp)) return;

            comp.ApplyGient();
        }

        private void Update()
        {
            // 호스트 + 게임 진행 중일 때만 동작
            if (!IsSessionOwner || !gameLoop.Value)
                return;

            schedulerAccum += Time.deltaTime;
            if (schedulerAccum < SchedulerCheckInterval)
                return;

            schedulerAccum = 0f;

            var now = NetworkManager.ServerTime.Time;

            TickScheduler(now);
            UpdateCurrentTime(now);
        }

        private void TickScheduler(double now)
        {
            if (schedule.Count == 0)
                return;

            List<int> toRemove = null;

            for (int i = 0; i < schedule.Count; i++)
            {
                var task = schedule[i];
                if (now < task.executeTime) continue;

                task.action?.Invoke();

                if (task.repeating)
                {
                    task.executeTime += task.interval;
                }
                else
                {
                    toRemove ??= new List<int>();
                    toRemove.Add(i);
                }
            }

            if (toRemove != null)
            {
                for (int i = toRemove.Count - 1; i >= 0; i--)
                {
                    schedule.RemoveAt(toRemove[i]);
                }
            }
        }

        private void UpdateCurrentTime(double now)
        {
            if (now - lastTimeSyncServer < 1.0)
                return;

            var elapsed = (int)(now - gameStartTimeServer);
            if (elapsed < 0) elapsed = 0;

            if (elapsed != currentTime.Value)
                currentTime.Value = elapsed;

            lastTimeSyncServer = now;
        }

        private void AddOneShotTask(double offsetSeconds, Action action)
        {
            schedule.Add(new ScheduledTask
            {
                executeTime = gameStartTimeServer + offsetSeconds,
                interval = 0,
                repeating = false,
                action = action
            });
        }

        private void AddRepeatingTask(double firstOffsetSeconds, double intervalSeconds, Action action)
        {
            schedule.Add(new ScheduledTask
            {
                executeTime = gameStartTimeServer + firstOffsetSeconds,
                interval = intervalSeconds,
                repeating = true,
                action = action
            });
        }

        private void OnGameLoopChanged(bool previousValue, bool newValue)
        {
            if (!IsSessionOwner) return;

            if (!newValue)
            {
                roleManager.UnassignRole();
            }
        }

        private void CheckTimeOut(int prev, int now)
        {
            if (!gameInitialized) return;
            if (now < gameTime) return;

            currentTime.OnValueChanged -= CheckTimeOut;

            switch (GameManager.Instance.gameMode.Value)
            {
                case GameManager.GameMode.HideSeek:
                    EndGameRpc(Role.Hider);
                    break;
                case GameManager.GameMode.LastStand:
                    EndGameRpc(Role.Fighter);
                    break;
            }
        }

        [Rpc(SendTo.Everyone)]
        private void HideLoadingRpc()
        {
            loading.gameObject.SetActive(false);
        }

        [Rpc(SendTo.Everyone)]
        private void MoveRandomRpc()
        {
            var obj = NetworkManager.Singleton.LocalClient.PlayerObject;
            var pos = PlanetGravity.Instance.GetSurfacePoint(out var normal);
            obj.transform.position = pos + normal * 1f;
        }

        private void SpawnNpcOnce()
        {
            var npcCount = int.Parse(ConnectionManager.Instance.CurrentSession
                .Properties[Util.NPCCOUNT].Value);

            List<AnimalType> hiderTypes = new();

            foreach (var data in GameManager.Instance.playerDict.Values)
            {
                if (data.role is Role.Hider or Role.Fighter)
                    hiderTypes.Add(data.type);
            }

            NpcSpawner.Instance.SpawnBatchRpc(hiderTypes.ToArray(), npcCount);
        }

        [Rpc(SendTo.Everyone)]
        public void NotifyRpc(string text)
        {
            inGame.Notify(text);
        }

        private void OnPlayersChanged(NetworkListEvent<PlayerData> _)
        {
            if (!gameInitialized) return;

            if (GameManager.Instance.gameMode.Value == GameManager.GameMode.HideSeek)
            {
                CheckHideSeekWinner();
            }
            else
            {
                CheckLastStanding();
            }
        }

        private void CheckHideSeekWinner()
        {
            var gm = GameManager.Instance;

            if (gm.seekerCount.Value == 0)
                EndGameRpc(Role.Hider);

            if (gm.hiderCount.Value == 0)
                EndGameRpc(Role.Seeker);
        }

        private void CheckLastStanding()
        {
            var gm = GameManager.Instance;

            if (gm.fighterCount.Value <= 1)
                EndGameRpc(Role.Fighter);
        }

        [Rpc(SendTo.Everyone)]
        private void EndGameRpc(Role winner)
        {
            GameManager.Instance.players.OnListChanged -= OnPlayersChanged;

            inGame.Unsubscribe();

            if (IsSessionOwner)
            {
                var snapshot = BuildResultSnapshot();
                ShowResultRpc(winner, snapshot);
            }

            schedule.Clear();
            SetGameLoopRpc(false);
            StartCoroutine(ReturnLobbyCo());
        }

        [Rpc(SendTo.Everyone)]
        private void ShowResultRpc(Role winner, GameResultDto[] snapshot)
        {
            gameResult.MakeResults(winner, snapshot);
            gameResult.SetButtonActive(IsSessionOwner);
            gameResult.SetVisible(true);
        }

        private GameResultDto[] BuildResultSnapshot()
        {
            var players = GameManager.Instance.players.AsNativeArray();
            var list = new List<GameResultDto>(players.Length);

            foreach (var p in players)
            {
                list.Add(new GameResultDto
                {
                    clientId = p.clientId,
                    role = p.role,
                    name = p.name.Value,
                });
            }

            return list.ToArray();
        }

        private IEnumerator ReturnLobbyCo()
        {
            yield return new WaitForSecondsRealtime(7f);

            if (IsSessionOwner)
            {
                AutoReturn();
            }
        }

        private void AutoReturn()
        {
            if (SceneManager.GetActiveScene().name == Util.InGameSceneName)
                GameManager.Instance.GameEnd();
        }

        [Rpc(SendTo.Authority)]
        private void SetGameLoopRpc(bool started)
        {
            gameLoop.Value = started;
        }

        private class ScheduledTask
        {
            public double executeTime;
            public double interval;
            public bool repeating;
            public Action action;
        }
    }
}