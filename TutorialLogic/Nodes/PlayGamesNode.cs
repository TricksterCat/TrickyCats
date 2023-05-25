using GameRules.Modules.TutorialEngine.Input;
using GameRules.Scripts.TutorialEngine.Nodes;
using GraphProcessor;

namespace GameRules.Scripts.TutorialLogic.Nodes.Observers
{
    [NodeMenuItem("Input/Special/Play games (count)")]
    public class PlayGamesNode : BaseNode
    {
        public override bool canProcess => true;

        [Output("Games count")]
        public IReturnValue<int> Result;

        protected override void Process()
        {
            Result = SpecialHelper.GetPlayGames.Instance;
        }
    }
    
    [NodeMenuItem("Events/Special/Play games count (OnChange)")]
    public class PlayGamesChangeEventNode : ObserverNode
    {
        public override bool canProcess => true;

        protected override void Process()
        {
            
        }
    }
}