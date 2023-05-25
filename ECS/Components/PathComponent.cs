using Unity.Entities;
using Unity.Mathematics;

namespace GameRules.Scripts.ECS.UnitPathSystem.Components
{
    public struct PathComponent : IComponentData
    {
        public BlobAssetReference<PathBlobAsset> Value;
    }

    public struct NavPathBufferElement : IBufferElementData
    {
        public float2 Value;
    }
    
    public struct PathBlobAsset
    {
        public float SegmentLength;
        public BlobArray<float3> Positions;
    }
}