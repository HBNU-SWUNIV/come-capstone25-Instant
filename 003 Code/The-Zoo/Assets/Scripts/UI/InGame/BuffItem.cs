using Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class BuffItem : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI buffNameText;
        [SerializeField] private TextMeshProUGUI buffSignText;
        [SerializeField] private TextMeshProUGUI buffStackText;

        internal void SetBuff(BuffType type, bool isPositive, int stack)
        {
            image.color = isPositive ? Color.yellowGreen : Color.firebrick;
            buffNameText.text = type.ToString();
            if(type == BuffType.SPEED)
                buffSignText.text = isPositive ? "UP" : "DOWN";
            else
                buffSignText.text = isPositive ? "DOWN" : "UP";
            buffStackText.text = stack.ToString();
        }
    }
}