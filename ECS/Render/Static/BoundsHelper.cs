using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameRules.Scripts.ECS.Render.Static
{
    public class BoundsHelper : MonoBehaviour
    {
        public MeshRenderer Renderer;
        public MeshFilter Filter;

        [ShowInInspector, ReadOnly]
        public Bounds Bounds_Renderer => Renderer.bounds;
        [ShowInInspector, ReadOnly]
        public Matrix4x4 L2W_Renderer => Renderer.localToWorldMatrix;
        
        [ShowInInspector, ReadOnly]
        public Bounds MeshBounds => Filter.sharedMesh.bounds;
        [ShowInInspector, ReadOnly]
        public Matrix4x4 L2W => transform.localToWorldMatrix;
        
        
        
        private void Reset()
        {
            Renderer = GetComponent<MeshRenderer>();
            Filter = GetComponent<MeshFilter>();
        }
    }
}