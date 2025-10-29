using UnityEngine;

namespace Scriptable
{
    [CreateAssetMenu(fileName = "SfxData", menuName = "Audio/Sound Effect Data")]
    public class SfxData : ScriptableObject
    {
        public AudioClip clip;
        [Range(0f,1f)] public float volume = 1f;
        public float pitch = 1f;
    }
}