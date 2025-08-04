using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = System.Object;
using Unity.Profiling;
using Unity.Profiling.LowLevel;

namespace UIBindings
{
    [Serializable]
    public class ValueBindingRW<T> : ValueBinding<T>
    {
        public override Boolean IsTwoWay => !_forceOneWay;

        public void SetValue( T value )
        {
            if( !Enabled || !_isValid || !_isSubscribed )
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

        protected override void OoInitInfrastructure( Object source, PropertyInfo property, DataProvider lastConverter, bool forceOneWay, MonoBehaviour debugHost )
        {
            base.OoInitInfrastructure( source, property, lastConverter, forceOneWay, debugHost );

            if( forceOneWay )
            {
                //Do not init two-way binding if it is forced to be one-way.
                _forceOneWay = true;
                return;
            }

            if ( !property.CanWrite )
            {
                Debug.LogError($"[{nameof(BindingBase)}] Property {property.DeclaringType.Name}.{property.Name} is read-only and cannot be used for two-way binding.", debugHost);
                return;
            }

            if( lastConverter is IDataReadWriter<T> twoWayConverter )
                _lastConverterTargetToSource = twoWayConverter;
            else
                _directSetter = (Action<T>)Delegate.CreateDelegate( typeof(Action<T>), source, property.GetSetMethod( true ) );
        }

        private Action<T>           _directSetter;
        private IDataReadWriter<T>  _lastConverterTargetToSource;
        private Boolean             _forceOneWay;
    }
}