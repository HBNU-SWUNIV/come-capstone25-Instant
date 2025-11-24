using System.Collections;
using Planet;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Players.Common
{
    public class HittableBody : NetworkBehaviour
    {
        public NetworkVariable<int> healthPoint = new (3);
        public ulong lastAttackerId;

        private Rigidbody rBody;

        public override void OnNetworkSpawn()
        {
            rBody = GetComponent<Rigidbody>();
        }

        public void Initialize(int point)
        {
            healthPoint.Value = point;
        }

        public void Damaged(int damage, ulong attackerId)
        {
            lastAttackerId = attackerId;
            healthPoint.Value -= damage;
        }

        public void Healed(int heal)
        {
            healthPoint.Value += heal;
        }

        [Rpc(SendTo.Authority)]
        public void KnockBackRpc(Vector3 attackerPos, float power, float upPower)
        {
            rBody.linearVelocity = Vector3.zero;

            var dir = (transform.position - attackerPos).normalized;

            var up = -PlanetGravity.Instance.GetGravityDirection(transform.position);
            var finalDir = (dir + up * upPower).normalized;

            rBody.AddForce(finalDir * power, ForceMode.Impulse);
        }

        [Rpc(SendTo.Authority)]
        public void AirborneRpc(Vector3 attackerPos, float power)
        {
            rBody.linearVelocity = Vector3.zero;

            var dir  = (attackerPos - transform.position).normalized;

            var up = -PlanetGravity.Instance.GetGravityDirection(transform.position);
            var finalDir = (dir + up).normalized;

            rBody.AddForce(finalDir * power, ForceMode.VelocityChange);
        }
    }
}