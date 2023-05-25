using System.Collections.Generic;
using GameRules.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GameRules.Scripts.UI.News
{
    public class NewsWindow : MonoBehaviour
    {
        private Dictionary<string, PriorityInfo> _priority;
        
        [SerializeField]
        private ShowController _showController;

        [SerializeField]
        private RectTransform _content;
        [SerializeField]
        private GameObject _prefab;
        
        [SerializeField]
        private PriorityInfo[] _priorityInfos;

        [SerializeField]
        private GameObject _notiff;

        private void Awake()
        {
            _priority = new Dictionary<string, PriorityInfo>(_priorityInfos.Length);
            for (int i = 0; i < _priorityInfos.Length; i++)
            {
                var info = _priorityInfos[i];
                _priority[info.PriorityId] = info;
            }
            
            var all = NewsData.News;
            for (int i = 0; i < all.Count; i++)
                AddNews(all[i]);

            _content.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 1f;

            if (_notiff != null && !_notiff.activeSelf && NewsData.LastVisitNewsIndex != NewsData.LastIndex)
                _notiff.SetActive(true);
            
            NewsData.GetNewNewsEvent += AddNewsWithNotiff;
        }

        private void OnDestroy()
        {
            NewsData.GetNewNewsEvent -= AddNewsWithNotiff;
        }

        private void AddNews(NewsModel model)
        {
            var newItem = Instantiate(_prefab, _content).GetComponent<NewsItem>();
            newItem.Draw(model, this);
        }
        
        
        private void AddNewsWithNotiff(NewsModel model)
        {
            AddNews(model);
            
            if(_notiff != null && !_notiff.activeSelf && _showController.CurrentState == ShowController.StateAnimation.Hide)
                _notiff.SetActive(true);
        }

        public PriorityInfo GetPriorityInfo(string modelStatusType)
        {
            return _priority[modelStatusType];
        }
        
        public void Open()
        {
            if (_notiff != null && _notiff.activeSelf)
            {
                _notiff.SetActive(false);
                NewsData.LastVisitNewsIndex = NewsData.LastIndex;
            }

            _showController.Show();
        }
    }
}
