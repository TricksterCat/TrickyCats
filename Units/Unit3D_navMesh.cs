using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.UnitPathSystem.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Scripting;

namespace GameRules.Scripts.Units
{
    [Preserve, RequiresEntityConversion]
    public class Unit3D_navMesh : MonoBehaviour
    {
        public bool IsNavMesh;

        [SerializeField]
        private float _speed;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (IsNavMesh)
            {
                dstManager.AddComponent<NavMeshPathMover>(entity);
                dstManager.AddComponent<NavPathBufferElement>(entity);
            }
            else
            {
                dstManager.AddComponent<SimplePathMover>(entity);
                dstManager.AddComponent<VelocityComponent>(entity);
            }
            
            dstManager.AddComponentData(entity, new SpeedComponent
            {
                Value = _speed
            });
            dstManager.AddComponentData(entity, new RecruitComponent
            {
                LockedToTime = 0,
                Force = 0
            });
        }
    }
}