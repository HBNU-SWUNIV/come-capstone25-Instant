using System.Collections.Generic;
using Scriptable;
using UnityEngine;

namespace GamePlay.Spawner
{
    public class SpawnObjectStore : MonoBehaviour
    {
        [Header( "Animals" )]
        [SerializeField] private List<AnimalData> animalDataList;
        [Header( "Interactions" )]
        [SerializeField] private List<InteractionData> interactions;

        public List<InteractionData> Interactions => interactions;

        public static SpawnObjectStore Instance { get; private set; }

        public void Awake()
        {
            if (!Instance) Instance = this;
            else Destroy(gameObject);
        }

        public AnimalData GetAnimalData(AnimalType type)
        {
            var data = animalDataList.Find(d => d.type == type);
            
            return data;
        }

        public int GetLength()
        {
            return animalDataList.Count;
        }
    }
}