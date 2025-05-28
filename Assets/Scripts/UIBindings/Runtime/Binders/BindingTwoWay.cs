using System;
using System.Reflection;
using UnityEngine;
using Object = System.Object;
using Unity.Profiling;
using Unity.Profiling.LowLevel;

namespace UIBindings
{
    [Serializable]
    public class BindingTwoWay<T> : Binding<T>
    {
        public override Boolean IsTwoWay => true;

        public void SetValue( T value )
        {
            if( !Enabled || !_isValid || !_isSubscribed )
            {
                return;
            }

            if( _lastConverterTargetToSource != null )
            {
                WriteConvertedValueMarker.Begin( _debugBindingProfileMarkerName );
                _lastConverterTargetToSource.SetValue( value );
                WriteConvertedValueMarker.End();
            }
            else
            {
                WriteDirectValueMarker.Begin( _debugBindingProfileMarkerName );
                _directSetter( value );
                WriteDirectValueMarker.End();
            }
        }

        protected override void DoAwake( Object source, PropertyInfo property, DataProvider lastConverter, MonoBehaviour debugHost )
        {
            base.DoAwake( source, property, lastConverter, debugHost );

            if ( !property.CanWrite )
            {
                Debug.LogError($"[{nameof(Binding)}] Property {property.DeclaringType.Name}.{property.Name} is read-only and cannot be used for two-way binding.", debugHost);
                return;
            }

            if( lastConverter is IDataReadWriter<T> twoWayConverter )
                _lastConverterTargetToSource = twoWayConverter;
            else
                _directSetter = (Action<T>)Delegate.CreateDelegate( typeof(Action<T>), source, property.GetSetMethod( true ) );

            _debugBindingProfileMarkerName = $"{Source.name}.{Path} <-> {debugHost.name}";
        }

        private Action<T>           _directSetter;
        private IDataReadWriter<T> _lastConverterTargetToSource;
    }
}