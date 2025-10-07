using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = System.Object;
using Unity.Profiling;
using UnityEngine.Assertions;

namespace UIBindings
{
    [Serializable]
    public class ValueBindingRW<T> : ValueBinding<T>
    {
        public override Boolean IsTwoWay => !_forceOneWay;

        public void SetValue( T value )
        {
            if( !Enabled || !_isInited || !_isSubscribed || SourceObject == null )
            {
                return;
            }

            if( !IsTwoWay )
            {
                Debug.LogError( $"Trying to set value to one-way binding {this}. Ignored", _debugHost );
                return;
            }

            if ( !_isValueInitialized || !EqualityComparer<T>.Default.Equals( value, _lastValue ) )
            {
                _isValueInitialized = true;
                _lastValue = value;

                if( _lastConverterTargetToSource != null )
                {
                    WriteConvertedValueMarker.Begin( ProfilerMarkerName );
                    _lastConverterTargetToSource.SetValue( value );
                    WriteConvertedValueMarker.End();
                }
                else
                {
                    WriteDirectValueMarker.Begin( ProfilerMarkerName );
                    _directSetter( value );
                    WriteDirectValueMarker.End();
                }
            }
        }

        protected override void OnInitInfrastructure( Object source, DataProvider lastConverter, bool forceOneWay, MonoBehaviour debugHost )
        {
            base.OnInitInfrastructure( source, lastConverter, forceOneWay, debugHost );

            if( forceOneWay )
            {
                //Do not init two-way binding if it is forced to be one-way.
                _forceOneWay = true;
                return;
            }

            if( lastConverter is IDataReadWriter<T> twoWayConverter )
                _lastConverterTargetToSource = twoWayConverter;
            else
                _directSetter = CreateDirectSetter( source, _directPropertyInfo );
        }

        protected override void OnSetSourceObject( object oldValue, Object value )
        {
            base.OnSetSourceObject( oldValue, value );

            if(!_forceOneWay)
            {
                _directSetter = CreateDirectSetter( value, _directPropertyInfo );
            }

            _isValueInitialized = false;
        }

        private Action<T> CreateDirectSetter( object sourceObject, PropertyInfo propertyInfo )
        {
            if ( sourceObject != null )
                return (Action<T>)Delegate.CreateDelegate( typeof(Action<T>), sourceObject, propertyInfo.GetSetMethod( true ) );
            else
                return DefaultSetter;
        }

        private Action<T>           _directSetter;
        private IDataReadWriter<T>  _lastConverterTargetToSource;
        private Boolean             _forceOneWay;
        private static readonly Action<T> DefaultSetter =  _  => { };
    }
}