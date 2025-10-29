using System.Linq;
using Networks;
using Players;
using Players.Common;
using Unity.Netcode;
using UnityEngine;
using Utils;

namespace GamePlay
{
    public class RoleManager : NetworkBehaviour
    {
        public int seekerCount = 1;

        internal void AssignRole(int seed)
        {
            var count = ConnectionManager.Instance.CurrentSession.Properties[Util.SEEKERCOUNT].Value;
            seekerCount = int.Parse(count);

            Random.InitState(seed);

            var clients = NetworkManager.Singleton.ConnectedClientsIds
                .OrderBy(_ => Random.value)
                .ToList();

            for (var i = 0; i < seekerCount; i++)
            {
                var id = clients[i];
                SetRoleRpc(Role.Seeker, RpcTarget.Single(id, RpcTargetUse.Temp));
            }
            for (var i = seekerCount; i < clients.Count; i++)
            {
                var id = clients[i];
                SetRoleRpc(Role.Hider, RpcTarget.Single(id, RpcTargetUse.Temp));
            }
        }

        internal void UnassignRole()
        {
            for (var i = 0; i < NetworkManager.Singleton.ConnectedClientsIds.Count; i++)
            {
                var id = NetworkManager.Singleton.ConnectedClientsIds[i];
                SetRoleRpc(Role.None, RpcTarget.Single(id, RpcTargetUse.Temp));;
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void SetRoleRpc(Role role, RpcParams rpcParams)
        {
            print(PlayerLocator.LocalPlayer.name);

            PlayerLocator.LocalPlayer.entity.role.Value = role;
        }
    }
}