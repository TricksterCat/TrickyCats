using System;

namespace GameRules.Scripts.Extensions
{
    public class ChangeProperty<T> where T : struct
    {
        public event Action<T> OnChange; 
        
        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                if(_value.Equals(value))
                    return;
                _value = value;
                OnChange?.Invoke(value);
            }
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }
    public class ChangeReferenceProperty<T> where T : class
    {
        public event Action<T> OnChange; 
        
        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                if(ReferenceEquals(value, _value))
                    return;
                _value = value;
                OnChange?.Invoke(value);
            }
        }
        
        public override string ToString()
        {
            return _value.ToString();
        }
    }
}