using System.Collections.Generic;
using Unity.Services.Multiplayer;
using Utils;

namespace Networks
{
    public class SessionOptionBuilder
    {
        private readonly Dictionary<string, PlayerProperty> playerProperties = new();
        private readonly Dictionary<string, SessionProperty> sessionProperties = new();

        private bool isPrivate;

        private string name;
        private string password;
        private int seekerCount;
        private int npcCount;
        private int gameTime;

        public SessionOptionBuilder Name(string value)
        {
            name = value;
            return this;
        }

        public SessionOptionBuilder Password(string value = null)
        {
            password = value;

            var prop = new SessionProperty(value, VisibilityPropertyOptions.Private);

            if (!sessionProperties.TryAdd(Util.PASSWORD, prop)) sessionProperties[Util.PASSWORD] = prop;

            return this;
        }

        public SessionOptionBuilder SeekerCount(int value)
        {
            seekerCount = value;

            var prop = new SessionProperty(value.ToString());

            if (!sessionProperties.TryAdd(Util.SEEKERCOUNT, prop)) sessionProperties[Util.SEEKERCOUNT] = prop;

            return this;
        }

        public SessionOptionBuilder NpcCount(int value)
        {
            npcCount = value;

            var prop = new SessionProperty(value.ToString());

            if (!sessionProperties.TryAdd(Util.NPCCOUNT, prop)) sessionProperties[Util.NPCCOUNT] = prop;

            return this;
        }

        public SessionOptionBuilder GameTime(int value)
        {
            gameTime = value;

            var prop = new SessionProperty(value.ToString());

            if (!sessionProperties.TryAdd(Util.GAMETIME, prop)) sessionProperties[Util.GAMETIME] = prop;

            return this;
        }

        public SessionOptionBuilder IsPrivate(bool value = false)
        {
            isPrivate = value;
            return this;
        }

        public SessionOptionBuilder PlayerProperty(string key, string value)
        {
            var prop = new PlayerProperty(value, VisibilityPropertyOptions.Member);

            playerProperties.Add(key, prop);

            return this;
        }

        public SessionOptions BuildCreate()
        {
            return new SessionOptions
            {
                Name = name,
                Password = password,
                MaxPlayers = 4,
                IsPrivate = isPrivate,
                PlayerProperties = playerProperties,
                SessionProperties = sessionProperties
            }.WithDistributedAuthorityNetwork();
        }

        public JoinSessionOptions BuildJoin()
        {
            return new JoinSessionOptions
            {
                Password = password,
                PlayerProperties = playerProperties
            };
        }
    }
}