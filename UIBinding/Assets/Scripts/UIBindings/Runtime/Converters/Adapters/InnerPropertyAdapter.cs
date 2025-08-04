using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Unity.Profiling;
using UnityEngine.Assertions;

namespace UIBindings.Adapters
{
    /// <summary>
    /// Read/write property of some object that is read by another property adapter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InnerPropertyAdapter<TSource, TProperty> : PropertyAdapter, IDataReadWriter<TProperty>
    {
        public override Type InputType  => typeof(TSource);
        public override Type OutputType  => typeof(TProperty);

        public InnerPropertyAdapter( [NotNull] PropertyAdapter source, [NotNull] PropertyInfo propertyInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) 
                : base( propertyInfo, isTwoWayBinding, notifyPropertyChanged )
        {
            Assert.IsNotNull( source );
            Assert.IsTrue( propertyInfo.PropertyType == typeof(TProperty) );
            Assert.IsTrue( propertyInfo.DeclaringType == typeof(TSource) );
            
            _sourcePropertyAdapter = (IDataReadWriter<TSource>)source;
            _getter = (Func<TSource, TProperty>)Delegate.CreateDelegate( typeof(Func<TSource, TProperty>), propertyInfo.GetGetMethod( true ) );
            _onSourcePropertyChangedDelegate = OnSourcePropertyChanged;

            // Is binding one-way we dont want to spent time on creating setter delegate
            if( isTwoWayBinding )
                _setter = (Action<TSource, TProperty>)Delegate.CreateDelegate( typeof(Action<TSource, TProperty>), propertyInfo.GetSetMethod( true ) );
        }

        public EResult TryGetValue(out TProperty value )
        {
            ReadPropertyMarker.Begin( NameofTypes );
            
            var sourceChangedStatus = _sourcePropertyAdapter.TryGetValue( out var sourceObject );
            
            //Handle INotifyPropertyChanged on source object
            if ( sourceChangedStatus != EResult.NotChanged && IsSubscribed )
            {
                if( _lastSourceObject is INotifyPropertyChanged oldNotifyChanged )                    
                    oldNotifyChanged.PropertyChanged -= _onSourcePropertyChangedDelegate;

                if ( sourceObject != null )
                {
                    if( sourceObject is INotifyPropertyChanged newNotifyChanged )
                    {
                        newNotifyChanged.PropertyChanged += _onSourcePropertyChangedDelegate;
                        _isNeedPolling                   =  false; //If we have INotifyPropertyChanged we dont need polling
                    }
                    else
                        _isNeedPolling = true;
                }
                else
                    _isNeedPolling = false;

                _lastSourceObject = sourceObject;
            }

            var propValue = sourceObject != null ?_getter( sourceObject ) : default;
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

            WritePropertyMarker.Begin( NameofTypes );
            _ = _sourcePropertyAdapter.TryGetValue( out var sourceObject );
            //if( !_isInited || !EqualityComparer<TProperty>.Default.Equals( value, _lastValue ) ) todo consider write value optimization
            {
                //_lastValue = value;
                //_isInited  = true;
                _setter( sourceObject, value );
            }
            WritePropertyMarker.End();
        }

        public override void Subscribe( )
        {
            base.Subscribe();

            //Do not subscribe right now, actually subscribe at TryGetValue because we do not want read source object before planned update

            ((PropertyAdapter)_sourcePropertyAdapter).Subscribe( );
        }

        public override void Unsubscribe( )
        {
            base.Unsubscribe();

            if ( _lastSourceObject is INotifyPropertyChanged sourceObjectWasNotifyable )
            {
                sourceObjectWasNotifyable.PropertyChanged -= _onSourcePropertyChangedDelegate;
                _lastSourceObject = default; 
            }

            ((PropertyAdapter)_sourcePropertyAdapter).Unsubscribe( );
        }

        public override Boolean IsNeedPolling( )
        {
            return _isNeedPolling || ((PropertyAdapter)_sourcePropertyAdapter).IsNeedPolling( );
        }

        public override String ToString( )
        {
            return  $"{nameof(InnerPropertyAdapter<TSource, TProperty>)}{NameofTypes}.{_getter.Method.Name}";
        }

        private static readonly string NameofTypes = $"<{typeof(TSource).Name},{typeof(TProperty).Name}>";

        private void OnSourcePropertyChanged( object sender, String propertyName )
        {
            if( String.IsNullOrEmpty( propertyName ) || propertyName == PropertyName )
            {
                NotifyPropertyChanged?.Invoke( sender, propertyName );
            }
        }

        private readonly IDataReadWriter<TSource>   _sourcePropertyAdapter;
        private readonly Func<TSource, TProperty>   _getter;
        private readonly Action<TSource, TProperty> _setter;
        private readonly Action<object, string>     _onSourcePropertyChangedDelegate;

        private bool      _isInited;
        private TProperty _lastValue;
        private TSource   _lastSourceObject;
        private Boolean   _isNeedPolling;
    }
}