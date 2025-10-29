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

        private readonly WaitForSeconds waitFirstDelay = new(20f);
        private readonly WaitForSeconds missionInterval = new(60f);

        private MissionExecutor executor;
        private MissionData currentMission;

        public void Awake()
        {
            executor = GetComponent<MissionExecutor>();
        }

        public void StartMission()
        {
            SetVisibleRpc(false);

            MissionLoopRpc();
        }

        [Rpc(SendTo.Authority, RequireOwnership = false)]
        private void MissionLoopRpc()
        {
            StartCoroutine(MissionCo());
        }

        private IEnumerator MissionCo()
        {
            SetSeekerMissionRpc();

            yield return waitFirstDelay;

            while (PlayManager.Instance.gameBoot.Value)
            {
                var index = Random.Range(0, missions.Count);
                currentMission = missions[index];

                SetVisibleRpc(true);

                AssignMissionRpc((int) currentMission.type, currentMission.description, currentMission.targetValue);

                yield return missionInterval;
            }
        }

        [Rpc(SendTo.Everyone)]
        private void SetVisibleRpc(bool show)
        {
            executor.SetVisible(show);
        }

        [Rpc(SendTo.Everyone)]
        private void SetSeekerMissionRpc()
        {
            var seeker = PlayerLocator.LocalPlayer.entity.role.Value == Role.Seeker;

            executor.SetSeekerVisible(seeker);
        }

        [Rpc(SendTo.Everyone)]
        private void AssignMissionRpc(int type, string desc, int target)
        {
            print($"[MissionManager] Start Assign mission: {type}");

            var localPlayer = PlayerLocator.LocalPlayer.entity;
            if (localPlayer.role.Value != Role.Hider)
            {
                print("This is not hider, mission is not assigned.");
                executor.SetVisible(false);
                return;
            }

            print("This is hider, mission is assigned.");

            executor.SetMission((MissionType) type, desc, target);
        }
    }
}