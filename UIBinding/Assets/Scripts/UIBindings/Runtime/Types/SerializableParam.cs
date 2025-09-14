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

        public static SerializableParam FromInt(int value)
        {
            var param = new SerializableParam { Primitive = value, ValueType = EType.Int };
            return param;
        }

        public static SerializableParam FromFloat(float value)
        {
            var encodedValue = UnsafeUtility.As<float, int>( ref value );
            var param = new SerializableParam { Primitive = encodedValue, ValueType = EType.Float };
            return param;
        }

        public static SerializableParam FromBool(bool value)
        {
            var param = new SerializableParam { Primitive = value ? 1 : 0, ValueType = EType.Bool };
            return param;
        }

        public static SerializableParam FromString(string value)
        {
            var param = new SerializableParam { String = value, ValueType = EType.String };
            return param;
        }

        public static SerializableParam FromObject(UnityEngine.Object value)
        {
            var param = new SerializableParam { Object = value, ValueType = EType.Object };
            return param;
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