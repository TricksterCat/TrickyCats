using System.Runtime.CompilerServices;
using GameRules.Scripts.ECS.Components;
using GameRules.Scripts.ECS.Game.Systems;
using GameRules.Scripts.ECS.Render;
using GameRules.Scripts.ECS.UnitPathSystem.Components;
using GameRules.Scripts.Modules.Collisions.NavMesh;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace GameRules.Scripts.ECS.UnitPathSystem
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(ApplyVelocitySystem))]
    [UpdateAfter(typeof(CopyFromTransformSystem))]
    public class PathMoveSystem : SystemBase
    {
        private struct SpawnPoint
        {
            public int index;
            public int subNextIndex;
            public float3 from;
            public float3 to;

            public SpawnPoint(int i, int next, float3 from, float3 to)
            {
                index = i;
                this.subNextIndex = next;
                this.from = from;
                this.to = to;
            }
        }
        
        private NativeList<SpawnPoint> _spawnPairs;
        
        private NativeArray<PathComponent> _paths;
        private EntityQuery _pathsQuery;
        EntityCommandBufferSystem barrier => World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();

        public void FreePaths()
        {
            if (_paths.IsCreated)
            {
                _paths.Dispose();
                _spawnPairs.Clear();
            }
        }

        protected override void OnCreate()
        {
            _pathsQuery = GetEntityQuery(ComponentType.ReadOnly<PathComponent>());
            _spawnPairs = new NativeList<SpawnPoint>(1024, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            FreePaths();
            _spawnPairs.Dispose();
        }
        
        protected override void OnUpdate()
        {
            var dependency = Dependency;
            var dt = Time.DeltaTime;
            var dtPhysics = Time.DeltaTime * 25;
            
            var count = _pathsQuery.CalculateEntityCount();
            if (!_paths.IsCreated || count != _paths.Length)
            {
                FreePaths();
                _paths = _pathsQuery.ToComponentDataArray<PathComponent>(Allocator.Persistent);
                
                for (int i = 0; i < _paths.Length; i++)
                {
                    ref var points = ref _paths[i].Value.Value.Positions;
                    for (int j = 0; j < points.Length; j++)
                    {
                        var next = (j +1) % points.Length;
                        _spawnPairs.Add(new SpawnPoint(i, next, points[j], points[next]));
                    }
                }
            }
            
            dependency = Entities.WithName("UpdateSimplePath").ForEach((ref VelocityComponent velocity, ref SimplePathMover pathMover, in SpeedComponent speed, in Translation translation) =>
            {
                if(!pathMover.Path.IsCreated)
                    return;
                ref var path = ref pathMover.Path.Value;
                var positionNext = path.Positions[pathMover.PathIndex].xz;

                var dirToNext = math.normalizesafe(positionNext - translation.Value.xz);
                velocity.Value.xz = dirToNext * speed.Value * dt;
                //rotation.Value = math.slerp(rotation.Value, quaternion.LookRotation(new float3(dirToNext.x, 0, dirToNext.y), new float3(0, 1, 0)), dt/2);

                if (math.distancesq(translation.Value.xz, positionNext) < 0.1 * 0.1)
                    pathMover.PathIndex = (pathMover.PathIndex + 1) % path.Positions.Length;
            }).Schedule(dependency);
            
            dependency = Entities.WithName("UpdateNavMeshPath").ForEach((ref PhysicsVelocity velocity, ref Rotation rotation, ref NavMeshPathMover pathMover, in DynamicBuffer<NavPathBufferElement> navPath, in Translation translation, in SpeedComponent speed) =>
            {
                if(!pathMover.IsPathValid || navPath.Length == 0)
                    return;
                
                
                pathMover.PathIndex = math.select(pathMover.PathIndex,
                    math.min(pathMover.PathIndex + 1, navPath.Length - 1),
                    math.distancesq(translation.Value.xz, navPath[pathMover.PathIndex].Value) < 1);


                var prevIndex = pathMover.PathIndex - 1;
                var positionPrev = prevIndex >= 0 ? navPath[prevIndex].Value : translation.Value.xz;
                var positionNext = navPath[pathMover.PathIndex].Value;

                var dirToNext = math.normalizesafe(positionNext - positionPrev);
                velocity.Linear.xz = dirToNext * speed.Value;

                var dir = math.@select(dirToNext, pathMover.Head, pathMover.PathIndex == (navPath.Length - 1));
                
                rotation.Value = math.nlerp(rotation.Value, quaternion.LookRotation(new float3(dir.x, 0, dir.y), new float3(0, 1, 0)), dt*8);

            }).Schedule(dependency);
            
            Dependency = dependency;
        }
        
        public void SpawnToRandomPosition(NativeArray<Entity> entities, ref JobHandle handle)
        {
            var barrierSystem = barrier;
            var paths = _paths;
            var spawnPoints = _spawnPairs;
            
            uint sid = (uint) UnityEngine.Random.Range(1, 1000000);
            var commandBuffer = barrierSystem.CreateCommandBuffer();
            
            handle = Job.WithName("SetRandomPositionAndPath_Job").WithCode(() =>
            {
                var random = new Random(sid);
                for (int i = 0; i < entities.Length; i++)
                    SpawnToRandomPosition(entities[i], ref paths, ref spawnPoints, ref commandBuffer, ref random);
            }).Schedule(handle);
            barrierSystem.AddJobHandleForProducer(handle);
        }
        
        public void SpawnToRandomPosition(Entity prefab, int count, ref JobHandle handle)
        {
            var barrierSystem = barrier;
            var paths = _paths;
            var spawnPoints = _spawnPairs;
            
            uint sid = (uint) UnityEngine.Random.Range(1, 1000000);
            var commandBuffer = barrierSystem.CreateCommandBuffer();
            
            handle = Job.WithName("SpawnInRandomPositionAndPath_Job").WithCode(() =>
            {
                var random = new Random(sid);
                for (int i = 0; i < count; i++)
                    SpawnToRandomPosition(commandBuffer.Instantiate(prefab), ref paths, ref spawnPoints, ref commandBuffer, ref random);
            }).Schedule(handle);
            barrierSystem.AddJobHandleForProducer(handle);
        }
        
        public void SpawnToRandomPosition(Entity entity, Random random, EntityCommandBuffer commandBuffer)
        {
            SpawnToRandomPosition(entity, ref _paths, ref _spawnPairs, ref commandBuffer, ref random);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [BurstCompile]
        private static void SpawnToRandomPosition(Entity entity, ref NativeArray<PathComponent> paths, ref NativeList<SpawnPoint> spawnPoints, ref EntityCommandBuffer commandBuffer, ref Random random)
        {
            var spawnPoint = spawnPoints[random.NextInt(0, spawnPoints.Length)];

            var positions = math.lerp(spawnPoint.from, spawnPoint.to, random.NextFloat());
            positions.xz += random.NextFloat2Direction();
            
            commandBuffer.SetComponent(entity, new Translation
            {
                Value = positions
            });
            commandBuffer.SetComponent(entity, new SimplePathMover
            {
                Path = paths[spawnPoint.index].Value,
                PathIndex = spawnPoint.subNextIndex
            });
            commandBuffer.SetComponent(entity, new UnitRenderComponent
            {
                StartAnimation = random.NextFloat(0, 100) + random.NextFloat(0, 10) + random.NextFloat()
            });
            commandBuffer.AddComponent(entity, new Scale
            {
                Value = random.NextFloat(0.9f, 1.06f)
            });
        }

    }
}