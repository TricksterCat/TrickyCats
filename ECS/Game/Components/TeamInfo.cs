using Unity.Entities;

namespace GameRules.Scripts.ECS.Game.Components
{
    [GenerateAuthoringComponent]
    public struct TeamInfo : IComponentData
    {
        //public int LastTeamSize;
        public int TeamSize;
        public float Score;
    }
}