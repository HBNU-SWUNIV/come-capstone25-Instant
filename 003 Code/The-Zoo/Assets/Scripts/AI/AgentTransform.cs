using System.Collections;
using AI.Seeker;
using Players.Common;
using Unity.Netcode;
using UnityEngine;

namespace AI
{
    public class AgentTransform : CharacterBase
    {
        private const float GainJump = 6f;
        private const float GainSpin = 3f;
        private const float GainAttack = 15f;
        internal const float maxSuspicion = 150f;
        private const float SuspicionDecayPerSecond = 3f;
        internal bool isMove;
        internal bool isRun;
        internal bool isSpin;

        internal Vector2 lookInput;
        internal Vector2 moveInput;

        internal float suspicion = 60f;

        private bool lastSpin;
        private bool forceStop;

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            if (suspicion > 0f)
            {
                suspicion -= SuspicionDecayPerSecond * Time.fixedDeltaTime;
                if (suspicion < 0f) suspicion = 0f;
            }
        }

        private void Begin()
        {
            SpinHold = false;
            isSpin = false;
            isMove = false;
            isRun = false;
            forceStop = false;

            suspicion = 60f;

            Initialize(3);

            StartCoroutine(RandomStopRoutine());

            if (GameManager.Instance.gameMode.Value == GameManager.GameMode.LastStand)
            {
                SetSpeed(3f);
            }
            else
            {
                SetSpeed(2.5f);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            Begin();
        }

        internal void MoveAction(int action1, int action2)
        {
            moveInput.y = action1 switch
            {
                1 => 1f,
                2 => -1f,
                _ => 0f
            };
            moveInput.x = action2 switch
            {
                1 => 1f,
                2 => -1f,
                _ => 0f
            };

            if (forceStop)
            {
                moveInput = Vector2.zero;
            }

            isMove = moveInput != Vector2.zero;

            Move(moveInput);
        }

        internal void LookAction(int action)
        {
            var yaw = action switch
            {
                1 => 1f,
                2 => -1f,
                _ => 0f
            };

            Rotate(yaw);
        }

        internal void JumpAction(int action)
        {
            if (action != 1) return;

            Jump(AddSuspicion);
        }

        internal void SpinAction(int action)
        {
            isSpin = action == 1;

            Spin(isSpin, AddSuspicion);
        }

        internal void RunAction(int action)
        {
            isRun = action == 1;

            Run(isRun);
        }

        internal void AttackAction(int action)
        {
            if (action != 1) return;

            Attack(AddSuspicion);
        }

        private void AddSuspicion(HiderActionType type)
        {
            switch (type)
            {
                case HiderActionType.Jump:
                    suspicion = Mathf.Min(suspicion + GainJump, maxSuspicion);
                    break;
                case HiderActionType.Spin:
                    var info = animator.Animator.GetCurrentAnimatorStateInfo(0);

                    if (!info.IsName("Spin")) return;
                    if (info.normalizedTime >= 1.15f && !lastSpin)
                    {
                        lastSpin = true;
                        suspicion = Mathf.Min(suspicion + GainSpin, maxSuspicion);
                    }
                    else
                    {
                        lastSpin = false;
                    }

                    break;
                case HiderActionType.Attack:
                    suspicion = Mathf.Min(suspicion + GainAttack, maxSuspicion);
                    break;
            }
        }

        protected override IEnumerator DeathCo()
        {
            animator.OnDeath();

            isDead.Value = true;

            OnDeath();

            yield return respawnWait;

            RequestDespawnRpc(new NetworkObjectReference(NetworkObject));
        }

        private void OnDeath()
        {
            if (!IsOwner) return;

            var id = hBody.lastAttackerId;
            if(!NetworkManager.ConnectedClients.TryGetValue(id, out var player)) return;
            var targetRef = new NetworkObjectReference(player.PlayerObject);

            GiveDamageRpc(targetRef, RpcTarget.Single(id, RpcTargetUse.Temp));
        }

        private IEnumerator RandomStopRoutine()
        {
            while (IsSpawned)
            {
                var wait = Random.Range(5f, 30f);
                yield return new WaitForSeconds(wait);

                var stopTime = Random.Range(0.2f, 15f);
                forceStop = true;

                yield return new WaitForSeconds(stopTime);

                forceStop = false;
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void GiveDamageRpc(NetworkObjectReference targetRef, RpcParams rpcParams = default)
        {
            if (!targetRef.TryGet(out var no) || !no.IsSpawned) return;

            if (!no.TryGetComponent<HittableBody>(out var body)) return;

            body.Damaged(1, 0);
        }

        [Rpc(SendTo.Authority)]
        private void RequestDespawnRpc(NetworkObjectReference targetRef)
        {
            if (!targetRef.TryGet(out var no)) return;
            if (OwnerClientId != no.OwnerClientId) return;
            if (!no.IsSpawned) return;
            no.DeferDespawn(1);
        }
    }
}