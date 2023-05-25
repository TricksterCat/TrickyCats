using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DefaultNamespace;
using GameRules.Firebase.Runtime;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.Jobs;
using GameRules.Scripts.ECS.UnitPathSystem.Components;
using JacksonDunstan.NativeCollections;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine.UI;

namespace GameRules.Scripts.ECS.Game.Systems
{
    //[UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(StepPhysicsWorld))]
    [UpdateAfter(typeof(GameUiSystem))]
    public class TeamCollisionSystem : SystemBase
    {
        public const int CellSizeInt = 2;
        public const float CellSizeFloat = CellSizeInt;
        public const float InvCellSizeFloat = 1f / CellSizeInt;
        
        private bool _isGameActive;
        private StepPhysicsWorld _stepPhysicsWorld;
        private BuildPhysicsWorld _buildPhysicsWorld;

        private NativeMultiHashMap<Entity, EntityWithCollisionGroup> _collisions;
        private NativeList<Entity> _uniqueKeys;
        
        private NativeMultiHashMap<int2, Entity> _hookMap;
        private NativeMultiHashMap<int2, Entity> _hunterMap;
        private NativeArray<int> _collisionForceByGroups;
        
        private struct EntityWithCollisionGroup
        {
            public Entity Entity;
            public int Group;

            public EntityWithCollisionGroup(Entity entity, int group)
            {
                Entity = entity;
                Group = @group;
            }
        }
        
        protected override void OnCreate()
        {
            _stepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            _buildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            _collisions = new NativeMultiHashMap<Entity, EntityWithCollisionGroup>(16384, Allocator.Persistent);
            _uniqueKeys = new NativeList<Entity>(1024, Allocator.Persistent);
            
            _hunterMap = new NativeMultiHashMap<int2, Entity>(1024, Allocator.Persistent);
            _hookMap = new NativeMultiHashMap<int2, Entity>(1024, Allocator.Persistent);
            
            _collisionForceByGroups = new NativeArray<int>(new[]
            {
                1, 
                4,
                10000
            }, Allocator.Persistent);
            
            GameMatchSystem.OnMatchBegin += OnMatchBegin;
            GameMatchSystem.OnMatchEnd += OnMatchEnd;
        }

        protected override void OnDestroy()
        {
            var dispose = Dependency;

            dispose = _hunterMap.Dispose(dispose);
            dispose = _hookMap.Dispose(dispose);
            dispose = _collisions.Dispose(dispose);
            dispose = _collisionForceByGroups.Dispose(dispose);
            dispose = _uniqueKeys.Dispose(dispose);
            
            dispose.Complete();
            
            GameMatchSystem.OnMatchBegin -= OnMatchBegin;
            GameMatchSystem.OnMatchEnd -= OnMatchEnd;
        }
        
        private void OnMatchBegin()
        {
            _isGameActive = true;
        }

        private void OnMatchEnd()
        {
            _isGameActive = false;
        }
        
        protected override unsafe void OnUpdate()
        {
            if(!_isGameActive)
                return;

            var hookMap = _hookMap;
            var hunterMap = _hunterMap;
            var collisions = _collisions;
            var keys = _uniqueKeys;
            
            var handle = Job.WithName("ClearMap").WithCode(() =>
            {
                keys.Clear();
                hookMap.Clear();
                hunterMap.Clear();
                collisions.Clear();
            }).Schedule(Dependency);
            
            var time = (float)Time.ElapsedTime;
            var parallelHookMap = hookMap.AsParallelWriter();
            var parallelHunterMap = hunterMap.AsParallelWriter();
            handle = Entities.WithName("CollectAllUnitsNoTeam").WithAll<SimplePathMover>().ForEach((in Entity entity, in Translation translation, in RecruitComponent recruitComponent) =>
                {
                    parallelHookMap.Add((int2)math.floor(translation.Value.xz / CellSizeFloat), entity);
                })
                .ScheduleParallel(handle);
            handle = Entities.WithName("CollectAllUnits").WithAll<NavMeshPathMover>().ForEach((in Entity entity, in Translation translation, in RecruitComponent recruitComponent) =>
                {
                    var key = (int2) math.floor(translation.Value.xz / CellSizeFloat);
                    parallelHunterMap.Add(key, entity);
                    if(recruitComponent.LockedToTime < time)
                        parallelHookMap.Add(key, entity);
                })
                .ScheduleParallel(handle);

            /*handle = new CollectHuntingToHookPairs
            {
                Input = hookMap,
                Units = hunterMap,
                Output = collisions.AsParallelWriter()
            }.Schedule(hookMap, 64, handle);*/

            handle = new CollectHuntingToHookPairsGrid
            {
                Output = collisions.AsParallelWriter()
            }.Schedule(hookMap, hunterMap, 256, handle);
            

            var beginSimulation = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

            var simulation = _stepPhysicsWorld.Simulation;
            ref var world = ref _buildPhysicsWorld.PhysicsWorld;
            
            var count = new NativeInt(Allocator.TempJob);
                    var teams = GetComponentDataFromEntity<TeamTagComponent>(true);
                    handle = new TriggerPlayerCollisionJob
                    {
                        PhysicsWorld = world,
                        PlayerTrigger = 1u << 11,
                        Output = collisions,
                    }.Schedule(simulation, ref world, handle);
                    
                    var buffer = beginSimulation.CreateCommandBuffer();
                    handle = new BulletsTriggerCollisionJob
                    {
                        PhysicsWorld = world,
                        BulletTag = 1u << 3,
                        Time = time,
                        Team = teams,
                        Output = collisions,
                        Recruit = GetComponentDataFromEntity<RecruitComponent>(true),
                        Buffer = buffer
                    }.Schedule(simulation, ref world, handle);
                    beginSimulation.AddJobHandleForProducer(handle);
                    handle = new CalculateUnitsCollisionCount
                    {
                        Count = count,
                        Input = collisions
                    }.Schedule(handle);
                    handle = new MultiHashMapSelectUniqueKeysDefferJob<Entity, EntityWithCollisionGroup>
                    {
                        HashMap = collisions,
                        UniqueKeys = keys.AsParallelWriter()
                    }.ScheduleBatch(count.UnsafeValue, 256, handle);
                    handle = count.Dispose(handle);
                    handle = new CalculateUnitsTeamsJob
                    {
                        Recruits = GetComponentDataFromEntity<RecruitComponent>(false),
                        Teams = teams,
                        HashMap = collisions,
                        UniqueKeys = keys,
                        ForceRecruit = (float)RemoteConfig.GetDouble("ForceRecruit", 5) * Time.DeltaTime,
                        ForceByGroup = _collisionForceByGroups
                    }.Schedule(keys, 64, handle);
                    
                    Dependency = handle;
        }

        private const float UnitsDistTest = 4 * 4;
        

        [BurstCompile]
        private struct CollectHuntingToHookPairs : IJobNativeMultiHashMapUniqueKeysVisit<int2, Entity>
        {
            [ReadOnly]
            public NativeMultiHashMap<int2, Entity> Input;
            [ReadOnly]
            public NativeMultiHashMap<int2, Entity> Units;
            
            [WriteOnly]
            public NativeMultiHashMap<Entity, EntityWithCollisionGroup>.ParallelWriter Output;
            
            public void Execute(ref NativeMultiHashMapIterator<int2> it, ref Entity value, int2 key)
            {
                var found = true;
                while (found)
                {
                    FillOutput(ref value, key);
                    FillOutput(ref value, key + new int2(-1, -1));
                    FillOutput(ref value, key + new int2(-1, 0));
                    FillOutput(ref value, key + new int2(-1, 1));
                    FillOutput(ref value, key + new int2(0, -1));
                    FillOutput(ref value, key + new int2(0, 1));
                    FillOutput(ref value, key + new int2(1, -1));
                    FillOutput(ref value, key + new int2(1, 0));
                    FillOutput(ref value, key + new int2(1, 1));
                    found = Input.TryGetNextValue(out value, ref it);
                }
            }

            private void FillOutput(ref Entity value, int2 key)
            {
                var found = Units.TryGetFirstValue(key, out var otherEntity, out var otherIt);
                while (found)
                {
                    Output.Add(value, new EntityWithCollisionGroup(otherEntity, 0));
                    found = Units.TryGetNextValue(out otherEntity, ref otherIt);
                }
            }
        }
        
        [BurstCompile]
        private struct CollectHuntingToHookPairsGrid : IJobHunterToHookGrid
        {
            [WriteOnly]
            public NativeMultiHashMap<Entity, EntityWithCollisionGroup>.ParallelWriter Output;
            
            public unsafe void ExecuteCenter(ref HookAndHunters data)
            {
                for (int i = 0; i < data.HooksCount; i++)
                {
                    var hookEntity = data.Hooks[i];
                    for (int j = 0; j < data.HuntersCount; j++)
                        Output.Add(hookEntity, new EntityWithCollisionGroup(data.Hunters[j], 0));
                }
            }

            public unsafe void ExecuteBorder(ref HookAndHunters center, ref HookAndHunters border)
            {
                for (int i = 0; i < center.HooksCount; i++)
                {
                    var hookEntity = center.Hooks[i];
                    for (int j = 0; j < border.HuntersCount; j++)
                        Output.Add(hookEntity, new EntityWithCollisionGroup(border.Hunters[j], 0));
                }
                
                for (int i = 0; i < border.HooksCount; i++)
                {
                    var hookEntity = border.Hooks[i];
                    for (int j = 0; j < center.HuntersCount; j++)
                        Output.Add(hookEntity, new EntityWithCollisionGroup(center.Hunters[j], 0));
                }
            }
        }
        
        private struct CalculateUnitsCollisionCount : IJob
        {
            [WriteOnly]
            public NativeInt Count;

            [ReadOnly] 
            public NativeMultiHashMap<Entity, EntityWithCollisionGroup> Input;
            
            public void Execute()
            {
                Count.Value = Input.GetUnsafeBucketData().bucketCapacityMask + 1;
            }
        }
        
        [BurstCompile]
        private struct TriggerPlayerCollisionJob : ITriggerEventsJob
        {
            [ReadOnly]
            public PhysicsWorld PhysicsWorld;
            
            [WriteOnly]
            public NativeMultiHashMap<Entity, EntityWithCollisionGroup> Output;

            [ReadOnly]
            public uint PlayerTrigger;
            
            public void Execute(TriggerEvent triggerEvent)
            {
                var filterA = PhysicsWorld.GetCollisionFilter(triggerEvent.BodyIndexA);
                var filterB = PhysicsWorld.GetCollisionFilter(triggerEvent.BodyIndexB);
                
                if((filterA.BelongsTo & PlayerTrigger) != 0)
                    Output.Add(triggerEvent.EntityB, new EntityWithCollisionGroup(triggerEvent.EntityA, 1));
                else if((filterB.BelongsTo & PlayerTrigger) != 0)
                    Output.Add(triggerEvent.EntityA, new EntityWithCollisionGroup(triggerEvent.EntityB, 1));
            }
        }
        
        [BurstCompile]
        private struct BulletsTriggerCollisionJob : ITriggerEventsJob
        {
            [ReadOnly]
            public PhysicsWorld PhysicsWorld;
            
            [ReadOnly]
            public ComponentDataFromEntity<TeamTagComponent> Team;
            [ReadOnly]
            public ComponentDataFromEntity<RecruitComponent> Recruit;

            [ReadOnly]
            public float Time;

            [ReadOnly]
            public uint BulletTag;

            [WriteOnly]
            public NativeMultiHashMap<Entity, EntityWithCollisionGroup> Output;

            public EntityCommandBuffer Buffer;

            public void Execute(TriggerEvent triggerEvent)
            {
                var filterA = PhysicsWorld.GetCollisionFilter(triggerEvent.BodyIndexA);
                var filterB = PhysicsWorld.GetCollisionFilter(triggerEvent.BodyIndexB);
                
                if((filterA.BelongsTo & BulletTag) != 0)
                    Compare(triggerEvent.EntityA, triggerEvent.EntityB);
                if((filterB.BelongsTo & BulletTag) != 0)
                    Compare(triggerEvent.EntityB, triggerEvent.EntityA);
            }

            private void Compare(Entity bulletEntity, Entity B)
            {
                if (Recruit.HasComponent(B))
                {
                    var recrouteComponent = Recruit[B];

                    var teamA = Team[bulletEntity].Value;
                    if (recrouteComponent.LockedToTime < Time && teamA != Team[B].Value)
                    {
                        Output.Add(B, new EntityWithCollisionGroup(bulletEntity, 2));
                        Buffer.DestroyEntity(bulletEntity);
                    }
                }
                else
                    Buffer.DestroyEntity(bulletEntity);
            }
        }
        
        [BurstCompile]
        private struct CalculateUnitsTeamsJob : IJobParallelForDefer
        {
            [ReadOnly] 
            public float ForceRecruit;
            
            [ReadOnly]
            public NativeArray<int> ForceByGroup;
            [ReadOnly] 
            public NativeMultiHashMap<Entity, EntityWithCollisionGroup> HashMap;
            [ReadOnly]
            public NativeList<Entity> UniqueKeys;

            [ReadOnly]
            public ComponentDataFromEntity<TeamTagComponent> Teams;
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<RecruitComponent> Recruits;
            
            public unsafe void Execute(int index)
            {
                var key = UniqueKeys[index];
                var defTeamIndex = Teams[key].Value;

                int* forcesByTeamUnSort = stackalloc int[15]; // 5 team * 3 group
                
                var found = HashMap.TryGetFirstValue(key, out var value, out var it);
                while (found)
                {
                    forcesByTeamUnSort[value.Group * 5 + Teams[value.Entity].Value] ++;
                    found = HashMap.TryGetNextValue(out value, ref it);
                }

                forcesByTeamUnSort[0] = 0;
                *(int4*)(forcesByTeamUnSort + 1) = *(int4*)(forcesByTeamUnSort + 1) * ForceByGroup[0] + 
                                                *(int4*)(forcesByTeamUnSort + 6) * ForceByGroup[1];
                
                int4 forcesByTeam = *(int4*)(forcesByTeamUnSort + 1);

                int4 forceBulletsByTeam = *(int4*)(forcesByTeamUnSort + 11) * ForceByGroup[2];
                
                var total = math.max(1f, math.csum(forcesByTeam));
                forcesByTeam += forceBulletsByTeam;
                
                var defTeam = forcesByTeamUnSort[defTeamIndex]; //TODO разделить на 2 задачи с пробегам по юнитам имеющим DefTeam и юнитам не имеющим DefTeam
                var attackTeam = total - defTeam;
                
                var bulletForce = math.cmax(forceBulletsByTeam);

                var bestTeam = math.cmax(forcesByTeam);
                var best = math.select(new int4(int.MaxValue), new int4(1, 2, 3, 4), forcesByTeam == bestTeam);
                
                var t = (attackTeam / total - defTeam / total + 1f) * 0.5f;

                var inForce = math.select(0, 1, t < 0.5f);
                var outForce = math.select(1, 0, t < 0.5f);
                
                var dif = ((inForce * InQuadratic(t) + outForce * OutQuadratic(t)) * 2f - 1f) * ForceRecruit;
                
                var rec = Recruits[key];
                rec.Next = math.cmin(best);
                rec.Force = math.max(rec.Force + dif, bulletForce);
                Recruits[key] = rec;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float InQuadratic(float p)
            {
                return p * p;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float OutQuadratic(float p)
            {
                var m = p - 1f;
                return 1f - m * m;
            }
        }
    }
}