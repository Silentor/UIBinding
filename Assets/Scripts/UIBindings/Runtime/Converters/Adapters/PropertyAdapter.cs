using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UIBindings.Runtime;
using Unity.Profiling;
using UnityEngine.Assertions;

namespace UIBindings.Adapters
{
    public class PropertyAdapter<T> : PropertyAdapter, IDataReadWriter<T>
    {
        public override Boolean IsTwoWay { get; }
        public override Type InputType  => typeof(T);
        public override Type OutputType  => typeof(T);

        private readonly Object      _source;
        private readonly Func<T>     _getter;
        private readonly Action<T>   _setter;

        private bool _isInited;
        private T _lastValue;

        public PropertyAdapter( [NotNull] object source, [NotNull] PropertyInfo propertyInfo, bool isTwoWayBinding )
        {
            Assert.IsNotNull( source );
            Assert.IsNotNull( propertyInfo );
            Assert.IsTrue( !isTwoWayBinding || propertyInfo.CanWrite );
            Assert.IsTrue( propertyInfo.PropertyType == typeof(T) );

            IsTwoWay = isTwoWayBinding;
            _source = source;
            _getter = (Func<T>)Delegate.CreateDelegate( typeof(Func<T>), source, propertyInfo.GetGetMethod() );

            // Is binding one-way we dont want to spent time on creating setter delegate
            if( isTwoWayBinding )
                _setter = (Action<T>)Delegate.CreateDelegate( typeof(Action<T>), source, propertyInfo.GetSetMethod( true ) );
        }

        public EResult TryGetValue(out T value )
        {
            ReadPropertyMarker.Begin( nameof(T) );
            
            var propValue = _getter();
            if( !_isInited || !EqualityComparer<T>.Default.Equals( propValue, _lastValue ) )        //TODO Check performance of EqualityComparer, consider using custom property adapter for some primitive types
            {
                _lastValue = propValue;
                _isInited  = true;
                value      = propValue;
                ReadPropertyMarker.End();
                return EResult.Changed;
            }

            ReadPropertyMarker.End();
            value = propValue;
            return EResult.NotChanged;
        }

        /// <summary>
        /// For debugging, ignore boxing
        /// </summary>
        public override EResult TryGetValue(out Object value )
        {
            var result = TryGetValue( out T typedValue );
            value = typedValue;
            return result;
        }

        public void SetValue(T value )
        {
            Assert.IsTrue( IsTwoWay );

            WritePropertyMarker.Begin( nameof(T) );
            if( !_isInited || !EqualityComparer<T>.Default.Equals( value, _lastValue ) )
            {
                _lastValue = value;
                _isInited  = true;
                _setter( value );
            }
            WritePropertyMarker.End();
        }

        public override String ToString( )
        {
            return  $"{nameof(PropertyAdapter<T>)}: {typeof(T).Name} property '{_source.GetType().Name}.{_getter.Method.Name}' on source '{_source}'";
        }
    }

    public abstract class PropertyAdapter : DataProvider
    {
        public abstract Type OutputType  { get; }

        /// <summary>
        /// Sometimes we need to adapt type of property to some other type, for example, if it is enum we want to use StructEnum
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        public static Type GetAdaptedType( Type propertyType )
        {
            if ( propertyType == null ) return null;
            if ( propertyType.IsEnum ) return typeof(StructEnum);
            return propertyType;
        }

        public static PropertyAdapter GetPropertyAdapter( object source, PropertyInfo propertyInfo, bool isTwoWayBinding )
        {
            var type = propertyInfo.PropertyType;

            //Fast way for some common types
            if ( type == typeof(int) )
                return new PropertyAdapter<int>( source, propertyInfo, isTwoWayBinding );
            else if ( type == typeof(float) )
                return new PropertyAdapter<float>( source, propertyInfo, isTwoWayBinding );
            else if( type == typeof(bool) )
                return new PropertyAdapter<bool>( source, propertyInfo, isTwoWayBinding );
            else if ( type == typeof(string) )
                return new PropertyAdapter<string>( source, propertyInfo, isTwoWayBinding );
            else if( type.IsEnum )
                return new StructEnumPropertyAdapter( source, propertyInfo, isTwoWayBinding );

            //Slow way for all other types
            var adapterType = typeof(PropertyAdapter<>).MakeGenericType( type );
            return (PropertyAdapter)Activator.CreateInstance( adapterType, source, propertyInfo, isTwoWayBinding );
        }

        /// <summary>
        /// For debugging, ignore boxing
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract EResult TryGetValue(out object value );

        protected static readonly ProfilerMarker ReadPropertyMarker = new ( ProfilerCategory.Scripts,  $"{nameof(PropertyAdapter)}.ReadProperty" );
        protected static readonly ProfilerMarker WritePropertyMarker = new ( ProfilerCategory.Scripts,  $"{nameof(PropertyAdapter)}.WriteProperty" );
    }
}