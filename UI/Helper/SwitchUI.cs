using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SwitchUI : MonoBehaviour
{
    [SerializeField, Required, ChildGameObjectsOnly, FoldoutGroup("Settings")]
    private RectTransform _statusItem;
    [SerializeField, FoldoutGroup("Settings")]
    private Color _nonActiveColor;
    [SerializeField, FoldoutGroup("Settings")]
    private Color _activeColor;

    [SerializeField, Range(0.1f, 1f)]
    private float _timeAnimation;
    [SerializeField]
    private bool _status;

    private Image _image;

    private Coroutine _coroutine;

    [Button]
    private void Change()
    {
        ChangeStatus(!_status);
    }

    private void Awake()
    {
        _image = GetComponent<Image>();
        OnEnd();
    }

    private void OnDisable()
    {
        OnEnd();
    }

    public void ChangeStatus(bool status)
    {
        if(_status == status)
            return;
        _status = status;

        if (_image == null)
            _image = GetComponent<Image>();

        if (!gameObject.activeInHierarchy)
        {
            OnEnd();
            return;
        }
        if (_coroutine == null)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                Unity.EditorCoroutines.Editor.EditorCoroutineUtility.StartCoroutine(InternalChangeStatus(), _status);
            else
            #endif
                _coroutine = StartCoroutine(InternalChangeStatus());
        }
    }

    private IEnumerator InternalChangeStatus()
    {
        float time = _timeAnimation;
        float velocity = 0;
        
        var status = _status;
        
        while (time > 0)
        {
            var normalizeTime = 1f - time / _timeAnimation;
            _image.color = Color.Lerp(_nonActiveColor, _activeColor, status ? normalizeTime : 1f - normalizeTime);
            
            _statusItem.pivot = new Vector2(Mathf.SmoothDamp(_statusItem.pivot.x, _status ? 0f : 1f, ref velocity, time), 0.5f);
            
            yield return null;

            ValidateStatus(ref status, ref time, ref velocity);
            time -= Time.deltaTime;
        }

        ValidateStatus(ref status, ref time, ref velocity);
        
        OnEnd();
    }

    private void ValidateStatus(ref bool status, ref float time, ref float velocity)
    {
        if (status == _status) 
            return;
        
        status = _status;
        time = _timeAnimation - time;
        velocity = 0;
    }

    private void OnEnd()
    {
        if (_status)
        {
            _image.color = _activeColor;
            _statusItem.pivot = new Vector2(0, 0.5f);
        }
        else
        {
            _image.color = _nonActiveColor;
            _statusItem.pivot = new Vector2(1, 0.5f);
        }

        if(_coroutine != null)
            StopCoroutine(_coroutine);
        _coroutine = null;
    }
}
