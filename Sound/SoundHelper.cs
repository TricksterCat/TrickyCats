using System.Collections;
using System.Collections.Generic;
using GameRules.Scripts;
using UnityEngine;

public class SoundHelper : MonoBehaviour
{
    [SerializeField]
    private AudioClip _clip;
    
    public void Play()
    {
        if(_clip != null)
            SoundManager.Instance.PlaySound(_clip);
    }
}
