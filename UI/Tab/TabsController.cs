using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GameRules.Scripts.Modules;
using GameRules.UI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameRules.Scripts.UI.Tab
{
    public class TabsController : MonoBehaviour
    {
        private readonly List<TabBtn> _tabButtons = new List<TabBtn>();
        private TabBtn _selectBtn;

        [SerializeField, BoxGroup("Default")]
        private bool _useDefaultSelect;
        [SerializeField, BoxGroup("Default")]
        private TabBtn _defTab;

        private string _lastTackScreen;
        
        [SerializeField]
        private ShowController _parentShowController;
        
        [SerializeField]
        private RectTransform _activeHandle;

        private IEnumerator Start()
        {
            if(!_useDefaultSelect || _defTab == null)
                yield break;
            
            if (_parentShowController != null)
            {
                while (_parentShowController.CurrentState != ShowController.StateAnimation.Show)
                    yield return null;
            }
            
            _defTab.ChangeTab();
        }

        public void Inject(TabBtn tabBtn)
        {
            _tabButtons.Add(tabBtn);
        }

        public bool IsSelect(TabBtn tabBtn)
        {
            return _selectBtn == tabBtn;
        }

        public void Select(TabBtn tabBtn)
        {
            if(IsSelect(tabBtn))
                return;
            
            _selectBtn?.DeSelect();

            if (tabBtn.TrackScreen)
            {
                if(!string.IsNullOrEmpty(_lastTackScreen))
                    CrowdAnalyticsMediator.Instance.ReplaceCurrentScreen(_lastTackScreen, tabBtn.WindowName);
                else
                    CrowdAnalyticsMediator.Instance.PushCurrentScreen(tabBtn.WindowName);
                
                _lastTackScreen = tabBtn.WindowName;
            }
            
            if(_parentShowController != null)
                _parentShowController.SetBackground(tabBtn.Background);
                
            _selectBtn = tabBtn;
            if (_activeHandle != null)
            {
                var tabTransform = (RectTransform) tabBtn.transform;
                var position = _activeHandle.position;
                position.x = tabTransform.position.x;
                _activeHandle.position = position;
            }
            
            DOTween.Restart("change_main_tab");
        }
    }
}