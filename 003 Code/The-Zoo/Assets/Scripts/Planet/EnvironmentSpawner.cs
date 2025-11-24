using System.Collections.Generic;
using Scriptable;
using Unity.Netcode;
using UnityEngine;

namespace Planet
{
    public class EnvironmentSpawner : NetworkBehaviour
    {
        [SerializeField] private List<EnvironmentData> envDataList;
        [SerializeField] private Transform propsParent;

        [Rpc(SendTo.Everyone)]
        internal void SpawnRpc(int seed)
        {
            Random.InitState(seed);

            foreach (var data in envDataList)
            {
                var prefab = data.obj;

                for (var i = 0; i < data.count; i++)
                {
                    var pos = PlanetGravity.Instance.GetSurfacePoint(out var normal);
                    var rot = Quaternion.FromToRotation(Vector3.up, normal) *
                              Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                    var obj = Instantiate(prefab, pos, rot);
                    obj.transform.SetParent(propsParent);
                }
            }

        }
    }
}