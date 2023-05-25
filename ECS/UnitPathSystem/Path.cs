using GameRules.RustyPool.Runtime;
using GameRules.Scripts.ECS.UnitPathSystem.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace GameRules.Scripts.ECS.UnitPathSystem
{
    [RequireComponent(typeof(Spline2DComponent))]
    public class Path : MonoBehaviour
    {
        public float Y;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var spline = GetComponent<Spline2DComponent>();
            #if UNITY_EDITOR
            //conversionSystem.AddHybridComponent(spline);
            #endif
            if(spline.Count == 0)
                return;
            
            BlobBuilder bb = new BlobBuilder(Allocator.Temp);
            ref var pathAsset = ref bb.ConstructRoot<PathBlobAsset>();
            pathAsset.SegmentLength = spline.distanceMarker;

            var points = TmpList<float3>.Get();
            float len = spline.Length;
            for (float dist = 0.0f; dist <= len; dist += pathAsset.SegmentLength)
            {
                float t = spline.DistanceToLinearT(dist);
                Vector3 p = spline.InterpolateWorldSpace(t);
                p.y = Y;
                points.Add(p);
            }

            var pointsArray = bb.Allocate(ref pathAsset.Positions, points.Count);
            for (int i = 0; i < points.Count; i++)
                pointsArray[i] = points[i];

            BlobAssetReference<PathBlobAsset> blobAssetReference = bb.CreateBlobAssetReference<PathBlobAsset>(Allocator.Persistent);
            dstManager.AddComponentData(entity, new PathComponent
            {
                Value = blobAssetReference
            });
            
            bb.Dispose();
            
        }
    }
}
