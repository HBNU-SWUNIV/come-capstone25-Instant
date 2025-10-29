using System.Collections;
using GamePlay.Spawner;
using Interactions;
using Mission;
using Networks;
using Planet;
using Players;
using Players.Common;
using Players.Structs;
using UI.InGame;
using UI.InGame.GameResult;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace GamePlay
{
    public class PlayManager : NetworkBehaviour
    {
        [SerializeField] private InGameUI inGame;
        [SerializeField] private GameResultUI gameResult;
        [SerializeField] private LoadingUI loading;

        public NetworkVariable<bool> gameBoot = new();
        public NetworkVariable<int> currentTime = new();
        public NetworkVariable<int> sharedSeed = new();
        private readonly WaitForSeconds interval = new(1.0f);
        private readonly WaitForSeconds spawningWait = new(1.5f);

        private readonly WaitForSeconds wait = new(0.25f);

        internal EnvironmentSpawner envSpawner;
        internal InteractionSpawner intSpawner;
        internal MissionManager missionManager;
        internal RoleManager roleManager;

        internal bool gameInitialized;
        private int gameTime = 300;

        public static PlayManager Instance { get; private set; }

        public void Awake()
        {
            if (!Instance) Instance = this;
            else Destroy(gameObject);
        }

        public void OnEnable()
        {
            gameResult.gameObject.SetActive(false);
        }

        public override void OnNetworkSpawn()
        {
            intSpawner = GetComponent<InteractionSpawner>();
            envSpawner = GetComponent<EnvironmentSpawner>();
            missionManager = GetComponent<MissionManager>();
            roleManager = GetComponent<RoleManager>();

            if (IsSessionOwner) sharedSeed.Value = Random.Range(0, 1000000);

            if (!IsOwner) return;

            var time = ConnectionManager.Instance.CurrentSession.Properties[Util.GAMETIME].Value;
            
            gameTime = int.Parse(time);
            gameBoot.OnValueChanged += OnGameBootChanged;
            currentTime.OnValueChanged += CheckTimeOut;
            GameManager.Instance.players.OnListChanged += CheckWin;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;

            gameBoot.OnValueChanged -= OnGameBootChanged;
            currentTime.OnValueChanged -= CheckTimeOut;
        }

        private void OnGameBootChanged(bool previousValue, bool newValue)
        {
            if (!IsSessionOwner) return;

            if (newValue) StartCoroutine(GameStartCo());
            else roleManager.UnassignRole();
        }

        private void CheckWin(NetworkListEvent<PlayerData> changeEvent)
        {
            if (!gameInitialized) return;

            if (changeEvent.Type != NetworkListEvent<PlayerData>.EventType.Value) return;

            if (GameManager.Instance.GetRoleCount(Role.Seeker) <= 0)
                ShowResultRpc(false);
            else if (GameManager.Instance.GetRoleCount(Role.Hider) <= 0) ShowResultRpc(true);
        }

        private void CheckTimeOut(int previousValue, int newValue)
        {
            if (!gameInitialized) return;

            if (newValue < gameTime) return;

            ShowResultRpc(false);
        }

        protected override void OnInSceneObjectsSpawned()
        {
            if (!IsSessionOwner) return;

            base.OnInSceneObjectsSpawned();

            SetGameStateRpc(true);
        }

        [Rpc(SendTo.Authority, RequireOwnership = false)]
        private void SetGameStateRpc(bool started)
        {
            gameBoot.Value = started;
        }

        [Rpc(SendTo.Everyone)]
        private void ShowResultRpc(bool isSeekerWin)
        {
            StartCoroutine(ShowResultCo(isSeekerWin));
        }

        private IEnumerator ShowResultCo(bool isSeekerWin)
        {
            yield return wait;

            inGame.Unsubscribe();

            GameManager.Instance.players.OnListChanged -= CheckWin;

            gameResult.SetButtonActive(IsSessionOwner);

            yield return wait;

            gameResult.OnGameResult(isSeekerWin);

            yield return wait;

            gameResult.gameObject.SetActive(true);

            SetGameStateRpc(false);

            if (IsSessionOwner)
                AutoReturn();
        }

        [Rpc(SendTo.Everyone)]
        private void HideLoadingRpc()
        {
            loading.gameObject.SetActive(false);
        }

        [Rpc(SendTo.Everyone)]
        private void MoveRandomPositionRpc()
        {
            var obj = NetworkManager.Singleton.LocalClient.PlayerObject;

            var dir = Random.onUnitSphere;
            var pos = PlanetGravity.Instance.GetSurfacePoint(dir, out _);

            obj.transform.position = pos;
        }

        private IEnumerator GameStartCo()
        {
            PlayerLocator.LocalPlayer.playerInput.InputActions.Disable();

            MoveRandomPositionRpc();

            yield return null;

            intSpawner.SpawnRpc();

            yield return null;

            envSpawner.SpawnRpc(sharedSeed.Value);

            yield return wait;

            roleManager.AssignRole(sharedSeed.Value);

            yield return wait;

            yield return wait;

            yield return StartCoroutine(SpawnNpcCo());

            yield return wait;

            missionManager.StartMission();

            HideLoadingRpc();

            StartCoroutine(CountTimeCo());

            PlayerLocator.LocalPlayer.playerInput.InputActions.Enable();

            gameInitialized = true;
        }

        private IEnumerator SpawnNpcCo()
        {
            var count = ConnectionManager.Instance.CurrentSession.Properties[Util.NPCCOUNT].Value;
            var npc = int.Parse(count);

            yield return spawningWait;

            foreach (var data in GameManager.Instance.playerDict.Values)
            {
                if (data.role != Role.Hider) continue;

                NpcSpawner.Instance.SpawnRpc(data.type, npc);
            }
        }

        private IEnumerator CountTimeCo()
        {
            while (gameBoot.Value)
            {
                yield return interval;

                currentTime.Value += 1;
            }
        }

        internal void AutoReturn()
        {
            StartCoroutine(AutoReturnCo());
        }

        private IEnumerator AutoReturnCo()
        {
            yield return new WaitForSecondsRealtime(7f);

            if(SceneManager.GetActiveScene().name == "InGame")
                GameManager.Instance.GameEnd();
        }
    }
}