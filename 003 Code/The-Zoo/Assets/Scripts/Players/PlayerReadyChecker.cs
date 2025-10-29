using EventHandler;
using Networks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;

namespace Players
{
    public class PlayerReadyChecker : NetworkBehaviour
    {
        public NetworkVariable<FixedString32Bytes> playerId = new();
        public NetworkVariable<bool> isReady = new();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            isReady.OnValueChanged += OnPlayerReadyChanged;

            if (!IsOwner) return;

            playerId.Value = AuthenticationService.Instance.PlayerId;
            isReady.Value = ConnectionManager.Instance.CurrentSession.IsHost;
        }

        private void OnPlayerReadyChanged(bool previousValue, bool newValue)
        {
            GamePlayEventHandler.OnPlayerReady(playerId.Value.Value, newValue);
        }

        public void Initialize()
        {
            isReady.Value = IsSessionOwner;
        }

        public bool Toggle()
        {
            return isReady.Value = !isReady.Value;
        }
    }
}