using System.Collections.Generic;
using Players;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI.InGame
{
    public class BuffUI : MonoBehaviour
    {
        [FormerlySerializedAs("buffItem")] [SerializeField] private BuffItem buffItemPrefab;
        [SerializeField] private Transform buffView;

        private readonly List<BuffItem> spawnedItems = new();

        private void Start()
        {
            PlayerLocator.LocalPlayer.buff.OnBuffChanged += RefreshUI;
        }

        private void OnDestroy()
        {
            PlayerLocator.LocalPlayer.buff.OnBuffChanged -= RefreshUI;
        }

        private void ClearBuffs()
        {
            foreach (var item in spawnedItems)
                Destroy(item.gameObject);
            spawnedItems.Clear();
        }

        private void RefreshUI(List<(BuffType type, bool isPositive, int stackCount)> summaries)
        {
            ClearBuffs();

            foreach (var (type, isPositive, stack) in summaries)
            {
                var item = Instantiate(buffItemPrefab, buffView);
                item.SetBuff(type, isPositive, stack);
                spawnedItems.Add(item);
            }
        }
    }
}