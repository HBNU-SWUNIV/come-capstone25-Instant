using System;
using System.Collections.Generic;
using DG.Tweening;
using Networks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.Lobby.GameSetup
{
    public class GameSetupView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private List<RectTransform> contents = new();

        [Header("IsPrivate")]
        [SerializeField] private Toggle privateToggle;

        [Header("Code")]
        [SerializeField] private Button codeCopyButton;
        [SerializeField] private TMP_Text codeCopyText;

        [Header("Session Name")] 
        [SerializeField] private TMP_InputField sessionNameInput;
        [SerializeField] private TMP_Text sessionNamePlaceholder;

        [Header("Password")] 
        [SerializeField] private Sprite visibleOn;
        [SerializeField] private Sprite visibleOff;
        [SerializeField] private Image targetImage;
        [SerializeField] private Toggle passwordVisible;
        [SerializeField] private TMP_InputField passwordInput;

        [Header("Dropdowns")]
        [SerializeField] private TMP_Dropdown seekerCountDropdown;
        [SerializeField] private TMP_Dropdown npcCountDropdown;
        [SerializeField] private TMP_Dropdown gameTimeDropdown;

        [Header("Buttons")] [SerializeField] private Button applyButton;
        [SerializeField] private Button cancelButton;

        private const float Duration = 0.3f;

        private GameSetupController controller;

        private Sequence closeSequence;
        private Sequence openSequence;

        private Vector2 closeSize;
        private Vector2 openSize;
        private RectTransform rectTransform;

        private bool isOpen;
        private float stepDuration;

        private void Start()
        {
            controller = GetComponent<GameSetupController>();
            rectTransform = GetComponent<RectTransform>();

            openSize = rectTransform.sizeDelta;
            closeSize = new Vector2(openSize.x, openSize.y * 0.12f);

            rectTransform.sizeDelta = closeSize;

            foreach (var child in contents)
            {
                child.localScale = Vector3.zero;
                child.gameObject.SetActive(false);

                var cg = child.GetComponent<CanvasGroup>();
                if (!cg) child.gameObject.AddComponent<CanvasGroup>().alpha = 0;
                else cg.alpha = 0;
            }

            stepDuration = Duration / contents.Count;

            openSequence = DOTween.Sequence().Pause().SetAutoKill(false).SetRecyclable(true);
            closeSequence = DOTween.Sequence().Pause().SetAutoKill(false).SetRecyclable(true);

            SetupOpenSequence();
            SetupCloseSequence();

            Register();
            
            controller.Initialize();
            
            Initialize();
        }

        private void OnDestroy()
        {
            closeSequence.Kill();
            openSequence.Kill();

            Unregister();
        }

        private void Initialize()
        {
            codeCopyText.text = controller.JoinCode;
            
            sessionNameInput.text = string.Empty;
            sessionNamePlaceholder.text = controller.SessionName.Original;

            privateToggle.isOn = controller.IsPrivate.Original;
            passwordInput.text = controller.Password.Original;

            seekerCountDropdown.ClearOptions();
            List<string> options = new();
            for (var i = 1; i < ConnectionManager.Instance.CurrentSession.PlayerCount; i++)
            {
                options.Add(i.ToString());
            }
            seekerCountDropdown.AddOptions(options);

            seekerCountDropdown.value = controller.SeekerCount.Original - 1;

            npcCountDropdown.value = controller.NpcCount.Original - 3;

            gameTimeDropdown.value = controller.GameTime.Original / 60 - 5;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            AudioManager.Instance.PlayUISfx();

            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (isOpen || openSequence.IsPlaying()) return;

            Clear();

            openSequence.Restart();
        }

        private void Register()
        {
            privateToggle.onValueChanged.AddListener(OnPrivateToggled);
            sessionNameInput.onValueChanged.AddListener(OnSessionNameChanged);
            passwordInput.onValueChanged.AddListener(OnPasswordChanged);
            seekerCountDropdown.onValueChanged.AddListener(OnSeekerCountChanged);
            npcCountDropdown.onValueChanged.AddListener(OnNpcCountChanged);
            gameTimeDropdown.onValueChanged.AddListener(OnGameTimeChanged);

            passwordVisible.onValueChanged.AddListener((_) => AudioManager.Instance.PlayUISfx());
            codeCopyButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            applyButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);
            cancelButton.onClick.AddListener(AudioManager.Instance.PlayUISfx);

            passwordVisible.onValueChanged.AddListener(OnPasswordVisibleChanged);
            codeCopyButton.onClick.AddListener(OnCopyCodeButtonClick);
            applyButton.onClick.AddListener(OnApplyButtonClick);
            cancelButton.onClick.AddListener(OnCancelButtonClick);
        }

        private void Unregister()
        {
            privateToggle.onValueChanged.RemoveListener(OnPrivateToggled);
            sessionNameInput.onValueChanged.RemoveListener(OnSessionNameChanged);
            passwordInput.onValueChanged.RemoveListener(OnPasswordChanged);
            seekerCountDropdown.onValueChanged.RemoveListener(OnSeekerCountChanged);
            npcCountDropdown.onValueChanged.RemoveListener(OnNpcCountChanged);
            gameTimeDropdown.onValueChanged.RemoveListener(OnGameTimeChanged);

            passwordVisible.onValueChanged.RemoveListener((_) => AudioManager.Instance.PlayUISfx());
            codeCopyButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);
            applyButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);
            cancelButton.onClick.RemoveListener(AudioManager.Instance.PlayUISfx);

            passwordVisible.onValueChanged.RemoveListener(OnPasswordVisibleChanged);
            codeCopyButton.onClick.RemoveListener(OnCopyCodeButtonClick);
            applyButton.onClick.RemoveListener(OnApplyButtonClick);
            cancelButton.onClick.RemoveListener(OnCancelButtonClick);
        }


        private void TrackChange<T>(T value, GameOptionField<T> optionField)
        {
            optionField.Current = value;
            
            applyButton.interactable = optionField.IsDirty;
        }
        
        private void OnPrivateToggled(bool arg0)
        {
            TrackChange(arg0, controller.IsPrivate);
        }
        
        private void OnSessionNameChanged(string arg0)
        {
            TrackChange(arg0, controller.SessionName);
        }
        
        private void OnPasswordChanged(string arg0)
        {
            TrackChange(arg0, controller.Password);
        }

        private void OnSeekerCountChanged(int arg0)
        {
            TrackChange(arg0 + 1, controller.SeekerCount);
        }
        
        private void OnNpcCountChanged(int arg0)
        {
            TrackChange(arg0 + 3, controller.NpcCount);
        }

        private void OnGameTimeChanged(int arg0)
        {
            TrackChange((arg0 + 5) * 60, controller.GameTime);
        }

        private void Clear()
        {
            applyButton.interactable = false;
            
            controller.Reset();
            
            Initialize();
        }

        private void OnCopyCodeButtonClick()
        {
            GUIUtility.systemCopyBuffer = codeCopyButton.GetComponentInChildren<TMP_Text>().text;
        }

        private void OnPasswordVisibleChanged(bool value)
        {
            targetImage.sprite = !value ? visibleOn : visibleOff;
            passwordInput.inputType = !value ? TMP_InputField.InputType.Password : TMP_InputField.InputType.Standard;
            passwordInput.ForceLabelUpdate();
        }

        private async void OnApplyButtonClick()
        {
            try
            {
                if (!isOpen || (closeSequence?.IsPlaying() ?? false)) return;

                await controller.Save();

                controller.Apply();

                closeSequence.Restart();
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }

        private void OnCancelButtonClick()
        {
            if (!isOpen || (closeSequence?.IsPlaying() ?? false)) return;

            closeSequence.Restart();
        }

        private void SetupOpenSequence()
        {
            openSequence.Insert(0f, rectTransform.DOSizeDelta(openSize, Duration));

            for (var i = 0; i < contents.Count; i++)
            {
                var child = contents[i];
                var delay = i * stepDuration;

                var cg = child.GetComponent<CanvasGroup>();

                openSequence.Insert(delay, DOTween.Sequence()
                    .AppendCallback(() => child.gameObject.SetActive(true))
                    .Join(cg.DOFade(1, 0.2f))
                    .Join(child.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack))
                );
            }

            openSequence.OnComplete(() => isOpen = true);
        }

        private void SetupCloseSequence()
        {
            closeSequence.Insert(0.15f, rectTransform.DOSizeDelta(closeSize, Duration));

            for (var i = contents.Count - 1; i >= 0; i--)
            {
                var child = contents[i];
                var cg = child.GetComponent<CanvasGroup>();

                var delay = (contents.Count - 1 - i) * stepDuration;

                closeSequence.Insert(delay, DOTween.Sequence()
                    .Join(cg.DOFade(0f, 0.15f))
                    .Join(child.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack))
                    .AppendCallback(() => child.gameObject.SetActive(false))
                );
            }

            closeSequence.OnComplete(() => isOpen = false);
        }
        }
}