using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    [Serializable]
    public struct DictEntry<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField] private List<DictEntry<TKey, TValue>> entries = new();
        private readonly Dictionary<TKey, TValue> dict = new();

        public bool TryGetValue(TKey key, out TValue value) => dict.TryGetValue(key, out value);
        public TValue this[TKey key] { get => dict[key]; set => dict[key] = value; }
        public bool ContainsKey(TKey key) => dict.ContainsKey(key);
        public int Count => dict.Count;
        public Dictionary<TKey, TValue> AsDictionary() => dict; // 필요 시 원본 dict 접근

        // --- 직렬화 훅 ---
        public void OnBeforeSerialize()
        {
            // 인스펙터에서 편집된 entries -> dict로 반영(마지막 값 우선)
            dict.Clear();
            foreach (var e in entries)
                if (e.Key != null) dict[e.Key] = e.Value;
        }

        public void OnAfterDeserialize()
        {
            // 역직렬화 완료 후 entries로부터 dict 재구성
            dict.Clear();
            foreach (var e in entries)
                if (e.Key != null) dict[e.Key] = e.Value;
        }
    }

}