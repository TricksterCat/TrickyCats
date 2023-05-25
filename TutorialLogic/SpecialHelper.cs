using GameRules.Modules.TutorialEngine.Input;

namespace GameRules.Scripts.TutorialLogic
{
    internal static class SpecialHelper
    {
        internal class GetPlayGames : IReturnValue<int>
        {
            public static GetPlayGames Instance { get; } = new GetPlayGames();
            
            private GetPlayGames()
            {
                
            }
            
            public int GetValue()
            {
                return GetOrPush.PlayGames;
            }

            public object GetValueAsObject()
            {
                return GetValue();
            }
        }
    }
}