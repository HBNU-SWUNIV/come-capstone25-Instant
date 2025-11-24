using System;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Utils
{
    public static class Util
    {
        public static readonly string PLAYERNAME = "에러가 발생했습니다.";
        public static readonly string PASSWORD = "Password";
        public static readonly string NPCCOUNT = "NpcCount";
        public static readonly string SEEKERCOUNT = "SeekerCount";
        public static readonly string GAMETIME = "GameTime";

        public const string InGameSceneName = "InGame";
        public const string LobbySceneName = "Lobby";
        public const string TitleSceneName = "Title";

        private const string Room = "의 방";
        
        public static string GetRandomString(int length)
        {
            return Guid.NewGuid().ToString("N")[..length];
        }
        
        public static string GetDefaultSessionName(string playerName)
        {
            var sb = new StringBuilder();
            sb.Append(playerName);
            sb.Append(Room);

            return sb.ToString();
        }

        public static string GetPlayerNameWithoutHash(string playerName)
        {
            var index = playerName.IndexOf('#');
            return index > 0 ? playerName[..index] : playerName;
        }
        
        public static Vector3 GetCirclePositions(Vector3 center, int index, float radius, int count)
        {
            var angle = index * Mathf.PI * 2f / count;
            var x = center.x + radius * Mathf.Cos(angle);
            var z = center.z + radius * Mathf.Sin(angle);

            return new Vector3(x, center.y, z);
        }

        public static Vector3 GetRandomPositionInSphere(float radius)
        {
            return Random.onUnitSphere.normalized * radius;
        }
    }
}