using GamePlay.Spawner;
using Players.Common;
using Players.Roles;
using Players.Structs;
using Scriptable;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using Utils;

namespace Players
{
    public class PlayerEntity : NetworkBehaviour
    {
        [SerializeField] internal TMP_Text playerNameText;
        [SerializeField] internal SpriteRenderer playerMarker;

        public NetworkVariable<FixedString32Bytes> playerName = new();
        public NetworkVariable<AnimalType> animalType = new();
        public NetworkVariable<Role> role = new();

        private PlayerRenderer playerRenderer;

        public void Awake()
        {
            playerRenderer = GetComponent<PlayerRenderer>();
        }

        public void Initialize()
        {
            AlignForward();
            role.Value = Role.None;
            playerMarker.color = GameManager.Instance.roleColor.defaultColor;
            CameraManager.Instance.EnableCamera(true);

            foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
                NetworkShowRpc(id);
        }

        public override void OnNetworkSpawn()
        {
            playerName.OnValueChanged += OnPlayerNameChanged;
            animalType.OnValueChanged += OnAnimalTypeChanged;
            role.OnValueChanged += OnRoleChanged;

            OnPlayerNameChanged("", playerName.Value);
            OnRoleChanged(Role.None, role.Value);

            playerMarker.gameObject.SetActive(false);

            GameManager.Instance.players.OnListChanged += OnObserverChanged;

            if (!IsOwner) return;

            Initialize();

            playerMarker.gameObject.SetActive(true);

            playerName.Value = AuthenticationService.Instance.PlayerName;

            gameObject.AddComponent<AudioListener>();

            var data = new PlayerData(OwnerClientId, playerName.Value, animalType.Value,
                role.Value);
            GameManager.Instance.AddRpc(data);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            playerName.OnValueChanged -= OnPlayerNameChanged;
            animalType.OnValueChanged -= OnAnimalTypeChanged;
            role.OnValueChanged -= OnRoleChanged;

            GameManager.Instance.players.OnListChanged -= OnObserverChanged;

            if (!IsOwner) return;

            GameManager.Instance.RemoveRpc(OwnerClientId);

            GameManager.Instance.playerDict.Clear();

            PlayerSpawner.Instance.RemoveRpc(animalType.Value);

            CameraManager.Instance.EnableCamera(false);
        }

        internal void AlignForward()
        {
            var forward = Vector3.Cross(
                CameraManager.Instance.orbit.transform.right,
                transform.up).normalized;

            transform.rotation = Quaternion.LookRotation(forward, transform.up);

            CameraManager.Instance.LookMove();
        }

        [Rpc(SendTo.Everyone)]
        internal void ShowNameTagRpc(bool show)
        {
            playerNameText.gameObject.SetActive(show);
        }

        private void OnPlayerNameChanged(FixedString32Bytes prev, FixedString32Bytes current)
        {
            playerNameText.text = Util.GetPlayerNameWithoutHash(current.Value);
        }

        private void OnAnimalTypeChanged(AnimalType previousValue, AnimalType newValue)
        {
            if (!IsOwner) return;

            GameManager.Instance.SetAnimalTypeRpc(OwnerClientId, newValue);
        }

        private void OnRoleChanged(Role previousValue, Role newValue)
        {
            switch (newValue)
            {
                case Role.Observer:
                    gameObject.layer = LayerMask.NameToLayer("Observer");
                    gameObject.GetComponent<FighterRole>().enabled = false;
                    gameObject.GetComponent<SeekerRole>().enabled = false;
                    gameObject.GetComponent<HiderRole>().enabled = false;
                    playerRenderer.UseObserverShader();
                    break;
                case Role.Hider:
                    gameObject.layer = LayerMask.NameToLayer("Hider");
                    gameObject.GetComponent<FighterRole>().enabled = false;
                    gameObject.GetComponent<SeekerRole>().enabled = false;
                    gameObject.GetComponent<HiderRole>().enabled = true;
                    break;
                case Role.Seeker:
                    gameObject.layer = LayerMask.NameToLayer("Seeker");
                    gameObject.GetComponent<FighterRole>().enabled = false;
                    gameObject.GetComponent<SeekerRole>().enabled = true;
                    gameObject.GetComponent<HiderRole>().enabled = false;
                    break;
                case Role.Fighter:
                    gameObject.layer = LayerMask.NameToLayer("Fighter");
                    gameObject.GetComponent<FighterRole>().enabled = true;
                    gameObject.GetComponent<SeekerRole>().enabled = false;
                    gameObject.GetComponent<HiderRole>().enabled = false;
                    break;
                case Role.None:
                default:
                    gameObject.layer = LayerMask.NameToLayer("Default");
                    gameObject.GetComponent<FighterRole>().enabled = false;
                    gameObject.GetComponent<SeekerRole>().enabled = false;
                    gameObject.GetComponent<HiderRole>().enabled = false;
                    playerRenderer.UseOriginShader();
                    break;
            }

            if (!IsOwner) return;

            GameManager.Instance.SetRoleRpc(OwnerClientId, newValue);
        }

        private void OnObserverChanged(NetworkListEvent<PlayerData> changeEvent)
        {
            // 누군가 옵저버로 변했을 때만 작동, 내가 옵저버일 때만 아래 실행 함
            if (changeEvent.Type != NetworkListEvent<PlayerData>.EventType.Value) return;
            if (changeEvent.Value.role != Role.Observer) return;
            if (role.Value != Role.Observer) return;

            foreach (var client in NetworkManager.Singleton.ConnectedClientsIds)
            {
                // 내가 observer 일 때, 남들로부터 난 숨기고, 난 봐야함, 기존 옵저버도 나를 볼 수 있어야 함
                if (OwnerClientId == client) continue;
                if (GameManager.Instance.playerDict[client].role == Role.Observer)
                    NetworkShowRpc(changeEvent.Value.clientId);
                else
                    NetworkHideRpc(client);
            }
        }

        internal void ChangeObserver()
        {
            role.Value = Role.Observer;
        }

        [Rpc(SendTo.Authority)]
        private void NetworkShowRpc(ulong fromId)
        {
            if (OwnerClientId == fromId) return;

            if (NetworkObject.IsNetworkVisibleTo(fromId)) return;

            NetworkObject.NetworkShow(fromId);
        }

        [Rpc(SendTo.Authority)]
        private void NetworkHideRpc(ulong fromId)
        {
            if (OwnerClientId == fromId) return;

            if (!NetworkObject.IsNetworkVisibleTo(fromId)) return;

            NetworkObject.NetworkHide(fromId);
        }
    }
}