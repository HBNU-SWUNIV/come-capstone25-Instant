using System.Collections;
using System.Collections.Generic;
using Planet;
using Unity.Netcode;
using UnityEngine;

namespace GamePlay
{
    public class TornadoManager : NetworkBehaviour
    {
        [SerializeField] private NetworkObject tornadoPrefab;

        private readonly List<NetworkObject> spawned = new(4);

        public void SpawnOnceServer()
        {
            if (!IsSessionOwner) return;
            if (!PlayManager.Instance.gameLoop.Value) return;

            SpawnTornadoInternal();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (!IsSessionOwner) return;

            ClearRpc();
        }

        private void SpawnTornadoInternal()
        {
            PlayManager.Instance.NotifyRpc("어디선가 강풍이 불어옵니다 !!");

            var pos = PlanetGravity.Instance.GetSurfacePoint(out var normal);
            var rot = Quaternion.FromToRotation(Vector3.up, normal) *
                      Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            var tornado = tornadoPrefab.InstantiateAndSpawn(
                NetworkManager,
                position: pos,
                rotation: rot
            );

            spawned.Add(tornado);
        }

        [Rpc(SendTo.Authority)]
        private void ClearRpc(RpcParams rpcParams = default)
        {
            foreach (var npc in spawned)
            {
                if(!npc.IsSpawned) continue;

                npc.Despawn();
            }

            spawned.Clear();
        }
    }
}