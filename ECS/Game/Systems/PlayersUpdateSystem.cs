using GameRules.Scripts.ECS.Game.Components;
using GameRules.Scripts.Players;
using Unity.Collections;
using Unity.Entities;

namespace GameRules.Scripts.ECS.Game.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class PlayersUpdateSystem : SystemBase
    {
        private EntityQuery _teams;
        private EntityQuery _playersCount;
        
        protected override void OnCreate()
        {
            _teams = GetEntityQuery(ComponentType.ReadOnly<TeamInfo>());
        }

        protected override void OnUpdate()
        {
            if(World.GetOrCreateSystem<GameMatchSystem>().CurrentState != GameMatchSystem.GAME_STATE.GAME)
                return;
                
            var teams = _teams.ToComponentDataArray<TeamInfo>(Allocator.TempJob);
            if (_playersCount.CalculateEntityCount() != teams.Length - 1)
            {
                Dependency = teams.Dispose(Dependency);
                return;
            }
            
            Entities.ForEach((Player3D_navMesh player) =>
            {
                var info = teams[player.TeamIndex];
                player.TeamSize = info.TeamSize;
                player.Score = (int)info.Score;
            })
                .WithStoreEntityQueryInField(ref _playersCount)
                .WithoutBurst()
                .WithReadOnly(teams)
                .Run();
            
            Dependency = teams.Dispose(Dependency);
        }
    }
}