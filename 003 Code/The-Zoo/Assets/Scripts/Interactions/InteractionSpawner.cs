using System.Collections.Generic;
using System.Linq;
using GamePlay.Spawner;
using Planet;
using Unity.Netcode;
using UnityEngine;

namespace Interactions
{
    public class InteractionSpawner : NetworkBehaviour
    {
        private readonly List<NetworkObject> spawnedObjects = new();

        [Rpc(SendTo.Authority, RequireOwnership = false)]
        internal void SpawnRpc(RpcParams rpcParams = default)
        {
            var interactions = SpawnObjectStore.Instance.Interactions;

            foreach (var data in interactions)
            {
                var prefab = data.obj;

                for (var i = 0; i < data.count; i++)
                {
                    var dir = Random.onUnitSphere;
                    var spawnPos = PlanetGravity.Instance.GetSurfacePoint(dir, out var surfaceNormal);

                    var rotationOnSurface = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
                    var rotation = rotationOnSurface * Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                    // 네트워크 스폰
                    var interaction = prefab.InstantiateAndSpawn(NetworkManager,
                        position: spawnPos,
                        rotation: rotation);

                    spawnedObjects.Add(interaction);
                }
            }
        }

        [Rpc(SendTo.Authority, RequireOwnership = false)]
        internal void ClearRpc(RpcParams rpcParams = default)
        {
            foreach (var obj in spawnedObjects.Where(obj => obj && obj.IsSpawned))
            {
                obj.Despawn();
            }

            spawnedObjects.Clear();
        }
    }
}