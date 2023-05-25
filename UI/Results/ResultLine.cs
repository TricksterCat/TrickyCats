using ByteSheep.Events;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameRules.Scripts.UI.Results
{
    public class ResultLine : MonoBehaviour
    {
        [SerializeField]
        private Image _back;
        
        [SerializeField]
        private TextMeshProUGUI _nameLabel;
        [SerializeField]
        private TextMeshProUGUI _scoreLabel;
        [SerializeField]
        private Image _avatar;

        [SerializeField]
        private float _maxWidth;

        [SerializeField]
        private AdvancedEvent _onRightText;
        [SerializeField]
        private AdvancedEvent _onLeftText;

        public void SetValues(float width, Color teamColor, string name, int score, Sprite miniIcon)
        {
            if (width > 0)
                _back.rectTransform.sizeDelta = new Vector2(width, _back.rectTransform.sizeDelta.y);
            
            _back.color = teamColor;
            
            _nameLabel.text = name;
            _scoreLabel.text = score.ToString();
            if (_avatar != null)
                _avatar.sprite = miniIcon;
        }

        public void UpdateTextPlace(Color teamColor, bool isMainPlayer)
        {
            
            if (_back.rectTransform.rect.width > _maxWidth)
            {
                _nameLabel.color = isMainPlayer ? new Color(1f - teamColor.r, 1f - teamColor.b, 1f - teamColor.g) : Color.grey;
                _scoreLabel.color = Color.grey;
                _onLeftText.Invoke();
            }
            else
            {
                _scoreLabel.color = teamColor;
                _nameLabel.color = isMainPlayer ? Color.white : Color.grey;
                _onRightText.Invoke();
            }
        }
    }
}
