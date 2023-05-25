using GameRules.Scripts.ECS.Events;
using GameRules.Scripts.Players;
using GameRules.Scripts.Units;

namespace GameRules.Scripts.ECS
{
    public class UnitConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((Unit3D_navMesh behaviour) => { behaviour.Convert(GetPrimaryEntity(behaviour), DstEntityManager, this); });
            Entities.ForEach((Player3D_navMesh behaviour) => { behaviour.Convert(GetPrimaryEntity(behaviour), DstEntityManager, this); });
            Entities.ForEach((DestroyEntityInject behaviour) => { behaviour.Convert(GetPrimaryEntity(behaviour), DstEntityManager, this); });
            Entities.ForEach((UnitPathSystem.Path behaviour) => { behaviour.Convert(GetPrimaryEntity(behaviour), DstEntityManager, this); });
        }
    }
}