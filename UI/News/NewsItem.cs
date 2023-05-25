using System;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameRules.Scripts.UI.News
{
    public class NewsItem : MonoBehaviour
    {
        [SerializeField]
        private Image _titleBox;
        [SerializeField]
        private TextMeshProUGUI _title;
        [SerializeField]
        private TextMeshProUGUI _status;
        [SerializeField]
        private TextMeshProUGUI _date;
        [SerializeField]
        private TextMeshProUGUI _message;

        private string _statusKey;
        
        public void Draw(NewsModel model, NewsWindow window)
        {
            var priority = window.GetPriorityInfo(model.StatusType);
            _statusKey = priority.StatusText;
            
            _titleBox.color = priority.TitleBoxColor;
            _status.color = priority.StatusColor;
            _status.text = LocalizationManager.GetTranslation(_statusKey);

            _date.text = model.Date.ToString("d");
            _title.text = model.Title;
            _message.text = model.Message;

            _message.CalculateLayoutInputVertical();

           var rt = GetComponent<RectTransform>();
           var size = rt.sizeDelta;
           size.y = _message.preferredHeight - _message.rectTransform.sizeDelta.y;
           rt.sizeDelta = size;
        }

        private void Awake()
        {
            LocalizationManager.OnLocalizeEvent += OnLocalizeEvent;
        }

        private void OnDestroy()
        {
            LocalizationManager.OnLocalizeEvent -= OnLocalizeEvent;
        }

        private void OnLocalizeEvent()
        {
            if(!string.IsNullOrWhiteSpace(_statusKey))
                _status.text = LocalizationManager.GetTranslation(_statusKey);
        }
    }
}
