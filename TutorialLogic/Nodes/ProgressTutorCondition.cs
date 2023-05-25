using System;
using Firebase.Analytics;
using GameRules.Core.Runtime;
using GameRules.Modules.TutorialEngine.Graph.Nodes.ConditionCollectors;
using GameRules.Scripts.Modules;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.TutorialEngine.Nodes;
using GameRules.UI;
using GraphProcessor;

namespace GameRules.Scripts.TutorialLogic.Nodes.Observers
{
    [Serializable, NodeMenuItem("Tutorial/Condition collectors/Progress condition")]
    public class ProgressTutorCondition : CustomConditionCollectorNode
    {
        public override string name => "Progress condition";

        protected override void OnChangeOwnerStatus(StepNode.StepStatus status)
        {
        }

        protected override bool IsReadyRun()
        {
            return LoadingController.ActiveScene == LoadingController.Scene.Menu && GetOrPush.Level.Value >= 3 && WindowsManager.IsOnly("MainMenu") && !ServerRequest.IsSyncProcessed && !InventoryDiffsHandler.HaveChange;
        }

        protected override bool IsStepCompleted()
        {
            return false;
        }
    }
}