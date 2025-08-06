using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Unity.Profiling;
using UnityEngine.Assertions;

namespace UIBindings.Adapters
{
    /// <summary>
    /// Read/write property of some source object or from result of another PropertyAdapter. Source object can be swapped
    /// </summary>
    public class PropertyAdapter<TSource, TProperty> : PropertyAdapter, IDataReadWriter<TProperty>
    {
        public override Type InputType  => typeof(TSource);
        public override Type OutputType  => typeof(TProperty);

        //First property adapter mode
        public PropertyAdapter( [NotNull] object source, [NotNull] PropertyInfo propertyInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) 
                : this( propertyInfo, isTwoWayBinding, notifyPropertyChanged )
        {
            Assert.IsNotNull( source );
            ChangeSourceObject( source );
        }

        //Next property adapter mode
        public PropertyAdapter( [NotNull] PropertyAdapter source, [NotNull] PropertyInfo propertyInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) 
                : this( propertyInfo, isTwoWayBinding, notifyPropertyChanged )
        {
            Assert.IsNotNull( source );
            _sourcePropertyAdapter = (IDataReadWriter<TSource>)source;
        }

        private PropertyAdapter( [NotNull] PropertyInfo propertyInfo, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) : base(
                propertyInfo, isTwoWayBinding, notifyPropertyChanged )
        {
            Assert.IsTrue( propertyInfo.PropertyType  == typeof(TProperty) );
            Assert.IsTrue( propertyInfo.DeclaringType == typeof(TSource) );

            _getter = (Func<TSource, TProperty>)Delegate.CreateDelegate( typeof(Func<TSource, TProperty>), propertyInfo.GetGetMethod( true ) );
            _onSourcePropertyChangedDelegate = OnSourcePropertyChanged;

            // Is binding one-way we dont want to spent time on creating setter delegate
            if( isTwoWayBinding )
                _setter = (Action<TSource, TProperty>)Delegate.CreateDelegate( typeof(Action<TSource, TProperty>), propertyInfo.GetSetMethod( true ) );
        }

        public override void ChangeSourceObject( object newSourceObject )
        {
            Assert.IsNotNull( newSourceObject );
            Assert.IsNull( _sourcePropertyAdapter );
        
            var newSourceObjectTyped = (TSource)newSourceObject;
            if ( EqualityComparer<TSource>.Default.Equals( _lastSourceObject, newSourceObjectTyped )  )
                return;

            ChangeSourceObject( newSourceObjectTyped );
            _isInited = false;      //If manually changed source, probably need update value asap
        }

        public EResult TryGetValue(out TProperty value )
        {
            ReadPropertyMarker.Begin( NameofTypes );

            TSource sourceObject;

            if ( _sourcePropertyAdapter == null )   //First property adapter mode
            {
                sourceObject = _lastSourceObject;
            }
            else
            {
                var sourceChangedStatus = _sourcePropertyAdapter.TryGetValue( out sourceObject );
            
                //Handle INotifyPropertyChanged on source object
                if ( sourceChangedStatus != EResult.NotChanged && IsSubscribed )
                {
                    ChangeSourceObject( sourceObject );
                }
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

            if ( _sourcePropertyAdapter == null )
            {
                _setter( _lastSourceObject, value );
            }
            else
            {
                _ = _sourcePropertyAdapter.TryGetValue( out var sourceObject );
                //if( !_isInited || !EqualityComparer<TProperty>.Default.Equals( value, _lastValue ) ) todo consider write value optimization
                {
                    //_lastValue = value;
                    //_isInited  = true;
                    _setter( sourceObject, value );
                }
            }

            WritePropertyMarker.End();
        }

        public override void Subscribe( )
        {
            base.Subscribe();

            if ( _sourcePropertyAdapter == null )
            {
                if( _lastSourceObject is INotifyPropertyChanged notifySourceObject )
                {
                    notifySourceObject.PropertyChanged += _onSourcePropertyChangedDelegate;
                    _isNeedPolling = false;
                }
                else
                    _isNeedPolling = true; 
            }
            else
            {
                //Do not subscribe right now, actually subscribe at TryGetValue because we do not want read source object before planned update
                ((PropertyAdapter)_sourcePropertyAdapter).Subscribe( );
            }
        }

        public override void Unsubscribe( )
        {
            base.Unsubscribe();

            if ( _lastSourceObject is INotifyPropertyChanged sourceObjectWasNotifyable )
            {
                sourceObjectWasNotifyable.PropertyChanged -= _onSourcePropertyChangedDelegate;
                _lastSourceObject = default; 
            }

            if( _sourcePropertyAdapter != null )
                ((PropertyAdapter)_sourcePropertyAdapter).Unsubscribe( );
        }

        public override Boolean IsNeedPolling( )
        {
            return _sourcePropertyAdapter == null 
                ? _isNeedPolling 
                : ((PropertyAdapter)_sourcePropertyAdapter).IsNeedPolling( ) || _isNeedPolling;
        }

        public override String ToString( )
        {
            return  $"{nameof(PropertyAdapter<TSource, TProperty>)}{NameofTypes}.{_getter.Method.Name}";
        }

        private static readonly string NameofTypes = $"<{typeof(TSource).Name},{typeof(TProperty).Name}>";

        private void OnSourcePropertyChanged( object sender, String propertyName )
        {
            if( String.IsNullOrEmpty( propertyName ) || propertyName == PropertyName )
            {
                NotifyPropertyChanged?.Invoke( sender, propertyName );
            }
        }

        //Handle subscription for INotifyPropertyChanged of source object (if supported)
        private void ChangeSourceObject( TSource newSourceObject )
        {
            if ( _lastSourceObject is INotifyPropertyChanged oldNotifyChanged )
            {
                oldNotifyChanged.PropertyChanged -= _onSourcePropertyChangedDelegate;
            }
        
            if( newSourceObject is INotifyPropertyChanged newNotifyChanged && IsSubscribed )
            {
                newNotifyChanged.PropertyChanged += _onSourcePropertyChangedDelegate;
                _isNeedPolling                   =  false; //If we have INotifyPropertyChanged we dont need polling
            }
            else
                _isNeedPolling = true;

            _lastSourceObject = newSourceObject;
        }

        //First property adapter mode
        private TSource _sourceObject;

        //Next property adapter mode
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