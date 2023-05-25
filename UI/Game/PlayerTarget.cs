using System;
using System.Collections;
using System.Collections.Generic;
using GameRules.Core.Runtime;
using GameRules.Scripts.ECS;
using GameRules.Scripts.ECS.Game.Systems;
using GameRulez.Modules.PlayerSystems;
using Players;
using Sirenix.OdinInspector;
using TMPro;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class PlayerTarget : MonoBehaviour
{
    [SerializeField, ChildGameObjectsOnly]
    private CanvasGroup _canvasGroup;
    [SerializeField, ChildGameObjectsOnly]
    private TextMeshProUGUI _teamSizeLabel;
    
    [SerializeField, ChildGameObjectsOnly]
    private RectTransform _arrow;
[SerializeField]
    private int2 _offsetArrow;
    
    [SerializeField]
    private Image[] _images;

    private IPlayerController _player;
    
    [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
    private int _angle;
    [ShowInInspector, Sirenix.OdinInspector.ReadOnly]
    private Vector2 _viewPort;
    
    private static int MainIndex;
    private int _lastTeamSize = -1;
    
    private float2 _viewPortElement;
    private float2 _rectPositionElement;
    private int _dirElement;
    private float _scaleElement;

    public static void SetMainIndex(int index)
    {
        MainIndex = index;
    }
    
    public void Attach(IPlayerController player)
    {
        _viewPortElement = float2.zero;
        _rectPositionElement = float2.zero;
        _dirElement = 0;
        _scaleElement = 1f;
        
        _player = player;
        var color = player.Team.PlayerColor;
        foreach (var image in _images)
            image.color = color;

        gameObject.name = player.Team.Name;
        
        _lastTeamSize = _player.TeamSize;
        _teamSizeLabel.text = _lastTeamSize.ToString();
        _canvasGroup.alpha = player.IsMain ? 1f : 0.4f;

        _nextPosition = ((RectTransform) transform).anchoredPosition;

        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<GameUiSystem>().Attach(this);
    }

    private Vector2 _nextPosition;
    private Vector2 _velocity;

    private void UpdatePosition()
    {
        var rt = (RectTransform) transform;
        rt.anchoredPosition = Vector2.SmoothDamp(rt.anchoredPosition, _nextPosition, ref _velocity, GameUiSystem.UpdatePlayerTarget_TimeRate,float.MaxValue);
    }
    
    public void SetInputCalculatePositions(ref InputData inputData)
    {
        if(!enabled || _player == null)
            return;
        
        var index = _player.TeamIndex - 1; //Исключаем 0 команду
        inputData.PlayerPositions[index] = _player.transform.position;
    }

    public void OnUpdate()
    {
        if(!enabled || _player == null)
            return;
        
        var teamSize = _player.TeamSize;
        if (_lastTeamSize != teamSize)
        {
            _lastTeamSize = teamSize;
            _teamSizeLabel.text = teamSize.ToString();
        }
        
        UpdatePosition();
    }
    
    public void OnUpdatePositions(in OutputData outputData)
    {
        if(!enabled || _player == null)
            return;
        
        var teamSize = _player.TeamSize;
        if (_lastTeamSize != teamSize)
        {
            _lastTeamSize = teamSize;
            _teamSizeLabel.text = teamSize.ToString();
        }
        
        var index = _player.TeamIndex - 1; //Исключаем 0 команду
        
        _dirElement = outputData.Dirs[index];
        _scaleElement = outputData.Scale[index];
        _rectPositionElement = outputData.RectPositions[index];
        _viewPortElement = outputData.Views[index];
        
        var viewElement = _viewPortElement;
        var rectPosition = _rectPositionElement;
        var angle = _dirElement;
        
        transform.localScale = Vector3.one * _scaleElement;
        
        _viewPort = viewElement;
        _angle = angle;
        switch (_angle)
        {
            case 0:
                _arrow.eulerAngles = new Vector3(0, 0, 90);
                _arrow.anchoredPosition = new Vector2(-_offsetArrow.x, 0);
                break;
            case 1:
                _arrow.eulerAngles = new Vector3(0, 0, 0);
                _arrow.anchoredPosition = new Vector2(0, _offsetArrow.y);
                break;
            case 2:
                _arrow.anchoredPosition = new Vector2(_offsetArrow.x, 0);
                _arrow.eulerAngles = new Vector3(0, 0, -90);
                break;
            case 3:
                _arrow.anchoredPosition = new Vector2(0, -_offsetArrow.y);
                _arrow.eulerAngles = new Vector3(0, 0, -180);
                break;
        }
        
        _nextPosition = rectPosition;
    }
    
    public struct InputData
    {
        [Unity.Collections.ReadOnly]
        public NativeArray<float3> PlayerPositions;
        
        [Unity.Collections.ReadOnly]
        public float4x4 P;
        [Unity.Collections.ReadOnly]
        public float4x4 V;
        
        [Unity.Collections.ReadOnly]
        public float2 canvasSize;

        [Unity.Collections.ReadOnly]
        public float2 Min;
        [Unity.Collections.ReadOnly]
        public float2 Max;
        [Unity.Collections.ReadOnly]
        public float2 ScaleRange;
        [Unity.Collections.ReadOnly]
        public float2 ScaleDist;

        public void Dispose(ref JobHandle handle)
        {
            handle = PlayerPositions.Dispose(handle);
        }
    }
    
    public struct OutputData
    {
        [WriteOnly]
        public NativeArray<float2> Views;
        [WriteOnly]
        public NativeArray<float2> RectPositions;
        [WriteOnly]
        public NativeArray<int> Dirs;
        [WriteOnly]
        public NativeArray<float> Scale;

        public void Dispose(ref JobHandle handle)
        {
            handle = Views.Dispose(handle);
            handle = RectPositions.Dispose(handle);
            handle = Dirs.Dispose(handle);
            handle = Scale.Dispose(handle);
        }
    }
    
    private Vector2 GetViewPort(Vector3 world)
    {
        var result = Camera.main.WorldToViewportPoint(world);
        if (result.z < 0)
            result = -result;

        return result;
    }
    
    public static float GetAngleFromVectorFloat(Vector2 dir) 
    {
        dir = dir.normalized;
        float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90;
        if (n < 0)
            n += 360;

        return n;
    }
}
