using System.Collections;
using System.Linq;
using EventHandler;
using GamePlay;
using GamePlay.Spawner;
using Mission;
using Players.Common;
using Scriptable;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Utils;

namespace Players
{
    public class PlayerController : CharacterBase
    {
        internal PlayerBuff buff;
        internal PlayerEntity entity;
        internal PlayerInputHandler playerInput;
        private PlayerReadyChecker readyChecker;
        private bool isAround;

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            UpdateMovement();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsOwner) return;

            if (other.CompareTag("Mission")) MissionNotifier.Instance.NotifyStayRock(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsOwner) return;

            if (other.CompareTag("Mission")) MissionNotifier.Instance.NotifyStayRock(false);
        }

        public void ApplyMouseSensitivity(float value)
        {
            sensitivity = Mathf.Clamp(value, 0.02f, 5f);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner) return;

            buff = GetComponent<PlayerBuff>();
            entity = GetComponent<PlayerEntity>();
            playerInput = GetComponent<PlayerInputHandler>();
            readyChecker = GetComponent<PlayerReadyChecker>();

            PlayerLocator.Set(this);
            Initialize(3);
            Subscribe();

            var sens = PlayerPrefs.GetFloat("opt_mouse_sens", sensitivity);
            ApplyMouseSensitivity(sens);

            PivotBinder.Instance?.BindPivot(transform);
            CameraManager.Instance.Initialize(transform);

            playerInput.HideCursor();

            StartCoroutine(PositionSetting());
        }

        private IEnumerator PositionSetting()
        {
            yield return new WaitForSeconds(0.2f);

            var index = NetworkManager.ConnectedClientsIds.Count % 4;
            InitialLobbyPosition(index);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (!IsOwner) return;

            PlayerLocator.Clear();

            Unsubscribe();

            playerInput.ShowCursor();
        }

        protected override IEnumerator DeathCo()
        {
            animator.OnDeath();

            isDead.Value = true;

            yield return respawnWait;

            var sessionOwner = NetworkManager.Singleton.CurrentSessionOwner;
            AllNpcDamagedRpc(entity.animalType.Value, RpcTarget.Single(sessionOwner, RpcTargetUse.Temp));

            animator.Rebind();

            isDead.Value = false;
            CanMove = true;

            entity.ChangeObserver();
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void AllNpcDamagedRpc(AnimalType type, RpcParams rpcParams = default)
        {
            var list = NpcSpawner.Instance.spawnedNpc
                .Where(d => d.type == type)
                .Select(d => d.npc)
                .ToList();

            foreach (var npc in list)
            {
                if (!npc) continue;
                if (!npc.IsSpawned) continue;
                if (!npc.TryGetComponent<HittableBody>(out var body)) return;

                body.Damaged(3, 0);
            }
        }

        private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
        {
            if (!IsOwner) return;
            if (clientId != NetworkManager.LocalClientId) return;

            Initialize(3);

            buff.Initialize();
            entity.AlignForward();
            playerInput.HideCursor();

            entity.ShowNameTagRpc(false);

            if (!sceneName.Equals(Util.LobbySceneName)) return;

            GamePlayEventHandler.OnUIChanged(Util.LobbySceneName);

            entity.ShowNameTagRpc(true);
            entity.Initialize();
            readyChecker.Initialize();

            var index = GameManager.Instance.GetMyOrder(clientId);

            InitialLobbyPosition(index);
        }

        private void Subscribe()
        {
            if (!IsOwner) return;

            NetworkManager.SceneManager.OnLoadComplete += OnSceneLoadComplete;

            playerInput.InputActions.Player.RightClick.performed += Rmb;
            playerInput.InputActions.Player.RightClick.canceled += Rmb;

            playerInput.InputActions.Player.Look.performed += OnLookInput;
            playerInput.InputActions.Player.Look.canceled += OnLookInput;

            playerInput.InputActions.Player.Run.performed += OnRunInput;
            playerInput.InputActions.Player.Run.canceled += OnRunInput;

            playerInput.InputActions.Player.Spin.started += OnSpinInput;
            playerInput.InputActions.Player.Spin.canceled += OnSpinInput;

            playerInput.InputActions.Player.Jump.started += OnJumpPerformed;
            playerInput.InputActions.Player.Attack.started += OnAttackStarted;
        }

        private void Unsubscribe()
        {
            if (!IsOwner) return;

            NetworkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;

            playerInput.InputActions.Player.RightClick.performed -= Rmb;
            playerInput.InputActions.Player.RightClick.canceled -= Rmb;

            playerInput.InputActions.Player.Look.performed -= OnLookInput;
            playerInput.InputActions.Player.Look.canceled -= OnLookInput;

            playerInput.InputActions.Player.Run.performed -= OnRunInput;
            playerInput.InputActions.Player.Run.canceled -= OnRunInput;

            playerInput.InputActions.Player.Spin.started -= OnSpinInput;
            playerInput.InputActions.Player.Spin.canceled -= OnSpinInput;

            playerInput.InputActions.Player.Jump.started -= OnJumpPerformed;
            playerInput.InputActions.Player.Attack.started -= OnAttackStarted;
        }

        private void Rmb(InputAction.CallbackContext ctx)
        {
            isAround = ctx.performed;
        }

        private void UpdateMovement()
        {
            var moveInput = playerInput.MoveInput;

            Move(moveInput);
        }

        private void OnLookInput(InputAction.CallbackContext ctx)
        {
            if (!CanMove) return;

            if (isAround)
            {
                CameraManager.Instance.LookAround();
            }
            else
            {
                CameraManager.Instance.LookMove();

                Rotate(playerInput.LookInput.x);

                CameraManager.Instance.SetEulerAngles(transform.rotation.eulerAngles.y);
            }
        }

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            Jump();
        }

        private void OnRunInput(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) Run(true);
            if (ctx.canceled) Run(false);
        }

        private void OnAttackStarted(InputAction.CallbackContext ctx)
        {
            Attack();
        }

        private void OnSpinInput(InputAction.CallbackContext ctx)
        {
            if(ctx.started) Spin(true);
            else if(ctx.canceled) Spin(false);
        }

        public void ApplyRandomReward()
        {
            if (!IsOwner) return;

            if (buff.RemoveBuff()) return;

            buff.CreateBuff(BuffType.SPEED, true);
        }

        public void ApplyRandomPenalty()
        {
            if (!IsOwner) return;

            var type = Random.Range(0, (int) BuffType.End);
            buff.CreateBuff((BuffType) type, false);
        }
    }
}