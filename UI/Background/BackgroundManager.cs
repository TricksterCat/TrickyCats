using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI.Michsky.UI.ModernUIPack;

namespace GameRules.Scripts.UI.Background
{
    public class BackgroundManager : MonoBehaviour
    {
        [SerializeField]
        private float _cahngeColorTime;
        [SerializeField]
        private UIGradient _uiGradient;
        [SerializeField]
        private BackgroundItem[] _items;
        
        private Tween _tween;
        private static BackgroundPreset _currentAsset;
        private static BackgroundPreset _nextAsset;
        
        private static readonly Dictionary<string, BackgroundItem> _dictionary = new Dictionary<string, BackgroundItem>();
        
        private void Awake()
        {
            _currentAsset = _nextAsset = null;
            _dictionary.Clear();
            
            for (int i = 0; i < _items.Length; i++)
                _dictionary[_items[i].Name] = _items[i];
        }

        private void Update()
        {
            if(_currentAsset == _nextAsset)
                return;

            var old = _currentAsset;

            _currentAsset = _nextAsset;
            if(old == null || _currentAsset.Gradient1 != old.Gradient1)
                ChangeColor(_currentAsset.Gradient1, _cahngeColorTime);
            
            if(old != null && old.Decor == _currentAsset.Decor)
                return;
            
            BackgroundItem item;
            if (string.IsNullOrEmpty(_currentAsset.Decor))
                item = null;
            else if (!_dictionary.TryGetValue(_currentAsset.Decor, out item))
            {
                Debug.LogError($"Not found background with name: \"{_currentAsset.Decor}\"");
                return;
            }
            
            if(old != null && !string.IsNullOrEmpty(old.Decor))
                _dictionary[old.Decor].Hide();
            
            item?.Select();
        }

        public static void Select(BackgroundPreset preset)
        {
            _nextAsset = preset;
        }

        private void ChangeColor(Gradient nextColor, float changeTime)
        {
            var currentColor = _uiGradient.EffectGradient;
            
            _tween?.Kill();
            var first = currentColor.colorKeys[0];
            var end = currentColor.colorKeys[1];

            var colors = new GradientColorKey[2];
            colors[0] = first;
            colors[1] = end;

            var value = 0f;
            DOTween.To(newValue => value = newValue, 0f, 1f, changeTime).OnUpdate(() =>
            {
                colors[0].color = Color.Lerp(first.color, nextColor.colorKeys[0].color, value);
                colors[0].time = Mathf.Lerp(first.time, nextColor.colorKeys[0].time, value);
                
                colors[1].color = Color.Lerp(end.color, nextColor.colorKeys[1].color, value);
                colors[1].time = Mathf.Lerp(end.time, nextColor.colorKeys[1].time, value);
                
                currentColor.SetKeys(colors, currentColor.alphaKeys);
                _uiGradient.Change();
            }).Play();
        }
    }
}