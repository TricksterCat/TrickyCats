using System;
using System.Collections;
using DG.Tweening;
using GameRules.RustyPool.Runtime;
using GameRules.Scripts.Modules;
using GameRules.Scripts.UI.Background;
using GameRules.Scripts.UI.Tab;
using GameRules.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class TabBtn : MonoBehaviour
{
    [SerializeField]
    private TabsController _controller;
    private DOTweenAnimation[] _animations;

    private bool _isStart;
    private bool _isCall;
    
    [SerializeField]
    private UnityEvent _onSelect;
    
    [SerializeField, BoxGroup("TrackScreen")]
    private bool _trackScreen;
    [SerializeField, BoxGroup("TrackScreen")]
    private string _windowName;
    
    [SerializeField]
    private BackgroundPreset _backgroundPreset;

    public bool TrackScreen => _trackScreen;
    public string WindowName => _windowName;
    public BackgroundPreset Background => _backgroundPreset;

    private void Awake()
    {
        if(_controller == null)
            return;
        
        _controller.Inject(this);
        
        var list = TmpList<DOTweenAnimation>.Get();
        GetComponentsInChildren(list);
        _animations = TmpList<DOTweenAnimation>.ReleaseAndToArray(list);
    }

    private IEnumerator Start()
    {
        yield return null;
        _isStart = true;
        if(_isCall)
            ChangeTab();
    }

    public void ChangeTab()
    {
        if (!_isStart)
        {
            _isCall = true;
            return;
        }
        
        if(_controller.IsSelect(this))
            return;

        if (_backgroundPreset != null)
            BackgroundManager.Select(_backgroundPreset);
        
        _controller.Select(this);
        for (int i = 0; i < _animations.Length; i++)
            _animations[i].DOPlayForward();
        
        if(!string.IsNullOrEmpty(_windowName))
            WindowsManager.ActiveWindows.Add(_windowName);
        

        _onSelect?.Invoke();
    }

    public void DeSelect()
    {
        if (!_isStart)
        {
            _isCall = false;
            return;
        }
        
        
        if(!string.IsNullOrEmpty(_windowName))
            WindowsManager.ActiveWindows.Remove(_windowName);
        for (int i = 0; i < _animations.Length; i++)
            _animations[i].DOPlayBackwards();
    }
}
