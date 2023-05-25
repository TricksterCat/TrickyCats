using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace GameRules.Scripts.ECS.Render
{
    public struct PlanePacket4
    {
        public float4 Xs;
        public float4 Ys;
        public float4 Zs;
        public float4 Distances;
    }
    
    public static class CullingHelper
    {
        public static NativeArray<PlanePacket4> BuildSOAPlanePackets(Plane[] cullingPlanes, Allocator allocator)
        {
            int cullingPlaneCount = cullingPlanes.Length;
            int packetCount = (cullingPlaneCount + 3) >> 2;
            var planes = new NativeArray<PlanePacket4>(packetCount, allocator, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < cullingPlaneCount; i++)
            {
                var p = planes[i >> 2];
                p.Xs[i & 3] = cullingPlanes[i].normal.x;
                p.Ys[i & 3] = cullingPlanes[i].normal.y;
                p.Zs[i & 3] = cullingPlanes[i].normal.z;
                p.Distances[i & 3] = cullingPlanes[i].distance;
                planes[i >> 2] = p;
            }

            // Populate the remaining planes with values that are always "in"
            for (int i = cullingPlaneCount; i < 4 * packetCount; ++i)
            {
                var p = planes[i >> 2];
                p.Xs[i & 3] = 1.0f;
                p.Ys[i & 3] = 0.0f;
                p.Zs[i & 3] = 0.0f;
                p.Distances[i & 3] = 32786.0f; //float.MaxValue;
                planes[i >> 2] = p;
            }

            return planes;
        }
        
        public static NativeArray<PlanePacket4> BuildSOAPlanePackets(NativeArray<Plane> cullingPlanes, Allocator allocator)
        {
            int cullingPlaneCount = cullingPlanes.Length;
            int packetCount = (cullingPlaneCount + 3) >> 2;
            var planes = new NativeArray<PlanePacket4>(packetCount, allocator, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < cullingPlaneCount; i++)
            {
                var p = planes[i >> 2];
                p.Xs[i & 3] = cullingPlanes[i].normal.x;
                p.Ys[i & 3] = cullingPlanes[i].normal.y;
                p.Zs[i & 3] = cullingPlanes[i].normal.z;
                p.Distances[i & 3] = cullingPlanes[i].distance;
                planes[i >> 2] = p;
            }

            // Populate the remaining planes with values that are always "in"
            for (int i = cullingPlaneCount; i < 4 * packetCount; ++i)
            {
                var p = planes[i >> 2];
                p.Xs[i & 3] = 1.0f;
                p.Ys[i & 3] = 0.0f;
                p.Zs[i & 3] = 0.0f;
                p.Distances[i & 3] = 32786.0f; //float.MaxValue;
                planes[i >> 2] = p;
            }

            return planes;
        }
        
        [BurstCompile]
        public static bool IsSkipped(NativeArray<PlanePacket4> cullingPlanePackets, AABB a)
        {
            float4 mx = a.Center.xxxx;
            float4 my = a.Center.yyyy;
            float4 mz = a.Center.zzzz;

            float4 ex = a.Extents.xxxx;
            float4 ey = a.Extents.yyyy;
            float4 ez = a.Extents.zzzz;

            int4 masks = 0;

            for (int i = 0; i < cullingPlanePackets.Length; i++)
            {
                var p = cullingPlanePackets[i];
                float4 distances = dot4(p.Xs, p.Ys, p.Zs, mx, my, mz) + p.Distances;
                float4 radii = dot4(ex, ey, ez, math.abs(p.Xs), math.abs(p.Ys), math.abs(p.Zs));

                masks += (int4) (distances + radii <= 0);
            }

            int outCount = math.csum(masks);
            return outCount != 0;
        }
        
        [BurstCompile]
        public static bool IsVisible(NativeArray<PlanePacket4> cullingPlanePackets, AABB a)
        {
            float4 mx = a.Center.xxxx;
            float4 my = a.Center.yyyy;
            float4 mz = a.Center.zzzz;

            float4 ex = a.Extents.xxxx;
            float4 ey = a.Extents.yyyy;
            float4 ez = a.Extents.zzzz;

            int4 masks = 0;

            for (int i = 0; i < cullingPlanePackets.Length; i++)
            {
                var p = cullingPlanePackets[i];
                float4 distances = dot4(p.Xs, p.Ys, p.Zs, mx, my, mz) + p.Distances;
                float4 radii = dot4(ex, ey, ez, math.abs(p.Xs), math.abs(p.Ys), math.abs(p.Zs));

                masks += (int4) (distances + radii <= 0);
            }

            int outCount = math.csum(masks);
            return outCount == 0;
        }
        
        [BurstCompile]
        private static float4 dot4(float4 xs, float4 ys, float4 zs, float4 mx, float4 my, float4 mz)
        {
            return xs * mx + ys * my + zs * mz;
        }
    }
}