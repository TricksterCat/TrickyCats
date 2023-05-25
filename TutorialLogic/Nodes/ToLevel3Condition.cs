using System;
using GameRules.Modules.TutorialEngine.Graph.Nodes.ConditionCollectors;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.TutorialEngine.Nodes;
using GameRules.UI;
using GraphProcessor;

namespace GameRules.Scripts.TutorialLogic.Nodes.Observers
{
    [Serializable, NodeMenuItem("Tutorial/Condition collectors/To Level 3 condition")]
    public class ToLevel3Condition : CustomConditionCollectorNode
    {
        public override string name => "To level 3 condition";
        
        protected override void OnChangeOwnerStatus(StepNode.StepStatus status)
        {
        }

        protected override bool IsReadyRun()
        {
            return LoadingController.ActiveScene == LoadingController.Scene.Menu && GetOrPush.Level.Value >= 3 && GetOrPush.RouletteSpins == 0 && Inventory.HardWallet.Value >= ServerRequest.FortuneHardPrice && WindowsManager.IsOnly("MainMenu") && !ServerRequest.IsSyncProcessed && !InventoryDiffsHandler.HaveChange;
        }

        protected override bool IsStepCompleted()
        {
            return false;
        }
    }
}