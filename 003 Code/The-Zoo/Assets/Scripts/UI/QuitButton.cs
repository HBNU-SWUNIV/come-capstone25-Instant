using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class QuitButton : Button
    {
        protected override void Start()
        {
            onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            AudioManager.Instance.PlayUISfx();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}