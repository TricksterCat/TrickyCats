using System;
using System.Collections;
using System.Collections.Generic;
using GameRules.Modules.TutorialEngine;
using GameRules.UI;
using Michsky.UI.ModernUIPack;
using UnityEngine;

public class ExitGame : MonoBehaviour
{
    private ModalWindowManager _manager;

    private void Start()
    {
        _manager = GetComponent<ModalWindowManager>();
    }

    public void CallGameQuit()
    {
        Application.Quit();
    }
    
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(WindowsManager.ActiveWindows.Count == 0 && !Tutorial.IsActive)
                _manager?.OpenWindow();
        }
    }
}
