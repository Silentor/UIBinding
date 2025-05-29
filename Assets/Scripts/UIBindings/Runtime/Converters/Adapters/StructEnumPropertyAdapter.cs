using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UIBindings.Runtime;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UIBindings.Adapters
{
    public class StructEnumPropertyAdapter : PropertyAdapter, IDataReadWriter<StructEnum>
    {
        public override Boolean IsTwoWay  { get; }
        public override Type    InputType { get; }
        public override Type    OutputType { get; }

        public StructEnumPropertyAdapter(Object source, PropertyInfo propertyInfo, Boolean isTwoWayBinding )
        {
            Assert.IsNotNull( source );
            Assert.IsNotNull( propertyInfo );
            Assert.IsTrue( !isTwoWayBinding || propertyInfo.CanWrite, "Property must be writable for two-way binding" );
            Assert.IsTrue( propertyInfo.PropertyType.IsEnum );

            IsTwoWay = isTwoWayBinding;
            _source  = source;
            (_getter, _setter)  = ConstructEnumReaderWriter( source, propertyInfo, isTwoWayBinding );
            InputType = propertyInfo.PropertyType;
            OutputType = typeof(StructEnum);
        }

        private readonly Object _source;
        private readonly Func<StructEnum> _getter;
        private readonly Action<StructEnum> _setter;
        private Boolean _isInited;
        private StructEnum _lastValue;

        //Construct 1 params instance func delegate with boxing. Pack enum value into StructEnum to preserve enum type but avoid boxing to Enum class
        private static (Func<StructEnum> getter, Action<StructEnum> setter) ConstructEnumReaderWriter( Object source, PropertyInfo propertyInfo, Boolean isTwoWayBinding )
        {
            var type1               = propertyInfo.PropertyType;
            var convertGetMethod       = typeof(StructEnumPropertyAdapter).GetMethod( nameof( GenericGetter ), BindingFlags.NonPublic | BindingFlags.Static );
            var closedConvertMethod = convertGetMethod.MakeGenericMethod( type1 );
            var getter              = (Func<StructEnum>)closedConvertMethod.Invoke( null, new [] { source, propertyInfo.GetGetMethod( true ) } );
            Action<StructEnum> setter = null;
            if ( isTwoWayBinding )
            {
                var convertSetMethod = typeof(StructEnumPropertyAdapter).GetMethod( nameof( GenericSetter ), BindingFlags.NonPublic | BindingFlags.Static );
                var closedConvertSetMethod = convertSetMethod.MakeGenericMethod( type1 );
                setter = (Action<StructEnum>)closedConvertSetMethod.Invoke( null, new [] { source, propertyInfo.GetSetMethod( true ) } );
            }

            return (getter, setter);
        }

        private static Func<StructEnum> GenericGetter<TEnum>( Object source, MethodInfo method ) where TEnum : struct, Enum, IConvertible //where TNumeric : struct, IConvertible
        {
            var propGetter = (Func<TEnum>) Delegate.CreateDelegate( typeof(Func<TEnum>), source, method );
            Func<StructEnum> getWrapper = () =>
            {
                var enumValue = propGetter( );
                if ( TryConvert( enumValue, out int intValue ) )
                    return new StructEnum( intValue, typeof(TEnum) );
                throw new InvalidCastException( $"Cannot convert {typeof(TEnum).Name} to StructEnum.Value. Probably enum is too big for int" );
            };
            return getWrapper;
        }

        private static Action<StructEnum> GenericSetter<TEnum>( Object source, MethodInfo method ) where TEnum : struct, Enum, IConvertible //where TNumeric : struct, IConvertible
        {
            var propSetter = (Action<TEnum>) Delegate.CreateDelegate( typeof(Action<TEnum>), source, method );
            Action<StructEnum> setWrapper = (structEnum) =>
            {
                if( structEnum.EnumType != typeof(TEnum) )
                    throw new InvalidCastException( $"Property {method} type is {typeof(TEnum).Name} but StructEnum type is {structEnum.EnumType.Name}" );
                var intValue = structEnum.Value;
                if( TryConvert( intValue, out TEnum enumValue ) )
                    propSetter( enumValue );
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

        public EResult TryGetValue(out StructEnum value )
        {
            var propValue = _getter();
            if( !_isInited || _lastValue != propValue )
            {
                _lastValue = propValue;
                _isInited  = true;
                value      = propValue;
                return EResult.Changed;
            }

            value = default;
            return EResult.NotChanged;
        }

        public void SetValue(StructEnum value )
        {
            Assert.IsTrue( IsTwoWay, "Cannot set value on one-way binding" );

            if( !_isInited || _lastValue != value )
            {
                _lastValue = value;
                _isInited  = true;
                _setter( value );
            }
        }

        public override String ToString( )
        {
            return  $"{GetType().Name} at property {InputType.Name} {_source.GetType().Name}.{_getter.Method.Name} on source '{_source}'";
        }


    }
}