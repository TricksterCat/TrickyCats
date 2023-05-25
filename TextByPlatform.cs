using System.Collections;
using System.Collections.Generic;
using I2.Loc;
using UnityEngine;

public class TextByPlatform : MonoBehaviour
{
    [SerializeField]
    private string _androidText;
    [SerializeField]
    private string _iosText;

    [SerializeField]
    private Localize[] TextLabel;
    
    void Start()
    {
        for (var index = 0; index < TextLabel.Length; index++)
        {
            var text = TextLabel[index];
#if UNITY_ANDROID
            text.Term = _androidText;
#else
            text.Term = _iosText;
#endif
        }
    }
}
