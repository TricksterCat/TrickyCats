using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowIfAspect : MonoBehaviour
{
    [System.Serializable]
    private struct AspectConfig
    {
        public Vector2 Value;
        public bool IsActive;
    }

    [SerializeField]
    private AspectConfig[] _configs;

    [SerializeField]
    private bool _def;
    
    private void OnEnable()
    {
        var image = GetComponent<Image>();
        if(image == null)
            return;
        var cam = Camera.main;
        var aspect = cam.aspect;
        var result = _def;
        
        for (int i = 0; i < _configs.Length; i++)
        {
            var range = _configs[i].Value;
            if (aspect > range.x && aspect < range.y)
            {
                result = _configs[i].IsActive;
                break;
            }
        }

        image.enabled = result;
    }
}
