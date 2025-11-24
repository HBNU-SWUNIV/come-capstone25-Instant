using System.Collections;
using System.Collections.Generic;
using Planet;
using Players.Common;
using Scriptable;
using Unity.Netcode;
using UnityEngine;

namespace GamePlay.Spawner
{
    [DefaultExecutionOrder(-100)]
    public class NpcSpawner : NetworkBehaviour
    {
        public static NpcSpawner Instance { get; private set; }

        internal readonly List<(AnimalType type, NetworkObject npc)> spawnedNpc = new();

        public void Awake()
        {
            if (!Instance) Instance = this;
            else Destroy(gameObject);
        }

        [Rpc(SendTo.Authority)]
        public void SpawnBatchRpc(AnimalType[] types, int count)
        {
            for (var i = 0; i < types.Length; i++)
            {
                StartCoroutine(SpawnCo(types[i], count));
            }
        }

        [Rpc(SendTo.Authority)]
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
                var pos = PlanetGravity.Instance.GetSurfacePoint(out var normal);

                var npc = prefab.InstantiateAndSpawn(NetworkManager,
                    position: pos + normal * 0.5f,
                    rotation: Quaternion.identity);

                spawnedNpc.Add((type, npc));

                if(!npc.TryGetComponent<IAnimalType>(out var agent)) continue;
                agent.Type = type;

                yield return null;
            }
        }

        [Rpc(SendTo.Authority)]
        internal void ClearRpc(RpcParams rpcParams = default)
        {
            foreach (var (type, npc) in spawnedNpc)
            {
                if(!npc.IsSpawned) continue;

                npc.Despawn();
            }

            spawnedNpc.Clear();
        }
    }
}