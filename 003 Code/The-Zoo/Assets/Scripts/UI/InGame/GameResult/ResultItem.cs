using TMPro;
using UnityEngine;

namespace UI.InGame.GameResult
{
    public class ResultItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerScoreText;

        public void SetPlayerName(string playerName)
        {
            playerNameText.text = playerName;
        }

        public void SetPlayerScore(string playerScore)
        {
            playerScoreText.text = playerScore;
        }
    }
}