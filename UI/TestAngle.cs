using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class TestAngle : MonoBehaviour
{
    public bool isDebug;
    public Text _text;

    public RectTransform toTr;

    [ShowInInspector, ReadOnly]
    private float _angle;
    [ShowInInspector, ReadOnly]
    private Vector2 _viewPort;

    [SerializeField]
    private Transform _testImage;
    
    // Update is called once per frame
    private void Update()
    {
        if(!isDebug)
            return;

        var root = _text.canvas.rootCanvas;
        var rectTr = (RectTransform) root.transform;
        var canvasSize = rectTr.sizeDelta;

        var tr = ((RectTransform) _testImage.transform);
        var from = GetViewPort(tr, canvasSize);
        var to = GetViewPort(toTr, canvasSize);

        _viewPort = from;
        
        var angle = GetAngleFromVectorFloat(to - from); 

        _angle = angle;
        _text.text = angle.ToString("F2");
        _testImage.localEulerAngles = new Vector3(0,0 , _angle);
    }

    private Vector2 GetViewPort(RectTransform tr, Vector2 canvasSize)
    {
        var from = tr.anchoredPosition;
        from.x += canvasSize.x * 0.5f;
        from.x /= canvasSize.x;
        from.y += canvasSize.y * 0.5f;
        from.y /= canvasSize.y;

        return from;
    }
    
    
    public static float GetAngleFromVectorFloat(Vector2 dir) 
    {
        dir = dir.normalized;
        float n = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90;
        if (n < 0)
            n += 360;

        return 360 - n;
    }
}
