namespace Networks
{
    public struct ConnectionData
    {
        public enum ConnectionType
        {
            Quick,
            Create,
            JoinById,
            JoinByCode
        }

        public ConnectionType Type { get; private set; }
        public string IdOrCode { get; private set; }
        public string Password { get; private set; }
        public string SessionName { get; private set; }
        public bool IsPrivate { get; private set; }

        public ConnectionData(ConnectionType type, string idOrCode = null, string password = null,
            string sessionName = null, bool isPrivate = false)
        {
            Type = type;
            IdOrCode = idOrCode;
            Password = password;
            SessionName = sessionName;
            IsPrivate = isPrivate;
        }
    }
}