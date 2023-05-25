using System.Numerics;
using Unity.Mathematics;
using UnityEditor;

namespace GameRules.Scripts.ECS.Animator.Editor
{
    public static class WritePropertyExtension
    {
        public static void Write(this SerializedProperty property, float4 value)
        {
            property.FindPropertyRelative("x").floatValue = value.x;
            property.FindPropertyRelative("y").floatValue = value.y;
            property.FindPropertyRelative("z").floatValue = value.z;
            property.FindPropertyRelative("w").floatValue = value.w;
        }
        
        public static void Write(this SerializedProperty property, float3 value)
        {
            property.FindPropertyRelative("x").floatValue = value.x;
            property.FindPropertyRelative("y").floatValue = value.y;
            property.FindPropertyRelative("z").floatValue = value.z;
        }
        
        public static void Write(this SerializedProperty property, float2 value)
        {
            property.FindPropertyRelative("x").floatValue = value.x;
            property.FindPropertyRelative("y").floatValue = value.y;
        }
        
        public static void Write(this SerializedProperty property, quaternion value)
        {
            property.FindPropertyRelative("value").Write(value.value);
        }

    }
}