using System;
using Mission;
using Unity.Netcode;
using UnityEngine;
using Utils;

namespace Players.Common
{
    public enum Role
    {
        None,
        Observer,
        Hider,
        Seeker,
    }

    public class PlayerRole : NetworkBehaviour
    {
        [SerializeField] protected Role role;
        [SerializeField] protected Transform interactPoint;
        [Tooltip("상호작용할 대상")][SerializeField] protected LayerMask interactLayer;
        [Tooltip("interact Point로부터의 거리")][SerializeField] protected float interactRange = 1f;
        [Tooltip("상호작용 범위")][SerializeField] protected float interactRadius = 1f;

        private PlayerEntity entity;
        protected PlayerController player;

        public Collider[] hits = new Collider[8];

        protected virtual void Awake()
        {
            entity = GetComponent<PlayerEntity>();
            player = GetComponent<PlayerController>();
        }

        protected virtual void OnEnable()
        {
            if (!IsOwner) return;

            entity.playerMarker.color = GetRoleColor();
            player.OnAttackCallback += TryInteract;
        }

        protected virtual void OnDisable()
        {
            if (!IsOwner) return;

            player.OnAttackCallback -= TryInteract;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = GetRoleColor();
            Gizmos.DrawWireSphere(interactPoint.position + transform.forward * interactRange, interactRadius);
        }

        protected Collider Cast()
        {
            var count = Physics.OverlapSphereNonAlloc(
                interactPoint.position + transform.forward * interactRange,
                interactRadius, hits, interactLayer);

            if (count < 1) return null;

            var closest = hits[0];
            var minSqrDist = (closest.transform.position - interactPoint.position).sqrMagnitude;

            for (var i = 1; i < count; i++)
            {
                var sqrDist = (hits[i].transform.position - interactPoint.position).sqrMagnitude;
                if (!(sqrDist < minSqrDist)) continue;

                minSqrDist = sqrDist;
                closest = hits[i];
            }

            return closest;
        }

        protected virtual void TryInteract()
        {
            if (!IsOwner) return;

            var target = Cast();
            if (!target) return;

            if (!target.TryGetComponent<NetworkObject>(out var no)) return;

            var targetRef = new NetworkObjectReference(no);

            RequestInteractionRpc(targetRef,
                RpcTarget.Single(no.OwnerClientId, RpcTargetUse.Temp));
        }

        protected virtual void RequestInteractionRpc(NetworkObjectReference targetRef, RpcParams param = default)
        {

        }

        internal Color GetRoleColor()
        {
            return role switch
            {
                Role.Hider => GameManager.Instance
                    ? GameManager.Instance.roleColor.hiderColor
                    : Color.green,
                Role.Seeker => GameManager.Instance
                    ? GameManager.Instance.roleColor.seekerColor
                    : Color.red,
                _ => GameManager.Instance
                    ? GameManager.Instance.roleColor.defaultColor
                    : Color.white
            };
        }
    }
}