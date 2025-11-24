using System;
using Unity.Netcode;
using UnityEngine;

namespace Scriptable
{
    public enum AnimalType
    {
        Cat,
        Dog,
        Dove,
        Gecko,
        Kookaburra,
        Mouse,
        Muskrat,
        Parrot,
        Pigeon,
        Platypus,
        Possum,
        Quokka,
        Rabbit,
        Sparrow,
        TasmanianDevil,
        Tortoise,
        Wombat,
        End
    }

    [CreateAssetMenu(fileName = "AnimalData", menuName = "Game/Animals", order = 0)]
    public class AnimalData : ScriptableObject
    {
        public AnimalType type;
        public NetworkObject playerPrefab;
        public NetworkObject npcPrefab;
    }
}