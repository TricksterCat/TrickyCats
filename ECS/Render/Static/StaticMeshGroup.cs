using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameRules.Scripts.ECS.Render.Static
{
    public struct StaticMeshGroup : IComponentData
    {
        public BlobAssetReference<GroupInfosArray> Data;
    }
    
    public class StaticMeshGroupResources : IComponentData
    {
        public Object[] Assets;
    }
    
    public struct InjectToRendererSystemTag : IComponentData
    {
        
    }
    
    public struct GroupInfosArray
    {
        public BlobArray<GroupInfo> Values;
    }
    
    public struct GroupInfo
    {
        public GroupInfoManagedLink Managed;

        public BlobArray<Matrix4x4> Positions;
        public BlobArray<AABB> Bounds;
    }

    public struct GroupInfoManagedLink : IEquatable<GroupInfoManagedLink>
    {
        public int Mesh;
        public int Material;
        public int SubMeshIndex;

        public bool Equals(GroupInfoManagedLink other)
        {
            return Mesh == other.Mesh && Material == other.Material && SubMeshIndex == other.SubMeshIndex;
        }

        public int GetHashCode()
        {
            unchecked
            {
                var hashCode = Mesh;
                hashCode = (hashCode * 397) ^ Material;
                hashCode = (hashCode * 397) ^ SubMeshIndex;
                return hashCode;
            }
        }
    }
}