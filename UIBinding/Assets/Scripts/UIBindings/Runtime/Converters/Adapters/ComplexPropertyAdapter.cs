using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Unity.Profiling;
using UnityEngine.Assertions;

namespace UIBindings.Adapters
{
    /// <summary>
    /// Read property value of some type (and write it if two-way binding)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ComplexPropertyAdapter<TSource, TProperty> : PropertyAdapter, IDataReadWriter<TProperty>
    {
        public override Boolean IsTwoWay { get; }
        public override Type InputType  => typeof(TSource);
        public override Type OutputType  => typeof(TProperty);

        private readonly IDataReadWriter<TSource>     _source;
        private readonly Func<TSource, TProperty>     _getter;
        private readonly Action<TSource, TProperty>   _setter;

        private bool _isInited;
        private TProperty _lastValue;

        public ComplexPropertyAdapter( [NotNull] IDataReadWriter<TSource> source, [NotNull] PropertyInfo propertyInfo, bool isTwoWayBinding )
        {
            Assert.IsNotNull( source );
            Assert.IsNotNull( propertyInfo );
            Assert.IsTrue( !isTwoWayBinding || propertyInfo.CanWrite );
            Assert.IsTrue( propertyInfo.PropertyType == typeof(TProperty) );

            IsTwoWay = isTwoWayBinding;
            _source = source;
            _getter = (Func<TSource, TProperty>)Delegate.CreateDelegate( typeof(Func<TSource, TProperty>), propertyInfo.GetGetMethod() );

            // Is binding one-way we dont want to spent time on creating setter delegate
            if( isTwoWayBinding )
                _setter = (Action<TSource, TProperty>)Delegate.CreateDelegate( typeof(Action<TSource, TProperty>), propertyInfo.GetSetMethod( true ) );
        }

        public EResult TryGetValue(out TProperty value )
        {
            ReadPropertyMarker.Begin( nameof(ComplexPropertyAdapter<TSource, TProperty>) );
            
            _ = _source.TryGetValue( out var sourceValue );     // Do not need to check if source object itself is changed, because property value can change independently
            var propValue = _getter( sourceValue );
            if( !_isInited || !EqualityComparer<TProperty>.Default.Equals( propValue, _lastValue ) )        //TODO Check performance of EqualityComparer, consider using custom property adapter for some primitive types
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
            var result = TryGetValue( out var typedValue );
            value = typedValue;
            return result;
        }

        public void SetValue( TProperty value )
        {
            Assert.IsTrue( IsTwoWay );

            WritePropertyMarker.Begin( nameof(ComplexPropertyAdapter<TSource, TProperty>) );
            _ = _source.TryGetValue( out var sourceObject );
            //if( !_isInited || !EqualityComparer<TProperty>.Default.Equals( value, _lastValue ) ) todo consider write value optimization
            {
                //_lastValue = value;
                //_isInited  = true;
                _setter( sourceObject, value );
            }
            WritePropertyMarker.End();
        }

        public override String ToString( )
        {
            return  $"{nameof(ComplexPropertyAdapter<TSource, TProperty>)}: {typeof(TProperty).Name} '{typeof(TSource).Name}.{_getter.Method.Name}' on source '{_source}'";
        }
    }
}