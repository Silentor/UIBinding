using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Unity.Profiling;

namespace UIBindings.Adapters
{
    /// <summary>
    /// Path adapter to read some value from some source object or from previous PathAdapter.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public abstract class PathAdapterT<TSource, TValue> : PathAdapter, IDataReadWriter<TValue>
    {
        protected PathAdapterT(PathAdapter sourceAdapter, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) : base( sourceAdapter, isTwoWayBinding, notifyPropertyChanged )
        {
            _sourceAdapter = (IDataReader<TSource>)sourceAdapter;

            Assert.IsNotNull( _sourceAdapter );
        }

        protected PathAdapterT(Type sourceObjectType, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) :
                base( sourceObjectType, isTwoWayBinding, notifyPropertyChanged )
        {
        }

        public override Type InputType => typeof(TSource);
        public override Type OutputType => typeof(TValue);

        /// <summary>
        /// Read value from source object or from previous PathAdapter.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public EResult TryGetValue(out TValue value )
        {
            ReadPropertyMarker.Begin( ParameterTypes );

            TSource sourceObject;

            if ( _sourceAdapter == null )   //First adapter mode, source object is explicit
            {
                sourceObject = _sourceObject;
            }
            else    // Intermediate adapter mode, source object is read from previous adapter
            {
                var sourceChangedStatus = _sourceAdapter.TryGetValue( out sourceObject );
            
                // If source is changed, handle INotifyPropertyChanged subscription
                if ( sourceChangedStatus != EResult.NotChanged && IsSubscribed )
                {
                    if( _sourceObject is INotifyPropertyChanged oldNotifySourceObject )
                    {
                        oldNotifySourceObject.PropertyChanged -= DoSourcePropertyChangedDelegate; 
                    }
                    _sourceObject = sourceObject;
                    if ( sourceObject is INotifyPropertyChanged newNotifySourceObject )
                    {
                        newNotifySourceObject.PropertyChanged += DoSourcePropertyChangedDelegate; 
                        IsSupportNotifySelf = true;
                    }
                    else
                        IsSupportNotifySelf = false;
                }
            }
            
            var newValue = sourceObject != null ? GetValue( sourceObject ) : default;
            if( !IsValueInited || !EqualityComparer<TValue>.Default.Equals( newValue, _lastValue ) )        //TODO Check performance of EqualityComparer, consider using custom property adapter for some primitive types
            {
                _lastValue = newValue;
                IsValueInited  = true;
                value      = newValue;
                ReadPropertyMarker.End();
                return EResult.Changed;
            }

            value = newValue;
            ReadPropertyMarker.End();
            return EResult.NotChanged;
        }

        public override EResult TryGetValue(out object value )
        {
            var result = TryGetValue( out TValue typedValue );
            value = typedValue;
            return result;
        }

        public void SetValue( TValue value )
        {
            Assert.IsTrue( IsTwoWay );

            WritePropertyMarker.Begin( ParameterTypes );

            TSource sourceObject;
            if ( _sourceAdapter == null )   //First adapter mode, source object is explicit
            {
                sourceObject = _sourceObject;
            }
            else    // Intermediate adapter mode, source object is read from previous adapter
            {
                _ = _sourceAdapter.TryGetValue( out sourceObject );
            }

            SetValue( sourceObject, value );

            WritePropertyMarker.End();
        }

        public override string ToString( ) => $"{GetType().Name}{ParameterTypes} (IsTwoWay: {IsTwoWay})";

        // Typed source adapter (base.SourceAdapter is untyped)
        private readonly IDataReader<TSource> _sourceAdapter;
        private readonly IDataReadWriter<TSource> _targetToSourceAdapter;

        // Typed source object (base.SourceObject is untyped). Also used to cache intermediate source object for intermediate adapter.
        private TSource _sourceObject;

        // To detect value changes
        private TValue  _lastValue;

        private static readonly string ParameterTypes = $"<{typeof(TSource).Name},{typeof(TValue).Name}>";

        protected abstract TValue GetValue( TSource sourceObject);
        protected abstract void SetValue( TSource sourceObject, TValue value );

        protected override void OnSetSourceObject( object sourceObject )
        {
            base.OnSetSourceObject( sourceObject );
            _sourceObject = (TSource)sourceObject;
        }

        protected override void OnUnsubscribed( )
        {
            base.OnUnsubscribed();

            // Unsubscribe from INotifyPropertyChanged of intermediate source object
            if( _sourceAdapter != null && _sourceObject is INotifyPropertyChanged notifySourceObject )                
                notifySourceObject.PropertyChanged -= DoSourcePropertyChangedDelegate;
        }
    }
}