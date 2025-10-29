using Scriptable;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InGame
{
    public class KeyUI : MonoBehaviour
    {
        [SerializeField] private KeyUIData keyUiData;

        private Image background;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            background = GetComponent<Image>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        internal void Enable()
        {
            SetColor(keyUiData.enableColor);
        }

        internal void Unable()
        {
            SetColor(keyUiData.unableColor);
        }

        internal void SetVisible(bool visible)
        {
            canvasGroup.alpha = visible ? 1 : 0;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void SetColor(Color color)
        {
            background.color = color;
        }
    }
}
