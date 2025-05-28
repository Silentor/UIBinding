using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace UIBindings.Adapters
{
    public class PropertyAdapter<T> : PropertyAdapter, IDataReadWriter<T>
    {
        public override Boolean IsTwoWay { get; }
        public override Type InputType  => typeof(T);

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

        public Boolean TryGetValue(out T value )
        {
            var propValue = _getter();
            if( !_isInited || !EqualityComparer<T>.Default.Equals( propValue, _lastValue ) )        //TODO Check performance of EqualityComparer, consider using custom property adapter for some primitive types
            {
                _lastValue = propValue;
                _isInited  = true;
                value      = propValue;
                return true;
            }

            value = default;
            return false;
        }

        public void SetValue(T value )
        {
            Assert.IsTrue( IsTwoWay );

            if( !_isInited || !EqualityComparer<T>.Default.Equals( value, _lastValue ) )
            {
                _lastValue = value;
                _isInited  = true;
                _setter( value );
            }
        }
    }

    public abstract class PropertyAdapter : DataProvider
    {
        public static PropertyAdapter GetPropertyAdapter( object source, PropertyInfo property, bool isTwoWay )
        {
            var type = property.PropertyType;

            //Fast way for some common types
            if ( type == typeof(int) )
                return new PropertyAdapter<int>( source, property, isTwoWay );
            else if ( type == typeof(float) )
                return new PropertyAdapter<float>( source, property, isTwoWay );
            else if( type == typeof(bool) )
                return new PropertyAdapter<bool>( source, property, isTwoWay );
            else if ( type == typeof(string) )
                return new PropertyAdapter<string>( source, property, isTwoWay );

            //Slow way for all other types
            var adapterType = typeof(PropertyAdapter<>).MakeGenericType( type );
            return (PropertyAdapter)Activator.CreateInstance( adapterType, source, property, isTwoWay );
        } 
    }
}