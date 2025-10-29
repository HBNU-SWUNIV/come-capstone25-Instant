using DG.Tweening;
using Networks;
using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

namespace UI.Lobby.PlayerList
{
    public class PlayerListItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject hostIcon;
        [SerializeField] private GameObject readyIcon;

        [SerializeField] private Image background;
        [SerializeField] private Image highlightBackground;

        [SerializeField] private TMP_Text playerNameText;

        [SerializeField] private GameObject actionButtons;
        [SerializeField] private Button promoteHostButton;
        [SerializeField] private Button kickButton;

        private string playerId;
        private bool isHost;

        private void OnEnable()
        {
            actionButtons.SetActive(false);

            promoteHostButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            kickButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);

            promoteHostButton.onClick.AddListener(OnPromoteHostButtonClick);
            kickButton.onClick.AddListener(OnKickButtonClick);

            isHost = false;

            background.gameObject.SetActive(true);
            highlightBackground.gameObject.SetActive(false);

            hostIcon.SetActive(false);

            readyIcon.SetActive(false);
        }

        private void OnDisable()
        {
            promoteHostButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);
            kickButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);

            promoteHostButton.onClick.RemoveListener(OnPromoteHostButtonClick);
            kickButton.onClick.RemoveListener(OnKickButtonClick);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!ConnectionManager.Instance.CurrentSession.IsHost) return;

            if (isHost) return;

            actionButtons.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!ConnectionManager.Instance.CurrentSession.IsHost) return;

            actionButtons.SetActive(false);
        }

        public void Create(IReadOnlyPlayer player)
        {
            playerId = player.Id;

            player.Properties.TryGetValue(Util.PLAYERNAME, out var prop);

            var playerName = prop == null ? "UNKNOWN" : prop.Value;

            playerNameText.SetText(playerName);
        }

        public void Host(bool value)
        {
            hostIcon.SetActive(value);

            isHost = value;

            actionButtons.SetActive(false);
        }

        public void Ready(bool value)
        {
            readyIcon.SetActive(value);
        }

        public void Highlight()
        {
            background.gameObject.SetActive(false);
            highlightBackground.gameObject.SetActive(true);
        }

        private void OnPromoteHostButtonClick()
        {
            ConnectionManager.Instance.ChangeHostAsync(playerId);
        }

        private void OnKickButtonClick()
        {
            ConnectionManager.Instance.KickPlayerAsync(playerId);
        }
    }
}