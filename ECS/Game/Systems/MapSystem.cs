using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.UnitPathSystem.Components;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace GameRules.Scripts.ECS.Game.Systems
{
    public class MapSystem : SystemBase
    {
        private NativeArray<int> _noTeamUnitsCount;
        private NativeArray<int> _totalUnitsCount;
        
        private NativeArray<TeamStack> _unitsCountByTeam;

        private MapInfo _mapInfo;
        private int _mapSizeInline;
        private bool _isInitialize;

        private EntityQuery _requestNoTeamTranslations;
        private EntityQuery _requestTeamTranslations;
        
        [StructLayout(LayoutKind.Sequential)]
        private struct TeamStack
        {
            public const int TeamCount = 4;
            private unsafe fixed int _value[TeamCount];

            private unsafe int this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _value[index];
            }


            public unsafe int Sum()
            {
                fixed (int* ptr = _value)
                    return math.csum(*(int4*)ptr);
            }
        }

        protected override void OnCreate()
        {
            RequireSingletonForUpdate<MapInfo>();

            _requestNoTeamTranslations = GetEntityQuery(ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<SimplePathMover>(), ComponentType.ReadOnly<RecruitComponent>());
            _requestTeamTranslations = GetEntityQuery(ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<TeamTagComponent>(),
                ComponentType.ReadOnly<RecruitComponent>(), ComponentType.Exclude<SimplePathMover>());
        }

        protected override unsafe void OnUpdate()
        {
            if (!_isInitialize && HasSingleton<MapInfo>())
            {
                _mapInfo = GetSingleton<MapInfo>();
                var mapSize = _mapSizeInline = _mapInfo.Size.x * _mapInfo.Size.y;
                _noTeamUnitsCount = new NativeArray<int>(mapSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                _totalUnitsCount = new NativeArray<int>(mapSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                _unitsCountByTeam = new NativeArray<TeamStack>(mapSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                
                _isInitialize = true;
            }
            
            if (!_isInitialize)
                return;

            var noTeamsCount = (int*)_noTeamUnitsCount.GetUnsafePtr();
            var totalUnitsCount = (int*)_totalUnitsCount.GetUnsafePtr();
            var unitsCountByTeam = (int*)_unitsCountByTeam.GetUnsafePtr();
            
            var size_TeamStack = _mapSizeInline * UnsafeUtility.SizeOf<TeamStack>();
            var size = _mapSizeInline * UnsafeUtility.SizeOf<int>();
            
            var handle = Job.WithName("PrepareMapSystemFrameJob").WithCode(() =>
            {
                UnsafeUtility.MemClear(noTeamsCount, size);
                UnsafeUtility.MemClear(unitsCountByTeam, size_TeamStack);
            })
                .WithNativeDisableUnsafePtrRestriction(noTeamsCount)
                .WithNativeDisableUnsafePtrRestriction(unitsCountByTeam)
                .Schedule(Dependency);
            
            var mapInfo = _mapInfo;
            handle = new CollectNoTeamUnitsJob
            {
                Output = noTeamsCount,
                MapInfo = mapInfo,
                Translations = GetComponentTypeHandle<Translation>(true)
            }.ScheduleParallel(_requestNoTeamTranslations, handle);
            handle = new CollectByTeamUnitsJob
            {
                Output = unitsCountByTeam,
                MapInfo = mapInfo,
                Translations = GetComponentTypeHandle<Translation>(true),
                TeamTagComponents = GetComponentTypeHandle<TeamTagComponent>(true)
            }.ScheduleParallel(_requestTeamTranslations, handle);
            handle = new MergeCountUnitsByCell
            {
                _noTeamUnitsCount = noTeamsCount,
                _unitsCountByTeam = (int4*)unitsCountByTeam,
                _totalUnitsCount = totalUnitsCount
            }.ScheduleBatch(_mapSizeInline, 128, handle);
            
            Dependency = handle;
        }
        
        [BurstCompile, NoAlias]
        private struct CollectNoTeamUnitsJob : IJobChunk
        {
            [ReadOnly, NoAlias]
            public ComponentTypeHandle<Translation> Translations;
            [ReadOnly, NoAlias]
            public MapInfo MapInfo;

            [NativeDisableUnsafePtrRestriction, NoAlias]
            public unsafe int* Output;
            
            public unsafe void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var transitions = chunk.GetNativeArray(Translations);
                //var tmpPositions = new NativeArray<float2>(chunk.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                //for (int i = 0; i < chunk.Count; i++)
                //    tmpPositions[i] = transitions[i].Value.xz * TeamCollisionSystem.InvCellSizeFloat;
                
                for (int i = 0; i < chunk.Count; i++)
                {
                    var position2D = math.asint(math.floor(transitions[i].Value.xz * TeamCollisionSystem.InvCellSizeFloat));
                    position2D -= MapInfo.Start;

                    var index = position2D.y * MapInfo.Size.x + position2D.x;
                    Interlocked.Increment(ref Output[index]);
                }
            }
        }
        
        
        [BurstCompile, NoAlias]
        private struct CollectByTeamUnitsJob : IJobChunk
        {
            [ReadOnly, NoAlias]
            public ComponentTypeHandle<Translation> Translations;
            [ReadOnly, NoAlias]
            public ComponentTypeHandle<TeamTagComponent> TeamTagComponents;
            [ReadOnly, NoAlias]
            public MapInfo MapInfo;

            [NativeDisableUnsafePtrRestriction, NoAlias]
            public unsafe int* Output;
            
            public unsafe void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var transitions = chunk.GetNativeArray(Translations);
                var teamTags = chunk.GetNativeArray(TeamTagComponents);
                //var tmpPositions = new NativeArray<float2>(chunk.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                //for (int i = 0; i < chunk.Count; i++)
                //    tmpPositions[i] = transitions[i].Value.xz * TeamCollisionSystem.InvCellSizeFloat;
                
                for (int i = 0; i < chunk.Count; i++)
                {
                    var position2D = math.asint(math.floor(transitions[i].Value.xz * TeamCollisionSystem.InvCellSizeFloat));
                    position2D -= MapInfo.Start;

                    var index = position2D.y * MapInfo.Size.x + position2D.x;
                    Interlocked.Increment(ref Output[index * TeamStack.TeamCount + teamTags[i].Value - 1]);
                }
            }
        }
        
        [BurstCompile, NoAlias]
        private unsafe struct MergeCountUnitsByCell : IJobParallelForBatch
        {
            [ReadOnly, NativeDisableUnsafePtrRestriction, NoAlias]
            public int* _noTeamUnitsCount;
            [ReadOnly, NativeDisableUnsafePtrRestriction, NoAlias]
            public int4* _unitsCountByTeam;
            
            [WriteOnly, NativeDisableUnsafePtrRestriction, NativeDisableParallelForRestriction, NoAlias]
            public int* _totalUnitsCount;
        
            public void Execute([AssumeRange(0u, uint.MaxValue)]int i, [AssumeRange(0u, uint.MaxValue)]int count)
            {
                for (int iMax = i + count; i < iMax; i++)
                    _totalUnitsCount[i] = _noTeamUnitsCount[i] + math.csum(_unitsCountByTeam[i]);
            }
        }

        protected override void OnStopRunning()
        {
            var handle = _noTeamUnitsCount.Dispose(Dependency);
            handle = _totalUnitsCount.Dispose(handle);
            handle = _unitsCountByTeam.Dispose(handle);
            handle.Complete();
            
            _isInitialize = false;
        }
    }
}