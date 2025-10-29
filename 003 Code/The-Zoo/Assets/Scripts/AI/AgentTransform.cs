using System.Collections;
using AI.Seeker;
using Players.Common;
using Unity.Netcode;
using UnityEngine;

namespace AI
{
    public class AgentTransform : CharacterBase
    {
        private const float GainJump = 10f;
        private const float GainSpin = 5f;
        private const float GainAttack = 30f;
        private const float MaxSuspicion = 130f;
        internal const float SuspicionThreshold = 120f;
        internal bool isRun;

        internal bool isSpin;

        private bool lastSpin;
        internal Vector2 lookInput;

        internal Vector2 moveInput;

        internal float suspicion;

        private void Begin()
        {
            SpinHold = false;
            isSpin = false;
            isRun = false;

            suspicion = 0f;

            Initialize(3);
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
                    suspicion = Mathf.Min(suspicion + GainJump, MaxSuspicion);
                    break;
                case HiderActionType.Spin:
                    var info = animator.Animator.GetCurrentAnimatorStateInfo(0);

                    if (!info.IsName("Spin")) return;
                    if (info.normalizedTime >= 1.15f && !lastSpin)
                    {
                        lastSpin = true;
                        suspicion = Mathf.Min(suspicion + GainSpin, MaxSuspicion);
                    }
                    else
                    {
                        lastSpin = false;
                    }

                    break;
                case HiderActionType.Attack:
                    suspicion = Mathf.Min(suspicion + GainAttack, MaxSuspicion);
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
            var player = NetworkManager.ConnectedClients[id].PlayerObject;
            var targetRef = new NetworkObjectReference(player);

            GiveDamageRpc(targetRef, RpcTarget.Single(id, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void GiveDamageRpc(NetworkObjectReference targetRef, RpcParams rpcParams = default)
        {
            if (!targetRef.TryGet(out var no) || !no.IsSpawned) return;

            if (!no.TryGetComponent<HittableBody>(out var body)) return;

            body.Damaged(1, 0);
        }

        [Rpc(SendTo.Authority, RequireOwnership = false)]
        private void RequestDespawnRpc(NetworkObjectReference targetRef)
        {
            if (!targetRef.TryGet(out var no)) return;
            if (OwnerClientId != no.OwnerClientId) return;
            if (!no.IsSpawned) return;
            no.DeferDespawn(1);
        }
    }
}