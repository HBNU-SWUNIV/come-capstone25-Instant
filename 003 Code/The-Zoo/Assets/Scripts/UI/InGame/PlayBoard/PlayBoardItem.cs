using Players.Common;
using TMPro;
using UnityEngine;

namespace UI.InGame.PlayBoard
{
    public class PlayBoardItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI pingText;

        public void SetInfo(string playerName, Role role, ulong ping)
        {
            nameText.text = playerName;
            // SetRole(role);
            SetPing(ping);
        }

        private void SetRole(Role role)
        {
            nameText.color = role switch
            {
                Role.Hider => GameManager.Instance.roleColor.hiderColor,
                Role.Seeker => GameManager.Instance.roleColor.seekerColor,
                _ => GameManager.Instance.roleColor.defaultColor
            };
        }

        public void SetPing(ulong ping)
        {
            pingText.text = $"{ping}";
            pingText.color = ping switch
            {
                < 80 => Color.green,
                < 150 => Color.yellow,
                _ => Color.red
            };
        }
    }
}