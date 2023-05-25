using System;
using GameRules.Modules.TutorialEngine.Graph.Nodes.ConditionCollectors;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.TutorialEngine.Nodes;
using GameRules.UI;
using GraphProcessor;

namespace GameRules.Scripts.TutorialLogic.Nodes.Observers
{
    [Serializable, NodeMenuItem("Tutorial/Condition collectors/To Level 2 condition")]
    public class Level2Condition : CustomConditionCollectorNode
    {
        public override string name => "To level 2 condition";
        
        protected override void OnChangeOwnerStatus(StepNode.StepStatus status)
        {
            
        }

        protected override bool IsReadyRun()
        {
            return LoadingController.ActiveScene == LoadingController.Scene.Menu && GetOrPush.Level.Value == 2 && WindowsManager.IsOnly("MainMenu") && 
                   Database.All.TryGetValue("PLAYER_COWBOY", out var cowboy) && Inventory.SoftWallet.Value >= cowboy.Conditions.SoftPrice && !ServerRequest.IsSyncProcessed && !InventoryDiffsHandler.HaveChange;
        }

        protected override bool IsStepCompleted()
        {
            return !Database.All.TryGetValue("PLAYER_COWBOY", out var cowboy) || cowboy.Conditions.SoftPrice == 0 || Inventory.IsAvailability(cowboy) || GetOrPush.Level.Value > 2;
        }
    }
}