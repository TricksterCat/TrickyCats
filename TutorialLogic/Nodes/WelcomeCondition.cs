using System;
using Firebase.Analytics;
using GameRules.Modules.TutorialEngine.Graph.Nodes.ConditionCollectors;
using GameRules.Scripts.TutorialEngine.Nodes;
using GameRules.UI;
using GraphProcessor;

namespace GameRules.Scripts.TutorialLogic.Nodes.Observers
{
    [Serializable, NodeMenuItem("Tutorial/Condition collectors/Welcome condition")]
    public class WelcomeCondition : CustomConditionCollectorNode
    {
        protected override void OnChangeOwnerStatus(StepNode.StepStatus status)
        {
            if (status == StepNode.StepStatus.Process)
                FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventTutorialBegin);
        }

        protected override bool IsReadyRun()
        {
            return  LoadingController.ActiveScene == LoadingController.Scene.Menu && GetOrPush.PlayGames == 0 && WindowsManager.IsOnly("MainMenu");
        }

        protected override bool IsStepCompleted()
        {
            return GetOrPush.PlayGames > 1;
        }
    }
}