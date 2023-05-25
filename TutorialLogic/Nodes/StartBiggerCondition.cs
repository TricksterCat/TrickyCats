using System;
using GameRules.Modules.TutorialEngine;
using GameRules.Modules.TutorialEngine.Graph.Nodes.ConditionCollectors;
using GameRules.Scripts.TutorialEngine.Nodes;
using GameRules.UI;
using GraphProcessor;
using UnityEngine.SceneManagement;

namespace GameRules.Scripts.TutorialLogic.Nodes.Observers
{
    [Serializable, NodeMenuItem("Tutorial/Condition collectors/Start bigger condition")]
    public class StartBiggerCondition : CustomConditionCollectorNode
    {
        public override string name => "Start bigger condition";

        protected override void OnChangeOwnerStatus(StepNode.StepStatus status)
        {
            if (status == StepNode.StepStatus.Process)
                MenuScene.IsFreeStartBigger = true;
        }

        protected override bool IsStepCompleted()
        {
            return GetOrPush.PlayGames > 1;
        }

        protected override bool IsReadyRun()
        {
            return LoadingController.ActiveScene == LoadingController.Scene.Menu && GetOrPush.PlayGames == 1 && WindowsManager.IsOnly("MainMenu");
        }
    }
}