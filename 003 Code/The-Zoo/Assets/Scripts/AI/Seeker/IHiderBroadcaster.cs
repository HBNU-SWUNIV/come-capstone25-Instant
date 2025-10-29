using UnityEngine;

namespace AI.Seeker
{
    public enum HiderActionType
    {
        Jump,
        Spin,
        Attack,
    }

    public interface ISeekerListener
    {
        void OnHiderAction(Transform hider, HiderActionType action);
    }

    public interface IHiderBroadcaster
    {
        void RegisterListener(ISeekerListener listener);
        void UnregisterListener(ISeekerListener listener);
        Transform HiderTransform { get; }
    }
}