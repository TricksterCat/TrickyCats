using System;
using GameRules.Modules.TutorialEngine.Graph.Nodes.ConditionCollectors;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.TutorialEngine.Nodes;
using GameRules.Scripts.UI.WheelOfFortune;
using GameRules.UI;
using GraphProcessor;
using UnityEngine;

namespace GameRules.Scripts.TutorialLogic.Nodes.Observers
{
    [Serializable, NodeMenuItem("Tutorial/Condition collectors/Complete BuyTicket condition")]
    public class CompleteBuyTicket : CustomConditionCollectorNode
    {
        public override string name => "Complete BuyTicket condition";
        private WheelOfFortuneWindow _fortuneWindow;
        
        protected override void OnChangeOwnerStatus(StepNode.StepStatus status)
        {
        }

        protected override bool IsReadyRun()
        {
            if (LoadingController.ActiveScene != LoadingController.Scene.Menu)
                return false;

            if(_fortuneWindow == null)
                _fortuneWindow = GameObject.FindObjectOfType<WheelOfFortuneWindow>();
            
            return Inventory.RouletteTickets.Value > 0 && WindowsManager.IsOnly("WheelOfFortune") && _fortuneWindow.CanRotate;
        }

        protected override bool IsStepCompleted()
        {
            return false;
        }
    }
}