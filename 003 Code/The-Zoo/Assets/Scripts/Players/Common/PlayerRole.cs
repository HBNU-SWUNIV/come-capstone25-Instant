using Unity.Netcode;
using UnityEngine;

namespace Players.Common
{
    public enum Role
    {
        None,
        Observer,
        Hider,
        Seeker,
        Fighter,
    }

    public class PlayerRole : NetworkBehaviour
    {
        [SerializeField] protected Role role;
        [SerializeField] protected Transform interactPoint;
        [Tooltip("상호작용할 대상")][SerializeField] protected LayerMask interactLayer;
        [Tooltip("interact Point로부터의 거리")][SerializeField] protected float interactRange = 1f;
        [Tooltip("상호작용 범위")][SerializeField] protected float interactRadius = 1f;

        protected PlayerEntity entity;
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

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = GetRoleColor();
            Gizmos.DrawWireSphere(interactPoint.position + transform.forward * interactRange, interactRadius);
        }
#endif

        protected Collider Cast()
        {
            var count = Physics.OverlapSphereNonAlloc(
                interactPoint.position + transform.forward * interactRange,
                interactRadius, hits, interactLayer);

            if (count < 1) return null;

            Collider closest = null;
            var minSqrDist = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var col = hits[i];

                if (!col) continue;

                if (col.transform == transform) continue;

                var sqrDist = (col.transform.position - transform.position).sqrMagnitude;
                if (!(sqrDist < minSqrDist)) continue;

                minSqrDist = sqrDist;
                closest = col;
            }

            return closest;
        }

        protected virtual void TryInteract()
        {
            if (!IsOwner) return;

            var target = Cast();
            if (!target) return;

            print(target.name);

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
                Role.Fighter => GameManager.Instance
                    ? GameManager.Instance.roleColor.fighterColor
                    : Color.yellow,
                _ => GameManager.Instance
                    ? GameManager.Instance.roleColor.defaultColor
                    : Color.white
            };
        }
    }
}