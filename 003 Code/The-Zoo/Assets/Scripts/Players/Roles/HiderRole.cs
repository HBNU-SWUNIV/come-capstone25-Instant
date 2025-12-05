using EventHandler;
using Interactions;
using Players.Common;
using Unity.Netcode;
using UnityEngine;

namespace Players.Roles
{
    public class HiderRole : PlayerRole
    {
        [SerializeField] private float speed = 3f;
        private InteractableObject currentObject;

        protected override void OnEnable()
        {
            base.OnEnable();

            currentObject = null;

            player.SetSpeed(speed);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (!IsOwner) return;

            player.SetSpeed(speed);
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            CheckInteraction();
        }

        private void CheckInteraction()
        {
            var target = Cast();
            if (!target || !target.TryGetComponent(out currentObject))
            {
                Unfocus();
                return;
            }

            Focus();
        }

        private void Focus()
        {
            if (!currentObject) return;

            if (currentObject is SpawningObject spawningObject)
            {
                var b = spawningObject.CanSpawn();
                GamePlayEventHandler.OnCheckInteractable(true, b);
            }
            else
            {
                GamePlayEventHandler.OnCheckInteractable(true, true);
            }
        }

        private void Unfocus()
        {
            GamePlayEventHandler.OnCheckInteractable(false, false);
        }

        [Rpc(SendTo.SpecifiedInParams)]
        protected override void RequestInteractionRpc(NetworkObjectReference targetRef,
            RpcParams param = default)
        {
            if (!targetRef.TryGet(out var no) || !no.IsSpawned) return;

            if (!no.TryGetComponent<InteractableObject>(out var component)) return;

            component.Interact(param.Receive.SenderClientId);
        }
    }
}