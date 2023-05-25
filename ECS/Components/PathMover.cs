using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;

namespace GameRules.Scripts.ECS.UnitPathSystem.Components
{
    public struct SimplePathMover : IComponentData
    {
        public int PathIndex;
        public BlobAssetReference<PathBlobAsset> Path;
    }

    public struct NavMeshPathMover : IComponentData
    {
        public int PathIndex;
        
        public bool IsPathValid;
        public double TimeToNewPath;

        public float2 Head;
    }
}