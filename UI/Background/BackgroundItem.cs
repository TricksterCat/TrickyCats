using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace GameRules.Scripts.UI.Background
{
    [Serializable]
    public class BackgroundItem
    {
        public string Name => _name;
        
        [SerializeField]
        private string _name;
        [SerializeField]
        private CanvasGroup _canvasGroup;
        
        [SerializeField, BoxGroup("Select")]
        private string _onSelectAnimation;
        [SerializeField, BoxGroup("Select")]
        private UnityEvent _onSelect;
        
        public void Select()
        {
            _canvasGroup.alpha = 1f;
            _onSelect?.Invoke();
            if (!string.IsNullOrEmpty(_onSelectAnimation))
                DOTween.Restart(_onSelectAnimation);
        }
        
        public void Hide()
        {
            _canvasGroup.alpha = 0f;
        }
    }
}