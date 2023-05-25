using System;
using GameRules.Modules.TutorialEngine.Graph.Nodes.ConditionCollectors;
using GameRules.Scripts.Modules;
using GameRules.Scripts.TutorialEngine.Nodes;
using GameRules.Scripts.UI.WheelOfFortune;
using GameRules.UI;
using GraphProcessor;
using UnityEngine;

namespace GameRules.Scripts.TutorialLogic.Nodes.Observers
{
    [Serializable, NodeMenuItem("Tutorial/Condition collectors/Complete WheelRotate condition")]
    public class CompleteWheelRotateCondition : CustomConditionCollectorNode
    {
        public override string name => "Complete WheelRotate condition";
        
        private WheelOfFortuneWindow _fortuneWindow;
        private bool _isComplete;
        
        protected override void OnChangeOwnerStatus(StepNode.StepStatus status)
        {
            
        }

        protected override bool IsReadyRun()
        {
            if (LoadingController.ActiveScene != LoadingController.Scene.Menu)
                return false;
            
            if(_fortuneWindow == null)
                _fortuneWindow = GameObject.FindObjectOfType<WheelOfFortuneWindow>();
            
            var isReady = _fortuneWindow != null && !_fortuneWindow.WaitRewardDiff.Value && WindowsManager.IsOnly("WheelOfFortune");
            if(isReady)
                CrowdAnalyticsMediator.Instance.CompleteTutorial();
            
            return isReady;
        }

        protected override bool IsStepCompleted()
        {
            return false;
        }
    }
}