using System;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

namespace UIBindings.Runtime
{
    [Serializable]
    public class SerializableParam
    {
        public const string PrimitiveFieldName = nameof(Primitive);
        public const string StringFieldName = nameof(String);
        public const string ObjectFieldName = nameof(Object);
        public const string ValueTypeFieldName = nameof(ValueType);

        [SerializeField] int Primitive;
        [SerializeField] String String;
        [SerializeField] UnityEngine.Object Object;
        [SerializeField] EType ValueType;

        //public EType Type => ValueType;

        public void SetInt(int value)
        {
            Primitive = value;
            ValueType = EType.Int;
        }

        public void SetFloat(float value)
        {
            Primitive = UnsafeUtility.As<float, int>( ref value );
            ValueType = EType.Float;
        }

        public void SetBool(bool value)
        {
            Primitive = value ? 1 : 0;
            ValueType = EType.Bool;
        }

        public void SetString(string value)
        {
            String = value;
            ValueType = EType.String;
        }

        public void SetObject(UnityEngine.Object value)
        {
            Object = value;
            ValueType = EType.Object;
        }

        public int GetInt() 
        {
            return Primitive;
        }

        public float GetFloat() 
        {
            return UnsafeUtility.As<int, float>( ref Primitive );
        }

        public bool GetBool() 
        {
            return Primitive != 0;
        }

        public string GetString() 
        {
            return String;
        }

        public UnityEngine.Object GetObject() 
        {
            return Object;
        }

        public System.Object GetBoxedValue( Type desiredType )
        {
            if (desiredType == typeof(int))
                return GetInt();
            if (desiredType == typeof(float))
                return GetFloat();
            if (desiredType == typeof(bool))
                return GetBool();
            if (desiredType == typeof(string))
                return GetString();
            if (typeof(UnityEngine.Object).IsAssignableFrom(desiredType))
                return GetObject();

            return null;
        }

       public enum EType
       {
           Undefined,
           Int,
           Float,
           Bool,
           String,
           Object
       }
    }
}