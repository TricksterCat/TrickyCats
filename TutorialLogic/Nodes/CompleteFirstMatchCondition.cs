using System;
using GameRules.Modules.TutorialEngine.Graph.Nodes.ConditionCollectors;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.TutorialEngine.Nodes;
using GameRules.UI;
using GraphProcessor;

namespace GameRules.Scripts.TutorialLogic.Nodes.Observers
{
    
    [Serializable, NodeMenuItem("Tutorial/Condition collectors/Complete first match condition")]
    public sealed class CompleteFirstMatchCondition : CustomConditionCollectorNode
    {
        public override string name => "Complete first match condition";

        protected override void OnChangeOwnerStatus(StepNode.StepStatus status)
        {
            if (status == StepNode.StepStatus.Process)
            {
                var result = GetOrPush.LastMatchResult;
                ((ShowPopupNode)Listener.Owner).Message.MessageKey = result > 3 ? "Tutorial/FIRST_MATCH_COMPLETE_V2" : "Tutorial/FIRST_MATCH_COMPLETE_V1";
            }
        }

        protected override bool IsReadyRun()
        {
            return LoadingController.ActiveScene == LoadingController.Scene.Menu && WindowsManager.IsOnly("MainMenu") && GetOrPush.PlayGames == 1 && !ServerRequest.IsSyncProcessed && !InventoryDiffsHandler.HaveChange;
        }

        protected override bool IsStepCompleted()
        {
            return GetOrPush.PlayGames > 1;
        }
    }
}