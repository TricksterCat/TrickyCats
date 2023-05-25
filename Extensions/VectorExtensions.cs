using System.Runtime.CompilerServices;
using UnityEngine;

namespace GameRules.Scripts.Extensions
{
    public static class VectorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 To3D(this Vector2 vector, float y = 0)
        {
            return new Vector3(vector.x, y, vector.y);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 To2D(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.z);
        }
    }
}