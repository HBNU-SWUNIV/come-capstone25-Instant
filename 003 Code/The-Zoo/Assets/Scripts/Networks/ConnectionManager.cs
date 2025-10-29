using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UI.Lobby.GameSetup;
using UI.Lobby.InformationPopup;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using Utils;
using static EventHandler.ConnectionEventHandler;
using static Networks.ConnectionData;

namespace Networks
{
    public class ConnectionManager : MonoBehaviour
    {
        private const int MaxPlayers = 4;

        public static ConnectionManager Instance { get; private set; }

        public ISession CurrentSession { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private async void OnEnable()
        {
            try
            {
                await UnityServices.InitializeAsync();

                NetworkManager.OnDestroying += Destroying;

                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectCallback;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnOnClientDisconnectCallback;
                NetworkManager.Singleton.OnSessionOwnerPromoted += OnSessionOwnerPromoted;
            }
            catch (Exception e)
            {
                MyLogger.Print(this, e.Message);
            }
        }

        private void NotifyUser(string message)
        {
            InformationPopup.instance?.ShowPopup(message);
        }

        private void Destroying(NetworkManager obj)
        {
            NetworkManager.OnDestroying -= Destroying;

            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnOnClientDisconnectCallback;
            NetworkManager.Singleton.OnSessionOwnerPromoted -= OnSessionOwnerPromoted;
        }

        private string GetReadableErrorMessage(SessionException error)
        {
            switch (error.Error)
            {
                case SessionError.None:
                    return "정상적으로 처리되었습니다.";

                case SessionError.Unknown:
                {
                    return error.Message;
                }

                case SessionError.NotAuthorized:
                    return "인증되지 않은 사용자입니다.";

                case SessionError.NotInLobby:
                    return "로비에 참여 중이 아닙니다.";

                case SessionError.LobbyAlreadyExists:
                case SessionError.SessionTypeAlreadyExists:
                    return "이미 동일한 이름의 세션이 존재합니다.";

                case SessionError.SessionNotFound:
                case SessionError.SessionDeleted:
                    return "세션을 찾을 수 없거나 이미 삭제되었습니다.";

                case SessionError.AllocationAlreadyExists:
                    return "이미 할당된 세션이 있습니다.";

                case SessionError.AllocationNotFound:
                    return "세션 할당 정보를 찾을 수 없습니다.";

                case SessionError.Forbidden:
                    return "세션 접근 권한이 없습니다.";

                case SessionError.RateLimitExceeded:
                    return "요청 한도를 초과했습니다. 잠시 후 다시 시도하세요.";

                case SessionError.InvalidParameter:
                    return "요청 매개변수가 잘못되었습니다.";

                case SessionError.InvalidMatchmakerTicket:
                case SessionError.InvalidMatchmakerAssignment:
                case SessionError.InvalidMatchmakerState:
                case SessionError.InvalidMatchmakerResults:
                    return "매치메이킹 정보가 유효하지 않습니다.";

                case SessionError.InvalidNetworkConfig:
                case SessionError.TransportInvalid:
                    return "네트워크 설정이 올바르지 않습니다.";

                case SessionError.InvalidSessionMetadata:
                case SessionError.InvalidSessionIdentifier:
                    return "세션 정보가 유효하지 않습니다.";

                case SessionError.NetworkManagerNotInitialized:
                case SessionError.NetworkManagerStartFailed:
                case SessionError.NetworkSetupFailed:
                    return "네트워크 초기화에 실패했습니다.";

                case SessionError.TransportComponentMissing:
                    return "전송(Transport) 구성 요소가 누락되었습니다.";

                case SessionError.InvalidOperation:
                    return "잘못된 작업 요청입니다.";

                case SessionError.MultiplayServerError:
                    return "서버 내부 오류가 발생했습니다.";

                case SessionError.MatchmakerAssignmentFailed:
                case SessionError.MatchmakerAssignmentTimeout:
                case SessionError.MatchmakerCancelled:
                    return "매치메이커 연결에 실패했습니다.";

                case SessionError.InvalidCreateSessionOptions:
                    return "세션 생성 옵션이 잘못되었습니다.";

                case SessionError.QoSMeasurementFailed:
                    return "네트워크 품질(QoS) 측정에 실패했습니다.";

                case SessionError.AlreadySubscribedToLobby:
                    return "이미 로비에 가입되어 있습니다.";

                default:
                    return $"세션 접속에 실패했습니다. (오류 코드: {error})";
            }
        }

        // <-------------------Connection------------------->
        private async Task HandleConnectionFlowAsync(Func<Task<ISession>> sessionFunc)
        {
            try
            {
                OnSessionConnectStart();

                CurrentSession = await sessionFunc.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ConnectionFlow failed: {e.Message}");
                throw;
            }
        }

        private async Task SignInAsync()
        {
            try
            {
                AuthenticationService.Instance.SignInFailed += SignInFailed;
                AuthenticationService.Instance.SwitchProfile(Util.GetRandomString(8));
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
                throw;
            }
        }

        private void SignInFailed(RequestFailedException e)
        {
            AuthenticationService.Instance.SignInFailed -= SignInFailed;
            Debug.LogWarning($"Sign in via Authentication failed: e.ErrorCode {e.ErrorCode}");
        }

        public async Task ConnectAsync(ConnectionData data)
        {
            try
            {
                switch (data.Type)
                {
                    case ConnectionType.JoinById:
                        await JoinByIdAsync(data.IdOrCode, data.Password);
                        break;
                    case ConnectionType.JoinByCode:
                        await JoinByCodeAsync(data.IdOrCode, data.Password);
                        break;
                    case ConnectionType.Create:
                        await CreateSessionAsync(data.SessionName, data.Password, data.IsPrivate);
                        break;
                    case ConnectionType.Quick:
                        await QuickJoinAsync(data.SessionName);
                        break;
                    default:
                        NotifyUser("잘못된 연결 요청입니다.");
                        break;
                }
            }
            catch (SessionException e)
            {
                Debug.LogWarning($"[Connection] SessionException: {e.Message}");

                NotifyUser(GetReadableErrorMessage(e));

                OnConnectionFailed();
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                NotifyUser(e.Message);

                OnConnectionFailed();
            }
        }

        public async Task DisconnectSessionAsync()
        {
            try
            {
                await CurrentSession.LeaveAsync();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                CurrentSession = null;
            }
        }

        public async Task<IList<ISessionInfo>> QuerySessionsAsync()
        {
            var sessionQueryOptions = new QuerySessionsOptions();
            var results = await MultiplayerService.Instance.QuerySessionsAsync(sessionQueryOptions);
            return results.Sessions;
        }

        private async Task CreateSessionAsync(string sessionName = null, string password = null, bool isPrivate = false)
        {
            try
            {
                var options = new SessionOptionBuilder()
                    .Name(sessionName)
                    .SeekerCount(1)
                    .NpcCount(5)
                    .GameTime(300)
                    .Password(password)
                    .IsPrivate(true)
                    .PlayerProperty(Util.PLAYERNAME, AuthenticationService.Instance.PlayerName)
                    .BuildCreate();

                await HandleConnectionFlowAsync(async () =>
                    await MultiplayerService.Instance.CreateSessionAsync(options));

                if (!isPrivate) PublicSessionAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"CreateSession failed: {e.Message}");
                throw;
            }
        }

        private async Task QuickJoinAsync(string sessionName)
        {
            try
            {
                var sessionId = $"Session_{Util.GetRandomString(5)}";

                var options = new SessionOptionBuilder()
                    .Name(sessionName)
                    .SeekerCount(1)
                    .NpcCount(5)
                    .GameTime(300)
                    .IsPrivate(true)
                    .PlayerProperty(Util.PLAYERNAME, AuthenticationService.Instance.PlayerName)
                    .BuildCreate();

                await HandleConnectionFlowAsync(async () =>
                    await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionId, options));

                PublicSessionAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"QuickJoin failed: {e.Message}");
                throw;
            }
        }

        private async Task JoinByIdAsync(string id, string password = null)
        {
            try
            {
                var options = new SessionOptionBuilder()
                    .Password(password)
                    .PlayerProperty(Util.PLAYERNAME, AuthenticationService.Instance.PlayerName)
                    .BuildJoin();

                await HandleConnectionFlowAsync(async () =>
                    await MultiplayerService.Instance.JoinSessionByIdAsync(id, options));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"JoinById failed: {e.Message}");
                throw;
            }
        }

        private async Task JoinByCodeAsync(string code, string password = null)
        {
            try
            {
                var options = new SessionOptionBuilder()
                    .Password(password)
                    .PlayerProperty(Util.PLAYERNAME, AuthenticationService.Instance.PlayerName)
                    .BuildJoin();

                await HandleConnectionFlowAsync(async () =>
                    await MultiplayerService.Instance.JoinSessionByCodeAsync(code, options));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"JoinById failed: {e.Message}");
                throw;
            }
        }

        // <-------------------Host------------------->

        private async Task WithHostSessionAsync(Func<IHostSession, Task> action)
        {
            if (CurrentSession is not { IsHost: true }) return;

            var host = CurrentSession.AsHost();
            await action.Invoke(host);
        }

        public async Task UpdateSessionAsync(
            GameOptionField<string> sessionName, GameOptionField<string> password,
            GameOptionField<bool> isPrivate, GameOptionField<int> seekerCount,
            GameOptionField<int> npcCount, GameOptionField<int> gameTime)
        {
            try
            {
                await WithHostSessionAsync(async host =>
                {
                    if (sessionName.IsDirty) host.Name = sessionName.Current;

                    if (password.IsDirty)
                    {
                        host.Password = password.Current;
                        host.SetProperty(Util.PASSWORD,
                            new SessionProperty(password.Current,
                                VisibilityPropertyOptions.Private));
                    }

                    if (isPrivate.IsDirty) host.IsPrivate = isPrivate.Current;

                    if (seekerCount.IsDirty)
                        host.SetProperty(Util.SEEKERCOUNT,
                            new SessionProperty(seekerCount.Current.ToString()));

                    if (npcCount.IsDirty)
                        host.SetProperty(Util.NPCCOUNT,
                            new SessionProperty(npcCount.Current.ToString()));

                    if (gameTime.IsDirty)
                        host.SetProperty(Util.GAMETIME,
                            new SessionProperty(gameTime.Current.ToString()));

                    await host.SavePropertiesAsync();
                });
            }
            catch (SessionException e)
            {
                NotifyUser(GetReadableErrorMessage(e));
                throw;
            }
            catch (Exception e)
            {
                NotifyUser(e.Message);
                throw;
            }
        }

        public async void ChangeHostAsync(string newHost)
        {
            try
            {
                await WithHostSessionAsync(async host =>
                {
                    host.Host = newHost;
                    await host.SavePropertiesAsync();
                });
            }
            catch (Exception e)
            {
                print(e.Message);
            }
        }

        public async void KickPlayerAsync(string playerId)
        {
            try
            {
                await WithHostSessionAsync(async host =>
                {
                    await host.RemovePlayerAsync(playerId);
                });
            }
            catch (Exception e)
            {
                print(e.Message);
            }
        }

        public async void LockSessionAsync()
        {
            try
            {
                await WithHostSessionAsync(async host =>
                {
                    host.IsLocked = true;
                    await host.SavePropertiesAsync();
                });
            }
            catch (Exception e)
            {
                print(e.Message);
            }
        }

        public async void UnlockSessionAsync()
        {
            try
            {
                await WithHostSessionAsync(async host =>
                {
                    host.IsLocked = false;
                    await host.SavePropertiesAsync();
                });
            }
            catch (Exception e)
            {
                print(e.Message);
            }
        }

        private async void PublicSessionAsync()
        {
            try
            {
                await WithHostSessionAsync(async host =>
                {
                    host.IsPrivate = false;
                    await host.SavePropertiesAsync();
                });
            }
            catch (Exception e)
            {
                print(e.Message);
            }
        }

        // <-------------------Event------------------->

        private void OnSessionOwnerPromoted(ulong sessionOwnerPromoted)
        {
            if (!NetworkManager.Singleton.LocalClient.IsSessionOwner) return;

            Debug.Log($"Client-{NetworkManager.Singleton.LocalClientId} is the session owner!");
        }

        private void OnClientConnectCallback(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId) return;
            Debug.Log($"Client-{clientId} is connected and can spawn {nameof(NetworkObject)}s.");
        }

        private async void OnOnClientDisconnectCallback(ulong clientId)
        {
            if (NetworkManager.Singleton.LocalClientId != clientId) return;

            print($"Client-{clientId} is disconnected");
            await CurrentSession?.LeaveAsync()!;
        }

        public async Task Login(string playerName)
        {
            try
            {
                if (AuthenticationService.Instance == null) return;

                if (!AuthenticationService.Instance.IsSignedIn) await SignInAsync();

                await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
            }
            catch (Exception e)
            {
                NotifyUser(e.Message);
                OnConnectionFailed();
                throw;
            }
        }
    }
}