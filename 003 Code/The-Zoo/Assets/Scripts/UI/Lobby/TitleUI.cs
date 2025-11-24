using System;
using System.ComponentModel;
using EventHandler;
using Networks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace UI.Lobby
{
    public class TitleUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private Button enterButton;

        private void OnEnable()
        {
            playerNameInput.onSelect.AddListener((str) => AudioManager.Instance.PlayUISfx());
            enterButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            enterButton.onClick.AddListener(OnEnterButtonClick);
            GamePlayEventHandler.OnUIChanged(Util.TitleSceneName);
        }

        private void OnDisable()
        {
            playerNameInput.onSelect.RemoveListener((str) => AudioManager.Instance.PlayUISfx());
            enterButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);
            enterButton.onClick.RemoveListener(OnEnterButtonClick);
        }

        private async void OnEnterButtonClick()
        {
            try
            {
                if (string.IsNullOrEmpty(playerNameInput.text)) throw new WarningException("플레이어의 이름을 입력해주세요");

                GamePlayEventHandler.OnPlayerLogin();

                var playerName = playerNameInput.text;

                await ConnectionManager.Instance.Login(playerName);
            }
            catch (Exception e)
            {
                InformationPopup.InformationPopup.instance.ShowPopup(e.Message);
                Debug.LogWarning(e);
            }
        }
    }
}