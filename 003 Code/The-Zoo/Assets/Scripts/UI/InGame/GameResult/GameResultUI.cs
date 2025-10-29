using System.Collections;
using System.Linq;
using Players.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace UI.InGame.GameResult
{

    public class GameResultUI : MonoBehaviour
    {
        private const string SeekerWin = "Seeker Win !";
        private const string HiderWin = "Hider Win !";
        [SerializeField] private Button returnLobbyButton;
        [SerializeField] private Transform parentObj;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private SerializableDictionary<Role, ResultItem> resultItems = new();
        [SerializeField] private SerializableDictionary<bool, Role[]> showRoles = new();

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void Start()
        {
            returnLobbyButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            returnLobbyButton.onClick.AddListener(OnReturnLobbyButtonClicked);
        }

        private void OnDestroy()
        {
            returnLobbyButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);
            returnLobbyButton.onClick.RemoveListener(OnReturnLobbyButtonClicked);
        }

        internal void SetButtonActive(bool isHost)
        {
            returnLobbyButton.interactable = isHost;
        }

        private void ClearResults()
        {
            for (var i = parentObj.childCount - 1; i >= 0; i--)
                Destroy(parentObj.GetChild(i).gameObject);
        }

        public void OnGameResult(bool isSeekerWin)
        {
            ClearResults();

            titleText.text = isSeekerWin ? SeekerWin : HiderWin;

            var sortedPlayers = GameManager.Instance.playerDict
                .OrderBy(kv => kv.Value.role == Role.Observer ? 1 : 0)
                .ToList();

            foreach (var kv in sortedPlayers)
            {
                var role = kv.Value.role;

                var matched = showRoles[isSeekerWin].Any(t => role == t);
                if (!matched) continue;

                if (!resultItems.TryGetValue(role, out var prefab) || !prefab)
                    continue;

                var item = Instantiate(prefab, parentObj);
                item.SetPlayerName(kv.Value.name);
            }
        }

        private void OnReturnLobbyButtonClicked()
        {
            StartCoroutine(DelayBeforeEnd());
        }

        private IEnumerator DelayBeforeEnd()
        {
            yield return new WaitForSecondsRealtime(0.1f);

            GameManager.Instance.GameEnd();
        }
    }
}