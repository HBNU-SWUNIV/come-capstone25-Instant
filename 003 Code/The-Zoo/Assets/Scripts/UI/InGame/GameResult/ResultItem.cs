using TMPro;
using UnityEngine;

namespace UI.InGame.GameResult
{
    public class ResultItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerNameText;

        public void SetPlayerName(string playerName)
        {
            playerNameText.text = playerName;
        }
    }
}