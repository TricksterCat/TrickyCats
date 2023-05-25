using GameRules.Scripts.ECS.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace GameRules.Scripts.ECS.Game.Systems
{
    public struct PlayerShotParams
    {
        public bool IsCanShot;
            
        public float2 Position;
        public float Range;
        public float Speed;
        public float Dispersion;
    }
        
    public struct BulletRequest
    {
        public int TeamIndex;
        
        public float3 Position;
        public float2 Direction;
        public float Speed;
    }
    
    [BurstCompile]
    internal struct UpdateNextShotTargets : IJobParallelFor
    {
        public Random Random;
        
        [DeallocateOnJobCompletion, ReadOnly]
        public NativeArray<PlayerShotParams> Players;

        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<Translation> Positions;
        [ReadOnly, DeallocateOnJobCompletion]
        public NativeArray<TeamTagComponent> Teams;

        [ReadOnly]
        public float dtFix;

        [WriteOnly]
        public NativeQueue<BulletRequest>.ParallelWriter Result;

        public void Execute(int index)
        {
            var player = Players[index];
            if(!player.IsCanShot)
                return;

            var range = player.Range;

            var center = new float3(player.Position.x, 0, player.Position.y);
            
            var distToHit = range * range;
            var indexToHit = -1;

            for (int i = 0; i < Positions.Length; i++)
            {
                var position = Positions[i];
                var dist = math.distancesq(position.Value, center);
                dist = math.select(dist, float.MaxValue, Teams[i].Value == index);
                
                indexToHit = math.select(indexToHit, i, dist < distToHit);
                distToHit = math.min(dist, distToHit);
            }
            
            if (indexToHit >= 0)
            {
                var position = new float3(player.Position.x, 1f, player.Position.y);
                var dir = math.normalizesafe(Positions[indexToHit].Value.xz - player.Position);
                
                var angle = math.select(0, Random.NextFloat(-player.Dispersion / 2, player.Dispersion / 2), player.Dispersion > 0.1f);
                Result.Enqueue(new BulletRequest
                {
                    TeamIndex = index,
                    Position = position,
                    Direction = math.mul(quaternion.RotateY(math.radians(angle)), new float3(dir.x, 0, dir.y)).xz,
                    Speed = player.Speed * dtFix// * 2.5f,
                });
            }
        }
    }
}