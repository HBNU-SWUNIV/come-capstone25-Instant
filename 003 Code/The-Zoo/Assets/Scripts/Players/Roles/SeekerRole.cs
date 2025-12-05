using System.Collections;
using Planet;
using Players.Common;
using Unity.Netcode;
using UnityEngine;

namespace Players.Roles
{
    public class SeekerRole : PlayerRole
    {
        [SerializeField] private float speed = 5f;
        [SerializeField] private float lungeDuration = 0.12f;
        [SerializeField] private float lungeDistance = 0.4f;

        private readonly WaitForSeconds delay = new (0.05f);
        private bool isLunging;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (!IsOwner) return;

            player.SetSpeed(speed);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (!IsOwner) return;

            player.SetSpeed(speed);
        }

        private IEnumerator LungeRoutine()
        {
            isLunging = true;

            var elapsed = 0f;
            var up = (transform.position - PlanetGravity.Instance.transform.position).normalized;
            var forwardOnSurface = Vector3.ProjectOnPlane(transform.forward, up).normalized;

            while (elapsed < lungeDuration)
            {
                var delta = (lungeDistance / lungeDuration) * Time.deltaTime;
                player.rBody.MovePosition(player.rBody.position + forwardOnSurface * delta);

                elapsed += Time.deltaTime;
                yield return null;
            }

            isLunging = false;
        }

        private IEnumerator TryAttackWithLunge()
        {
            if (isLunging) yield break;

            StartCoroutine(LungeRoutine());

            yield return delay;

            player.fxHandler.PlayAttackFx();

            base.TryInteract();
        }

        protected override void TryInteract()
        {
            StartCoroutine(TryAttackWithLunge());
        }

        [Rpc(SendTo.SpecifiedInParams)]
        protected override void RequestInteractionRpc(NetworkObjectReference targetRef,
            RpcParams rpcParams = default)
        {
            if (!targetRef.TryGet(out var no) || !no.IsSpawned) return;

            if (!no.TryGetComponent<HittableBody>(out var comp)) return;

            comp.Damaged(1, OwnerClientId);
            comp.KnockBackRpc(transform.position, 20f, 0.3f);
        }
    }
}