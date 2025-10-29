using Players.Common;
using Unity.Netcode;
using Utils;

namespace Players.Roles
{
    public class SeekerRole : PlayerRole
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            if (!IsOwner) return;

            player.SetSpeed(4f);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (!IsOwner) return;

            player.SetSpeed(3f);
        }

        protected override void TryInteract()
        {
            player.fxHandler.PlayAttackFx();

            base.TryInteract();
        }

        [Rpc(SendTo.SpecifiedInParams, RequireOwnership = false)]
        protected override void RequestInteractionRpc(NetworkObjectReference targetRef,
            RpcParams rpcParams = default)
        {
            if (!targetRef.TryGet(out var no) || !no.IsSpawned) return;

            if (!no.TryGetComponent<HittableBody>(out var comp)) return;

            comp.Damaged(1, OwnerClientId);
        }
    }
}