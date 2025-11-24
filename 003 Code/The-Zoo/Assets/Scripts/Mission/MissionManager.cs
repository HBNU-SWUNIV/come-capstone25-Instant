using System.Collections;
using System.Collections.Generic;
using GamePlay;
using Players;
using Players.Common;
using Scriptable;
using Unity.Netcode;
using UnityEngine;

namespace Mission
{
    public class MissionManager : NetworkBehaviour
    {
        [SerializeField] private List<MissionData> missions = new();

        private MissionExecutor executor;

        public void Awake()
        {
            executor = GetComponent<MissionExecutor>();
        }

        public void InitializeForHideSeek()
        {
            if (!IsSessionOwner) return;

            SetVisibleRpc(false);
            SetSeekerMissionRpc();
        }

        public void ExecuteRandomMissionServer()
        {
            if (!IsSessionOwner) return;
            if (missions == null || missions.Count == 0) return;
            if (!PlayManager.Instance.gameLoop.Value) return;

            var index = Random.Range(0, missions.Count);
            var mission = missions[index];

            AssignMissionRpc((int)mission.type, mission.description, mission.targetValue);
        }

        [Rpc(SendTo.Everyone)]
        public void LastStandModeRpc()
        {
            executor.SetVisible(false);
            executor.SetBaseMission(true, "최후의 1인이 되어라 !");
        }

        [Rpc(SendTo.Everyone)]
        private void SetVisibleRpc(bool show)
        {
            executor.SetVisible(show);
        }

        [Rpc(SendTo.Everyone)]
        private void SetSeekerMissionRpc()
        {
            var isSeeker = PlayerLocator.LocalPlayer.entity.role.Value == Role.Seeker;
            executor.SetBaseMission(isSeeker, "Hider를 찾아라 !");
        }

        [Rpc(SendTo.Everyone)]
        private void AssignMissionRpc(int type, string desc, int target)
        {
            var localPlayer = PlayerLocator.LocalPlayer.entity;
            if (localPlayer.role.Value != Role.Hider)
            {
                executor.SetVisible(false);
                return;
            }

            executor.SetVisible(true);
            executor.SetMission((MissionType)type, desc, target);
        }
    }
}