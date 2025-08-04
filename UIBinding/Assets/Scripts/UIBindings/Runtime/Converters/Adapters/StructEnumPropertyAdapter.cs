using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using UIBindings.Runtime;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using Unity.Profiling;

namespace UIBindings.Adapters
{
    public class StructEnumPropertyAdapter : PropertyAdapter, IDataReadWriter<StructEnum>
    {
        public override Type    InputType { get; }
        public override Type    OutputType { get; }

        public StructEnumPropertyAdapter(Object source, PropertyInfo propertyInfo, Boolean isTwoWayBinding, Action<object, string> notifyPropertyChanged )
            : base(propertyInfo, isTwoWayBinding, notifyPropertyChanged)    
        {
            Assert.IsNotNull( source );
            Assert.IsTrue( propertyInfo.PropertyType.IsEnum );

            _source  = source;
            (_getter, _setter)  = ConstructEnumReaderWriter( source, propertyInfo, isTwoWayBinding );
            InputType = propertyInfo.PropertyType;
            OutputType = typeof(StructEnum);
            _onSourcePropertyChangedDelegate = OnSourcePropertyChanged;
            _isNeedPolling = source is not INotifyPropertyChanged;
            NameofType = $"<{InputType.Name}>";
        }

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
            ReadPropertyMarker.Begin( NameofType );

            var propValue = _getter();
            if( !_isInited || _lastValue != propValue )
            {
                _lastValue = propValue;
                _isInited  = true;
                value      = propValue;
                ReadPropertyMarker.End();
                return EResult.Changed;
            }

            value = propValue;
            ReadPropertyMarker.End();
            return EResult.NotChanged;
        }

        public override EResult TryGetValue(out Object value )
        {
            var result = TryGetValue( out StructEnum typedValue );
            value = typedValue;
            return result;
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

        public override void Subscribe( )
        {
            base.Subscribe();

            if( _source is INotifyPropertyChanged notifyChanged )                
                notifyChanged.PropertyChanged += _onSourcePropertyChangedDelegate;
        }

        public override void Unsubscribe( )
        {
            base.Unsubscribe();

            if( _source is INotifyPropertyChanged notifyChanged )                
                notifyChanged.PropertyChanged -= _onSourcePropertyChangedDelegate;
        }

        public override Boolean IsNeedPolling() => _isNeedPolling;

        public override String ToString( )
        {
            return  $"{nameof(StructEnumPropertyAdapter)}{NameofType}.{_getter.Method.Name} of source '{_source.GetType().Name}'";
        }

        private readonly string NameofType;

        private readonly Object                 _source;
        private readonly Func<StructEnum>       _getter;
        private readonly Action<StructEnum>     _setter;
        private readonly Boolean                _isNeedPolling;
        private readonly Action<object, string> _onSourcePropertyChangedDelegate;

        private Boolean    _isInited;
        private StructEnum _lastValue;

        private void OnSourcePropertyChanged(Object sender, String propertyName )
        {
            if ( String.IsNullOrEmpty( propertyName ) || propertyName ==  PropertyName )
            {
                NotifyPropertyChanged?.Invoke( sender, propertyName );
            }
        }
    }
}