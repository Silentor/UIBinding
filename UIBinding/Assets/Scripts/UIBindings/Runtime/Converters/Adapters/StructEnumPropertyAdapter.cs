using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UIBindings.Runtime;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using Unity.Profiling;

namespace UIBindings.Adapters
{
    public class StructEnumPropertyAdapter<TSource> : PropertyAdapterTyped<TSource, StructEnum>
    {
        public StructEnumPropertyAdapter([NotNull] Object source, [NotNull] PropertyInfo propertyInfo, Boolean isTwoWayBinding, Action<object, string> notifyPropertyChanged )
            : base(source, propertyInfo, GenerateGetter( propertyInfo ), GenerateSetter( propertyInfo, isTwoWayBinding ), isTwoWayBinding, notifyPropertyChanged)    
        {
            Assert.IsTrue( propertyInfo.PropertyType.IsEnum );
        }

        public StructEnumPropertyAdapter([NotNull] PropertyAdapter source,[NotNull] PropertyInfo propertyInfo, Boolean isTwoWayBinding, Action<object, string> notifyPropertyChanged )
                : base(source, propertyInfo, GenerateGetter( propertyInfo ), GenerateSetter( propertyInfo, isTwoWayBinding ), isTwoWayBinding, notifyPropertyChanged)    
        {
            Assert.IsTrue( propertyInfo.PropertyType.IsEnum );
        }


        /// <summary>
        /// Generics + unsafe trick to convert enum to StructEnum (int value + Type) without boxing. Need because converters is not generic for now, so we cannot convert actual enum value
        /// </summary>
        private static Func<TSource, StructEnum> GenerateGetter( PropertyInfo propertyInfo )
        {
            var enumActualType          = propertyInfo.PropertyType;
            var convertGetMethod        = typeof(StructEnumPropertyAdapter<TSource>).GetMethod( nameof( GenericGetAndConvertFunc ), BindingFlags.NonPublic | BindingFlags.Static );
            var closedFuncGenerator     = convertGetMethod.MakeGenericMethod( enumActualType );
            var getter                  = (Func<TSource, StructEnum>)closedFuncGenerator.Invoke( null, new [] { propertyInfo.GetGetMethod( true ) } );
            return getter;
        }

        private static Action<TSource, StructEnum> GenerateSetter( PropertyInfo propertyInfo, bool isTwoWayBinding )
        {
            if ( !isTwoWayBinding )
                return null;

            var enumActualType          = propertyInfo.PropertyType;
            var convertGetMethod        = typeof(StructEnumPropertyAdapter<TSource>).GetMethod( nameof( GenericConvertAndSetAction ), BindingFlags.NonPublic | BindingFlags.Static );
            var closedActionGenerator   = convertGetMethod.MakeGenericMethod( enumActualType );
            var setter                  = (Action<TSource, StructEnum>)closedActionGenerator.Invoke( null, new [] { propertyInfo.GetSetMethod( true ) } );
            return setter;
        }

        private static Func<TSource, StructEnum> GenericGetAndConvertFunc<TEnum>( MethodInfo propertyGetterMethod ) where TEnum : struct, Enum, IConvertible 
        {
            var propGetter = (Func<TSource, TEnum>) Delegate.CreateDelegate( typeof(Func<TSource, TEnum>), propertyGetterMethod );
            Func<TSource, StructEnum> getAndConvert = (sourceObj) =>
            {
                var enumValue = propGetter( sourceObj );
                if ( TryConvert( enumValue, out int intValue ) )
                    return new StructEnum( intValue, typeof(TEnum) );
                throw new InvalidCastException( $"Cannot convert {typeof(TEnum).Name} to StructEnum.Value. Probably enum is too big for int" );
            };
            return getAndConvert;
        }

        private static Action<TSource, StructEnum> GenericConvertAndSetAction<TEnum>( Object source, MethodInfo method ) where TEnum : struct, Enum, IConvertible 
        {
            var propSetter = (Action<TSource, TEnum>) Delegate.CreateDelegate( typeof(Action<TSource, TEnum>), source, method );
            Action<TSource, StructEnum> setWrapper = (sourceObj, structEnum) =>
            {
                if( structEnum.EnumType != typeof(TEnum) )
                    throw new InvalidCastException( $"Property {method} type is {typeof(TEnum).Name} but StructEnum type is {structEnum.EnumType.Name}" );
                var intValue = structEnum.Value;
                if( TryConvert( intValue, out TEnum enumValue ) )
                    propSetter( sourceObj, enumValue );
                else 
                    throw new InvalidCastException( $"Cannot convert StructEnum.Value to {typeof(TEnum).Name}. Probably enum is too small for int" );
            };     
            return setWrapper;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvert<TFrom, TTo>(TFrom from, out TTo to)
                where TFrom : struct
                where TTo : struct
        {
            if (UnsafeUtility.SizeOf<TFrom>() <= UnsafeUtility.SizeOf<TTo>())
            {
                to = UnsafeUtility.As<TFrom, TTo>(ref from);
                return true;
            }
            to = default;
            return false;
        }
    }
}