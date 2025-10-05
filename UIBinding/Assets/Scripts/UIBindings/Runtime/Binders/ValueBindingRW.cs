using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;
using Unity.Profiling;

namespace UIBindings
{
    [Serializable]
    public class ValueBindingRW<T> : ValueBinding<T>
    {
        public override Boolean IsTwoWay => !_forceOneWay;

        public void SetValue( T value )
        {
            if( !Enabled || !_isInited || !_isSubscribed )
            {
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
            // else
            //     _directSetter = (Action<T>)Delegate.CreateDelegate( typeof(Action<T>), source, property.GetSetMethod( true ) );
        }

        private Action<T>           _directSetter;
        private IDataReadWriter<T>  _lastConverterTargetToSource;
        private Boolean             _forceOneWay;
    }
}