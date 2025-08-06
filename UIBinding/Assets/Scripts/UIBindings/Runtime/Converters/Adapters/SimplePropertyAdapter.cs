using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Unity.Profiling;

namespace UIBindings.Adapters
{
    /// <summary>
    /// Read/write property value of some source object. Source object is embedded in the adapter, hot swapping is not supported.
    /// </summary>
    /// <typeparam name="TProperty"></typeparam>
    public class SimplePropertyAdapter<TProperty> : PropertyAdapter, IDataReadWriter<TProperty>
    {
        public override Type InputType  => typeof(TProperty);
        public override Type OutputType  => typeof(TProperty);
        
        public  SimplePropertyAdapter( [NotNull] object source, [NotNull] PropertyInfo propertyInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged )
                : base( propertyInfo, isTwoWayBinding, notifyPropertyChanged )
        {
            Assert.IsNotNull( source );
            Assert.IsTrue( propertyInfo.PropertyType == typeof(TProperty) );

            _sourceObject = source;
            _getter = (Func<TProperty>)Delegate.CreateDelegate( typeof(Func<TProperty>), source, propertyInfo.GetGetMethod() );
            _isNeedPolling = source is not INotifyPropertyChanged;
            _onSourcePropertyChangedDelegate = OnSourcePropertyChanged;

            // Is binding one-way we dont want to spent time on creating setter delegate
            if( isTwoWayBinding )
                _setter = (Action<TProperty>)Delegate.CreateDelegate( typeof(Action<TProperty>), source, propertyInfo.GetSetMethod( true ) );
        }

        public EResult TryGetValue(out TProperty value )
        {
            ReadPropertyMarker.Begin( NameofType );
            
            var propValue = _getter();
            if( !_isInited || !EqualityComparer<TProperty>.Default.Equals( propValue, _lastValue ) )        //TODO Check performance of EqualityComparer, consider using custom property adapter for some primitive types
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

        public override void ChangeSourceObject(Object newSourceObject )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// For debugging, ignore boxing
        /// </summary>
        public override EResult TryGetValue(out Object value )
        {
            var result = TryGetValue( out TProperty typedValue );
            value = typedValue;
            return result;
        }

        public void SetValue(TProperty value )
        {
            Assert.IsTrue( IsTwoWay );

            WritePropertyMarker.Begin( NameofType );
            if( !_isInited || !EqualityComparer<TProperty>.Default.Equals( value, _lastValue ) )
            {
                _lastValue = value;
                _isInited  = true;
                _setter( value );
            }
            WritePropertyMarker.End();
        }

        public override void Subscribe( )
        {
            base.Subscribe();

            if( _sourceObject is INotifyPropertyChanged notifyChanged )                
                notifyChanged.PropertyChanged += _onSourcePropertyChangedDelegate;
        }

        public override void Unsubscribe( )
        {
            base.Unsubscribe();

            if( _sourceObject is INotifyPropertyChanged notifyChanged )                
                notifyChanged.PropertyChanged -= _onSourcePropertyChangedDelegate;
        }

        public override Boolean IsNeedPolling( )
        {
            return _isNeedPolling;
        }

        public override String ToString( )
        {
            return  $"{nameof(SimplePropertyAdapter<TProperty>)}{NameofType}.{_getter.Method.Name}' of source '{_sourceObject.GetType().Name}'";
        }

        private static readonly string NameofType = $"<{typeof(TProperty).Name}>";


        private readonly Object                 _sourceObject;
        private readonly Func<TProperty>                _getter;
        private readonly Action<TProperty>              _setter;
        private readonly Boolean                _isNeedPolling;
        private readonly Action<object, string> _onSourcePropertyChangedDelegate;

        private bool _isInited;
        private TProperty    _lastValue;

        private void OnSourcePropertyChanged(Object sender, String propertyName )
        {
            if( String.IsNullOrEmpty( propertyName ) || propertyName == PropertyName )
                NotifyPropertyChanged?.Invoke( sender, propertyName );
        }
    }
}