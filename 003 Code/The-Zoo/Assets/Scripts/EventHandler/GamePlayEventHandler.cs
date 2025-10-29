using System;

namespace EventHandler
{
    internal static class GamePlayEventHandler
    {
        internal static event Action<string, bool> PlayerReady;
        internal static event Action PlayerLogin;
        internal static event Action<bool, bool> CheckInteractable;
        internal static event Action<string> UIChanged;

        internal static void OnPlayerReady(string id, bool value)
        {
            PlayerReady?.Invoke(id, value);
        }

        internal static void OnPlayerLogin()
        {
            PlayerLogin?.Invoke();
        }

        internal static void OnCheckInteractable(bool value, bool enable)
        {
            CheckInteractable?.Invoke(value, enable);
        }
        internal static void OnUIChanged(string name)
        {
            UIChanged?.Invoke(name);
        }
    }
}