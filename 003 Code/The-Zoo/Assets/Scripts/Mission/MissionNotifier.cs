using System;
using Interactions;

namespace Mission
{
    public class MissionNotifier
    {
        public static MissionNotifier Instance { get; } = new();

        public event Action<PickupType> OnPickup;
        public event Action<bool> OnStayRock;

        public void NotifyPickup(PickupType type) => OnPickup?.Invoke(type);
        public void NotifyStayRock(bool isStay) => OnStayRock?.Invoke(isStay);
    }
}