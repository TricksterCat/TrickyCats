using DG.Tweening;
using GameRules.UI;
using I2.Loc;
using UnityEngine;

namespace GameRules.Scripts.UI
{
    public class WaitView : MonoBehaviour
    {
        public static WaitView Instance { get; private set; }

        [SerializeField]
        private DOTweenAnimation _animation;
        [SerializeField]
        private ShowController _showController;
        [SerializeField]
        private Localize _title;
        
        public void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Show(string title)
        {
            var isActive = !string.IsNullOrEmpty(title);
            _title.gameObject.SetActive(isActive);
            if(isActive)
                _title.SetTerm(title);
            
            _animation.DOPlay();
            _showController.Show();
        }

        public void Hide()
        {
            _showController.Hide();
            _animation.DOPause();
        }
    }
}