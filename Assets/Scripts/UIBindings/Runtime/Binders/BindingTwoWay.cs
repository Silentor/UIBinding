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
    public class BindingTwoWay<T> : Binding<T>
    {
        //Some binders can temporarily switch to one-way mode, for example, when they are not interactable.
        public bool OverrideOneWayMode = false;

        public override Boolean IsTwoWay => !OverrideOneWayMode;

        public void SetValue( T value )
        {
            if( !Enabled || !_isValid || !_isSubscribed )
            {
                return;
            }

            if ( !_isLastValueInitialized || !EqualityComparer<T>.Default.Equals( value, _lastValue ) )
            {
                _isLastValueInitialized = true;
                _lastValue = value;

                if( _lastConverterTargetToSource != null )
                {
                    WriteConvertedValueMarker.Begin( _debugSourceBindingInfo );
                    _lastConverterTargetToSource.SetValue( value );
                    WriteConvertedValueMarker.End();
                }
                else
                {
                    WriteDirectValueMarker.Begin( _debugSourceBindingInfo );
                    _directSetter( value );
                    WriteDirectValueMarker.End();
                }
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
        }

        private Action<T>           _directSetter;
        private IDataReadWriter<T> _lastConverterTargetToSource;
    }
}