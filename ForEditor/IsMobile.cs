using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class IsMobile : MonoBehaviour
{
    [SerializeField]
    private UnityEvent _isMobile;
    
    // Start is called before the first frame update
    private void Start()
    {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
    _isMobile.Invoke();
#endif
    }
}
