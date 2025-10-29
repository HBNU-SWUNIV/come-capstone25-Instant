using System;
using System.Threading.Tasks;
using Networks;
using UnityEngine;
using Utils;

namespace UI.Lobby.GameSetup
{
    public class GameSetupController : MonoBehaviour
    {
        public string JoinCode { get; private set; }
        public GameOptionField<string> Password { get; private set; }
        public GameOptionField<string> SessionName { get; private set; }
        public GameOptionField<bool> IsPrivate { get; private set; }
        public GameOptionField<int> SeekerCount { get; private set; }
        public GameOptionField<int> NpcCount { get; private set; }
        public GameOptionField<int> GameTime { get; private set; }

        public void Reset()
        {
            SessionName.Reset();
            Password.Reset();
            IsPrivate.Reset();
            SeekerCount.Reset();
            NpcCount.Reset();
            GameTime.Reset();
        }

        public void Initialize()
        {
            var info = ConnectionManager.Instance.CurrentSession;

            JoinCode = info.Code;

            IsPrivate = new GameOptionField<bool>(info.IsPrivate);
            SessionName = new GameOptionField<string>(info.Name);

            info.Properties.TryGetValue(Util.PASSWORD, out var password);
            Password = password != null
                ? new GameOptionField<string>(password.Value)
                : new GameOptionField<string>(string.Empty);

            info.Properties.TryGetValue(Util.SEEKERCOUNT, out var seekers);
            SeekerCount = seekers != null
                ? new GameOptionField<int>(int.Parse(seekers.Value))
                : new GameOptionField<int>(0);

            info.Properties.TryGetValue(Util.NPCCOUNT, out var npc);
            NpcCount = npc != null
                ? new GameOptionField<int>(int.Parse(npc.Value))
                : new GameOptionField<int>(5);

            info.Properties.TryGetValue(Util.GAMETIME, out var time);
            GameTime = time != null
                ? new GameOptionField<int>(int.Parse(time.Value))
                : new GameOptionField<int>(300);
        }

        public void Apply()
        {
            IsPrivate.Apply();
            Password.Apply();
            SessionName.Apply();
            SeekerCount.Apply();
            NpcCount.Apply();
            GameTime.Apply();
        }

        public async Task Save()
        {
            try
            {
                await ConnectionManager.Instance.UpdateSessionAsync(
                    SessionName, Password, IsPrivate, SeekerCount, NpcCount, GameTime);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
                throw;
            }
        }
    }
}