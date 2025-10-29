using Unity.Netcode;

namespace Players.Common
{
    public class HittableBody : NetworkBehaviour
    {
        public NetworkVariable<int> healthPoint = new (3);
        public ulong lastAttackerId;

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
    }
}