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
        [SerializeField] private Button returnLobbyButton;
        [SerializeField] private Transform parentTr;
        [SerializeField] private TextMeshProUGUI titleText;

        [SerializeField] private SerializableDictionary<Role, ResultItem> resultItems = new();

        private CanvasGroup canvasGroup;
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            returnLobbyButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            returnLobbyButton.onClick.AddListener(OnReturnLobbyButtonClicked);

            SetVisible(false);
        }

        private void OnDestroy()
        {
            returnLobbyButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);
            returnLobbyButton.onClick.RemoveListener(OnReturnLobbyButtonClicked);
        }

        internal void SetVisible(bool show)
        {
            canvasGroup.alpha = show ? 1 : 0;
            canvasGroup.interactable = show;
            canvasGroup.blocksRaycasts = show;
        }

        internal void SetButtonActive(bool isHost)
        {
            returnLobbyButton.interactable = isHost;
        }

        private void Clear()
        {
            foreach (Transform child in parentTr) Destroy(child.gameObject);
        }

        public void MakeResults(Role winner, GameResultDto[] results)
        {
            Clear();

            titleText.text = winner != Role.Fighter ? $"{winner} Win !" : "RESULT";

            var sortedPlayers = results
                .OrderBy(p => p.role == Role.Observer) // Observer는 뒤로
                .ThenByDescending(p => p.role == winner)         // 승자 역할 먼저
                .ToList();

            foreach (var data in sortedPlayers)
            {
                var role = data.role;

                if (!resultItems.TryGetValue(role, out var prefab))
                    continue;

                var item = Instantiate(prefab, parentTr);
                item.SetPlayerName(data.name.ToString());
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