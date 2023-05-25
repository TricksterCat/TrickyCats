using System;
using GameRules.Modules.TutorialEngine.Graph.Nodes.ConditionCollectors;
using GameRules.Scripts.Modules.Database;
using GameRules.Scripts.TutorialEngine.Nodes;
using GraphProcessor;

namespace GameRules.Scripts.TutorialLogic.Nodes.Observers
{
    [Serializable, NodeMenuItem("Tutorial/Condition collectors/To complete buy CowboyCat condition")]
    public class CompleteBuyCowboyCat : CustomConditionCollectorNode
    {
        
        public override string name => "To complete buy CowboyCat condition";
        
        protected override void OnChangeOwnerStatus(StepNode.StepStatus status)
        {
        }

        protected override bool IsReadyRun()
        {
            if (LoadingController.ActiveScene != LoadingController.Scene.Menu)
                return false;
            return Database.All.TryGetValue("PLAYER_COWBOY", out var cowboy) && Inventory.IsAvailability(cowboy);
        }

        protected override bool IsStepCompleted()
        {
            return false;
        }
    }
}