using Unity.Netcode;

namespace Players
{
    public static class PlayerLocator
    {
        public static PlayerController LocalPlayer { get; private set; }

        public static void Set(PlayerController player) => LocalPlayer = player;
        public static void Clear() => LocalPlayer = null;
    }
}