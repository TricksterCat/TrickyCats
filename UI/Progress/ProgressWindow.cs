using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GameRules;
using GameRules.Core.Runtime;
using Michsky.UI.ModernUIPack;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class ProgressWindow : MonoBehaviour
{
    [SerializeField]
    private ModalWindowManager _modalWindow;
    
    [SerializeField]
    private ScrollRect _scrollRect;

    [SerializeField]
    private RectTransform _content;

    [SerializeField]
    private GameObject _prefab;

    [SerializeField]
    private List<LevelInfo> _pool;

    private float _maxTarget;
    private float _timeCanMove;
    
    

    [SerializeField]
    private Color _isCompleteColor;
    [SerializeField]
    private Color _isNotCompleteColor;
    
    public void Next(bool isRight)
    {
        if(_timeCanMove > Time.time)
            return;
        
        _timeCanMove = Time.time + 0.1f;
        /*if(isRight && _scrollRect.velocity.x > -0.01)
            _scrollRect.velocity = Vector2.zero;
        else if(!isRight && _scrollRect.velocity.x < 0.01)
            _scrollRect.velocity = Vector2.zero;*/

        if (_scrollRect.normalizedPosition.x > 0.9999)
        {
            if(isRight)
                return;
            _scrollRect.normalizedPosition = new Vector2(1f, _scrollRect.normalizedPosition.y);
        }
        else if (_scrollRect.normalizedPosition.x < 0.0001)
        {
            if(!isRight)
                return;
            _scrollRect.normalizedPosition = new Vector2(0f, _scrollRect.normalizedPosition.y);
        }
        
        _scrollRect.velocity += new Vector2(isRight ? -_maxTarget : _maxTarget, 0);
    }

    private void Awake()
    {
        _pool = new List<LevelInfo>();
        _content.GetComponentsInChildren(_pool);
        _maxTarget = _scrollRect.viewport.rect.width;
    }

    public void Open()
    {
        Draw();
        _modalWindow.OpenWindow();
    }

    private void Draw()
    {
        var group = ServerRequest.ProgressTimeline();
        var groupCount = group.Count;
        var count = math.min(_pool.Count, groupCount);

        var current = GetOrPush.Level.Value;
        var lastLevel = group.Count - 1;
        
        int i = 0;
        for (; i < count; i++)
        {
            var element = _pool[i];
            
            element.SetValue(group[i] as JObject, i, IsCompleteLevel(i, current, lastLevel));
            element.SetColor(IsComplete(i, current, lastLevel));
            element.gameObject.SetActive(true);
        }

        for (; i < groupCount; i++)
        {
            var element = Instantiate(_prefab, _content).GetComponent<LevelInfo>();
            _pool.Add(element);
            element.SetValue(@group[i] as JObject, i, IsCompleteLevel(i, current, lastLevel));
            element.SetColor(IsComplete(i, current, lastLevel));
        }
        
        for (;i<_pool.Count;i++)
            _pool[i].gameObject.SetActive(false);
        
        _scrollRect.Rebuild(CanvasUpdate.Layout);
        _scrollRect.normalizedPosition = new Vector2(0, 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Color IsComplete(int index, int level, int lastIndex)
    {
        return index < level && index != lastIndex ? _isCompleteColor : _isNotCompleteColor;
    }

    private bool IsCompleteLevel(int index, int level, int lastIndex)
    {
        return index < level && index != lastIndex;
    }
}
