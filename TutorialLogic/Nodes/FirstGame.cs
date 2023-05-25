using System;
using GameRules;
using GameRules.Modules.TutorialEngine.Graph.Nodes.ConditionCollectors;
using GameRules.Scripts.TutorialEngine.Nodes;
using GraphProcessor;
using UnityEngine;

[Serializable, NodeMenuItem("Tutorial/Condition collectors/First game condition")]
public class FirstGameCondition : CustomConditionCollectorNode
{
    private int _id;
    private GameObject[] _fingers;
    
    public override string name => "First game condition";

    protected override void OnChangeOwnerStatus(StepNode.StepStatus status)
    {
        GameObject[] fingers;
        switch (status)
        {
            case StepNode.StepStatus.Process:
                fingers = GameObject.FindGameObjectsWithTag("GameFinger");
                if (fingers != null)
                {
                    _fingers = new GameObject[fingers.Length];
                    for (int i = 0; i < fingers.Length; i++)
                    {
                        var finger = fingers[i].transform;
                        var go = finger.GetChild(finger.childCount - 1).gameObject;
                        go.SetActive(true);
                        _fingers[i] = go;
                    }
                }
            
                Time.timeScale = 0;
                break;
            case StepNode.StepStatus.Complete:
                Time.timeScale = 1;
            
                fingers = _fingers;
                if (fingers != null)
                {
                    for (int i = fingers.Length - 1; i >= 0; i--)
                        GameObject.Destroy(fingers[i]);
                    _fingers = null;
                }
                break;
        }
    }

    protected override bool IsReadyRun()
    {
        return LoadingController.ActiveScene == LoadingController.Scene.Game;
    }

    protected override bool IsStepCompleted()
    {
        return GetOrPush.PlayGames > 0;
    }
}
