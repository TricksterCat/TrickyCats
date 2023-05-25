using System;
using UnityEngine;

namespace GameRules.Scripts
{
    public enum VarValueType
    {
        String,
        Int,
        Double,
        Bool
    }
    
    public struct VarValue
    {
        [SerializeField]
        private VarValueType _type;

        [SerializeField]
        private string _stringValue;

        [SerializeField]
        private float _floatValue;

        public object GetValue()
        {
            switch (_type)
            {
                case VarValueType.Bool:
                    return (int) _floatValue == 1;
                case VarValueType.Double:
                    return (double)_floatValue;
                case VarValueType.String:
                    return _stringValue;
                case VarValueType.Int:
                    return (long) _floatValue;
            }
            
            throw new NotImplementedException();
        }

        public long GetLong()
        {
            return (long)_floatValue;
        }
    }
}