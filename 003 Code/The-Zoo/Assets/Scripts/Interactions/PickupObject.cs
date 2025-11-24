using System.Collections;
using Mission;
using Planet;
using Players.Common;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Interactions
{
    public enum PickupType
    {
        Apple,
        Mushroom,
    }

    public class PickupObject : InteractableObject
    {
        [SerializeField] private PickupType type;
        [SerializeField] private GameObject models;
        [SerializeField] private bool isRecycle = false;

        private Rigidbody rBody;
        private PlanetBody pBody;

        private readonly WaitForSeconds wait = new(0.3f);
        private readonly WaitForSeconds respawnWait = new(1f);

        private readonly NetworkVariable<bool> isVisible = new(true);

        public override void Awake()
        {
            base.Awake();

            TryGetComponent(out rBody);
            TryGetComponent(out pBody);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            isVisible.OnValueChanged += SetVisible;

            if (!IsOwner) return;

            pBody?.Initialize(rBody);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            isVisible.OnValueChanged -= SetVisible;
        }

        protected override IEnumerator InteractCo()
        {
            isBusy.Value = true;

            yield return wait;

            CallbackRpc(RpcTarget.Single(lastInteractorId, RpcTargetUse.Temp));

            if (isRecycle)
            {
                StartCoroutine(Recycle());
            }
            else
            {
                NetworkObject.DeferDespawn(2);
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void CallbackRpc(RpcParams param = default)
        {
            MissionNotifier.Instance.NotifyPickup(type);
        }

        private void SetVisible(bool value, bool newValue)
        {
            models.SetActive(newValue);
        }

        private IEnumerator Recycle()
        {
            isVisible.Value = false;

            yield return respawnWait;

            var pos = PlanetGravity.Instance.GetSurfacePoint(out var surfaceNormal);

            var rotationOnSurface = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
            var rot = rotationOnSurface * Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            TeleportRpc(pos, rot);

            yield return respawnWait;

            isVisible.Value = true;

            isBusy.Value = false;
        }

        [Rpc(SendTo.Everyone)]
        private void TeleportRpc(Vector3 pos, Quaternion rot)
        {
            transform.SetPositionAndRotation(pos, rot);
        }
    }
}