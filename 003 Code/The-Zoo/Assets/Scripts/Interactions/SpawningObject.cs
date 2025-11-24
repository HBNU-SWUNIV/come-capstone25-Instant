using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Interactions
{
    public class SpawningObject : InteractableObject
    {
        [SerializeField] private NetworkObject spawnObject;
        [SerializeField] private BoxCollider[] spawnPoints;

        private readonly List<NetworkObject> spawnedObjects = new();
        private readonly WaitForSeconds wait = new(1f);

        private int maxSpawnCount = 5;

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;

            foreach (var obj in spawnedObjects.Where(obj => obj.IsSpawned))
            {
                obj.Despawn();
            }

            spawnedObjects.Clear();

            base.OnNetworkDespawn();
        }

        internal bool CanSpawn()
        {
            return spawnedObjects.Count < maxSpawnCount;
        }

        protected override IEnumerator InteractCo()
        {
            if (!CanSpawn()) yield break;

            isBusy.Value = true;

            SpawnRpc();

            yield return wait;

            isBusy.Value = false;
        }

        [Rpc(SendTo.Authority)]
        private void SpawnRpc(RpcParams rpcParams = default)
        {
            var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            var min = spawnPoint.bounds.min;
            var max = spawnPoint.bounds.max;
            var spawnPos = new Vector3(
                Random.Range(min.x, max.x),
                Random.Range(min.y, max.y),
                Random.Range(min.z, max.z)
            );

            var no = spawnObject.InstantiateAndSpawn(NetworkManager,
                position: spawnPos,
                rotation: Quaternion.identity);

            spawnedObjects.Add(no);

            if (!no.TryGetComponent<InteractableObject>(out var interactable)) return;

            interactable.InteractionCompleted += OnInteractionCompleted;
        }

        private void OnInteractionCompleted(InteractableObject obj)
        {
            if(spawnedObjects.Contains(obj.NetworkObject))
                spawnedObjects.Remove(obj.NetworkObject);
        }
    }
}