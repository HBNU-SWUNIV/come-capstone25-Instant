using System.Collections;
using System.Collections.Generic;
using Planet;
using Scriptable;
using Unity.Netcode;
using UnityEngine;
using Utils;

namespace GamePlay.Spawner
{
    [DefaultExecutionOrder(-100)]
    public class NpcSpawner : NetworkBehaviour
    {
        public static NpcSpawner Instance { get; private set; }

        private readonly List<NetworkObject> spawnedNpc = new();

        public void Awake()
        {
            if (!Instance) Instance = this;
            else Destroy(gameObject);
        }

        [Rpc(SendTo.Authority, RequireOwnership = false)]
        internal void SpawnRpc(AnimalType type, int count, RpcParams rpcParams = default)
        {
            StartCoroutine(SpawnCo(type, count));
        }

        private IEnumerator SpawnCo(AnimalType type, int count)
        {
            var data = SpawnObjectStore.Instance.GetAnimalData(type);
            var prefab = data.npcPrefab;

            for (var i = 0; i < count; i++)
            {
                var dir = Random.onUnitSphere;
                var pos = PlanetGravity.Instance.GetSurfacePoint(dir, out _);

                var npc = prefab.InstantiateAndSpawn(NetworkManager,
                    position: pos,
                    rotation: Quaternion.identity);

                spawnedNpc.Add(npc);

                yield return null;
            }
        }

        [Rpc(SendTo.Authority, RequireOwnership = false)]
        internal void ClearRpc(RpcParams rpcParams = default)
        {
            foreach (var npc in spawnedNpc)
            {
                if(!npc.IsSpawned) continue;

                npc.Despawn();
            }

            spawnedNpc.Clear();
        }
    }
}