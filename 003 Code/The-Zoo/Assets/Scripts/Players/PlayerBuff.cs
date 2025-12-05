using System;
using System.Collections.Generic;
using System.Linq;
using Players.Roles;
using UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Players
{
    public enum BuffType
    {
        Speed,
        Size,
        Attack,
        End
    }

    public struct Buff : IEquatable<Buff>
    {
        public BuffType type;
        public float value;
        public bool isPositive;

        public bool Equals(Buff other)
        {
            return type == other.type && value.Equals(other.value) && isPositive == other.isPositive;
        }

        public override bool Equals(object obj)
        {
            return obj is Buff other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)type, value, isPositive);
        }
    }

    public class PlayerBuff : MonoBehaviour
    {
        public event Action<List<(BuffType, bool, int)>> OnBuffChanged;
        private const float BuffAmount = 0.1f;

        public float totalMoveBuff = 1f;
        public float totalScaleBuff = 1f;

        private readonly List<Buff> buffs = new();

        internal void Initialize()
        {
            totalMoveBuff = 1f;
            totalScaleBuff = 1f;

            buffs.Clear();
        }

        internal void CreateBuff(BuffType type, bool isPositive)
        {
            var value = type switch
            {
                BuffType.Speed => isPositive ? BuffAmount : -BuffAmount,
                BuffType.Size => isPositive ? -BuffAmount : BuffAmount,
                _ => 0
            };

            buffs.Add(new Buff
            {
                type = type,
                value = value,
                isPositive = isPositive
            });

            ApplyBuff();
        }

        internal bool RemoveBuff()
        {
            // 🔹 1. 부정적인 버프만 추출
            var negativeBuffs = buffs.FindAll(b => !b.isPositive);

            // 🔹 2. 제거할 디버프가 없으면 종료
            if (negativeBuffs.Count == 0)
                return false;

            // 🔹 3. 랜덤하게 하나 선택
            var removeTarget = negativeBuffs[Random.Range(0, negativeBuffs.Count)];

            // 🔹 4. 실제 리스트에서 제거
            buffs.Remove(removeTarget);

            // 🔹 5. 변경 반영
            ApplyBuff();

            return true;
        }

        private void ApplyBuff()
        {
            var move = 1f;
            var scale = 1f;

            foreach (var buff in buffs)
            {
                switch (buff.type)
                {
                    case BuffType.Speed:
                        move += buff.value;
                        break;
                    case BuffType.Size:
                        scale += buff.value;
                        break;
                }
            }

            totalMoveBuff = move;
            totalScaleBuff = scale;

            PlayerLocator.LocalPlayer.UpdateSpeed(totalMoveBuff);
            PlayerLocator.LocalPlayer.UpdateScale(totalScaleBuff);

            var summaries = PlayerLocator.LocalPlayer.buff.GetBuffStacks();

            OnBuffChanged?.Invoke(summaries);
        }

        /// <summary>
        /// BuffType + 방향(긍정/부정)별 스택 카운트 요약
        /// </summary>
        private List<(BuffType type, bool isPositive, int stackCount)> GetBuffStacks()
        {
            return buffs
                .GroupBy(b => new { b.type, b.isPositive })
                .Select(g => (g.Key.type, g.Key.isPositive, g.Count()))
                .ToList();
        }
    }
}