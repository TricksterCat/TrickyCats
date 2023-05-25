using GameRules.Core.Runtime;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.Events;
using GameRules.Scripts.ECS.Game.Components;
using GameRules.Scripts.ECS.Render;
using GameRulez.Modules.PlayerSystems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using Entity = Unity.Entities.Entity;
using RenderBounds = Unity.Rendering.RenderBounds;

namespace GameRules.Scripts.ECS.Game.Systems
{
    public partial class GameMatchSystem
    {
        private bool UpdateWaitPrefabs(ref JobHandle handle)
        {
            if (_unitsInfo == null)
                return false;

            var teamCount = _unitsInfo.Length;
            for (int i = 0; i < teamCount; i++)
            {
                if (ReferenceEquals(_unitsInfo[i], null))
                    return false;
            }
            if (!this.HasSingleton<UnitPrefabs>())
                return false;
            
            var entity = GetSingletonEntity<UnitPrefabs>();
            var initializePrefabs = EntityManager.GetComponentData<UnitPrefabs>(entity);
            
            
            var prefabs = _prefabs;

            var playersSpeed = (float) RemoteConfig.GetDouble("AllPlayersSpeed");
            var minionSpeedMul = (float) RemoteConfig.GetDouble("MinionSpeedMul", 1.2);
            
            var speedArray = new NativeArray<float>(MAX_TEAM_COUNT, Allocator.Temp)
            {
                [0] = (float) RemoteConfig.GetDouble("AllMinionSpeed")
            };
            
            for (int i = 1; i < teamCount; i++)
                speedArray[i] = playersSpeed;

            speedArray[1] *= (float)RemoteConfig.GetDouble("PlayerSpeed");
            
            for (int i = 1; i < teamCount; i++)
                speedArray[i] *= minionSpeedMul;

            int unitsLayer = LayerMask.NameToLayer("UnitWithDither");
            int botsLayer = LayerMask.NameToLayer("Bots");

            for (int i = 0; i < teamCount; i++)
            {
                var unitInfo = _unitsInfo[i];
                var prefab = initializePrefabs[i];
                /*EntityManager.SetComponentData(prefab, new TeamTagComponent
                {
                    Value = i
                });*/

                var child = EntityManager.GetComponentData<SkinComponent>(prefab).Link;
                var l2w = EntityManager.GetComponentData<LocalToWorld>(child).Value;
                l2w = math.mul(l2w, Matrix4x4.Rotate(Quaternion.Euler(unitInfo.Rotate)));
                EntityManager.SetComponentData(child, new LocalToWorld
                {
                    Value = l2w
                });
                
                EntityManager.SetComponentData(child, new URPMaterialPropertyBaseColor
                {
                    Value = (Vector4)unitInfo.Color
                });
                
                EntityManager.SetSharedComponentData(child, new RenderMesh
                {
                    material = unitInfo.Material,
                    mesh = unitInfo.Mesh,
                    castShadows = ShadowCastingMode.Off,
                    receiveShadows = false,
                    subMesh = 0,
                    needMotionVectorPass = false,
                    layer = i == 0 ? botsLayer : unitsLayer
                });
                EntityManager.AddComponentData(child, new RenderBounds
                {
                    Value = new AABB
                    {
                        Center = new float3(0, 1, -0.25f),
                        Extents = new float3(1, 2, 1)
                    }
                });
                EntityManager.SetComponentData(prefab, new SpeedComponent
                {
                    Value = speedArray[i]
                });
                prefabs[i] = prefab;
            }
            speedArray.Dispose();
            
            BarrierEnd.CreateCommandBuffer().DestroyEntity(entity);
                        
            return true;
        }
    }
}