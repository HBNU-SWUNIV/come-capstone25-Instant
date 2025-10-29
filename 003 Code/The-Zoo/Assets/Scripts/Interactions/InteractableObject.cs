using System;
using System.Collections;
using Players.Common;
using Unity.Netcode;

namespace Interactions
{
    public abstract class InteractableObject : NetworkBehaviour
    {
        public event Action<InteractableObject> InteractionCompleted;

        public NetworkVariable<bool> isBusy = new();

        private FxHandler fxHandler;
        protected ulong lastInteractorId;

        public virtual void Awake()
        {
            fxHandler = GetComponent<FxHandler>();
        }

        internal void Interact(ulong clientId)
        {
            if (isBusy.Value) return;

            lastInteractorId = clientId;

            PlayHitFxRpc();

            StartCoroutine(InteractCo());

            OnCompleteInteraction();
        }

        [Rpc(SendTo.Everyone)]
        private void PlayHitFxRpc()
        {
            fxHandler?.PlayHitFx();
        }

        protected abstract IEnumerator InteractCo();

        private void OnCompleteInteraction()
        {
            InteractionCompleted?.Invoke(this);
        }
    }
}