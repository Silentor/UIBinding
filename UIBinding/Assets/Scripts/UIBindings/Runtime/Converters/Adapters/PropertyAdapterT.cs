using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Unity.Profiling;

namespace UIBindings.Adapters
{
    /// <summary>
    /// Read property value of some type (and write it if two-way binding)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PropertyAdapter<T> : PropertyAdapter, IDataReadWriter<T>
    {
        public override Boolean IsTwoWay { get; }
        public override Type InputType  => typeof(T);
        public override Type OutputType  => typeof(T);

        private readonly Object      _source;
        private readonly Func<T>     _getter;
        private readonly Action<T>   _setter;
        private readonly Action<object, string> _notifyPropertyChanged;

        private bool _isInited;
        private T _lastValue;
        

        public PropertyAdapter( [NotNull] object source, [NotNull] PropertyInfo propertyInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged = null )
        {
            Assert.IsNotNull( source );
            Assert.IsNotNull( propertyInfo );
            Assert.IsTrue( !isTwoWayBinding || propertyInfo.CanWrite );
            Assert.IsTrue( propertyInfo.PropertyType == typeof(T) );

            IsTwoWay = isTwoWayBinding;
            _source = source;
            _getter = (Func<T>)Delegate.CreateDelegate( typeof(Func<T>), source, propertyInfo.GetGetMethod() );
            _notifyPropertyChanged = notifyPropertyChanged;

            if( source is INotifyPropertyChanged notifyChangedSupported )
                notifyChangedSupported.PropertyChanged += (sender, propertyName) =>
                {
                    if( propertyName == propertyInfo.Name )
                    {
                        _notifyPropertyChanged( sender, propertyName );
                    }
                };

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
}