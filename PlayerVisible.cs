using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Core.Base;
using GameRules;
using GameRules.Core.Runtime;
using GameRules.Firebase.Runtime;
using Players;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering.Universal;

public class PlayerVisible : MonoBehaviour
{
    private bool _isRun;
    private Transform _target;

    private const float _timeUpdateDistL  = 2f;

    private IPlayerController _controller;
    private CinemachineFramingTransposer _framingTransposer;
    private int _size;
    private float _targetDist;
    private float _timeUpdateDist;

    [SerializeField]
    private AnimationCurve _curveDistanceCam;

    private float _velocity;
    
#if UNITY_EDITOR
    [SerializeField, BoxGroup("Debug")]
    private bool _isActiveDebug;
    [SerializeField, BoxGroup("Debug"), Range(0, 400)]
    private int _fakeSize;
#endif
    
    private IEnumerator Start()
    {
        var virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        _size = -1;
        _framingTransposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        
        while (virtualCamera.Follow == null || !virtualCamera.Follow.CompareTag("Player"))
            yield return null;

        _target = virtualCamera.Follow;
        _controller = _target.GetComponent<IPlayerController>();
        _isRun = true;

        var postProcessLayer = GetComponent<UniversalAdditionalCameraData>();
        //postProcessLayer.renderPostProcessing = GetOrPush.HighQuality;
        if(postProcessLayer.enabled)
            postProcessLayer.stopNaN = SystemInfo.graphicsShaderLevel >= 35 && RemoteConfig.GetBool("stopNaNPropagation");
    }

    private void FixedUpdate()
    {
        if(!_isRun)
            return;
        
        var size = _controller.TeamSize;
        if (_size == size) 
            return;
        
        _size = size;

        ForceUpdate();
    }


    [Button, BoxGroup("Debug")]
    private void ForceUpdate()
    {
        #if UNITY_EDITOR
        var size = _isActiveDebug ? _fakeSize :  _controller.TeamSize;
        if(!_isActiveDebug)
        _fakeSize = size;
        #else
        var size = _controller.TeamSize;
        #endif

        _timeUpdateDist = Time.time + _timeUpdateDistL;
        _targetDist = _curveDistanceCam.Evaluate(size);
    }

    private void Update()
    {
        if(!_isRun)
            return;
        
        var time = Time.time;
        if (time < _timeUpdateDist)
            _framingTransposer.m_CameraDistance = Mathf.SmoothDamp(_framingTransposer.m_CameraDistance, _targetDist, ref _velocity, _timeUpdateDist - time);
    }
}
