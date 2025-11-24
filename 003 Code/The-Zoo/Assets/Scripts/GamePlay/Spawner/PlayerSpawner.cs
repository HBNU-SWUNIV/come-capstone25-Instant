using System.Linq;
using Players;
using Players.Common;
using Scriptable;
using Unity.Netcode;
using UnityEngine;

namespace GamePlay.Spawner
{
    public class PlayerSpawner : NetworkBehaviour
    {
        private readonly NetworkList<int> spawnedAnimals = new();
        public static PlayerSpawner Instance { get; private set; }

        public void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        protected override void OnNetworkSessionSynchronized()
        {
            base.OnNetworkSessionSynchronized();

            Spawn();
        }

        [Rpc(SendTo.Owner)]
        private void AddRpc(AnimalType type)
        {
            spawnedAnimals.Add((int) type);
        }

        [Rpc(SendTo.Owner)]
        internal void RemoveRpc(AnimalType type)
        {
            spawnedAnimals.Remove((int) type);
        }

        private void Spawn()
        {
            var length = SpawnObjectStore.Instance.GetLength();

            var type = GetRandomAnimalTypeDistrict(length);

            SpawnPlayerObject(type);

            AddRpc(type);
        }

        private AnimalType GetRandomAnimalTypeDistrict(int max)
        {
            var candidates = Enumerable
                .Range(0, max)
                .Where(i => !spawnedAnimals.Contains(i))
                .ToList();

            if (candidates.Count == 0) return 0;

            return (AnimalType)candidates[Random.Range(0, candidates.Count)];
        }

        private void SpawnPlayerObject(AnimalType type)
        {
            var data = SpawnObjectStore.Instance.GetAnimalData(type);
            var prefab = data.playerPrefab;

            var netObj = prefab.InstantiateAndSpawn(
                NetworkManager,
                NetworkManager.LocalClientId,
                isPlayerObject: true);

            netObj.GetComponent<IAnimalType>().Type = type;
            netObj.GetComponent<PlayerEntity>().animalType.Value = type;
        }
    }
}