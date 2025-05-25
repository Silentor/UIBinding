using System;
using System.Reflection;
using UnityEngine;
using Object = System.Object;

namespace UIBindings
{
    [Serializable]
    public class BindingTwoWay<T> : Binding<T>
    {
        public void SetValue( T value )
        {
            if( !_isValid || !_isSubscribed )
            {
                return;
            }

            // if ( !_isLastValueInitialized )
            // {
            //     _isLastValueInitialized = true;
            //     _lastSourceValue = value;
            // }

            if( _lastConverterTargetToSource != null )
            {
                WriteConvertedValueMarker.Begin();
                _lastConverterTargetToSource.ProcessTargetToSource( value );
                WriteConvertedValueMarker.End();
            }
            else
            {
                WriteDirectValueMarker.Begin();
                _directSetter( value );
                WriteDirectValueMarker.End();
            }
        }

        private Action<T> _directSetter;

        protected override void DoAwake(MonoBehaviour host, PropertyInfo property )
        {
            base.DoAwake( host, property );

            if ( !property.CanWrite )
            {
                Debug.LogError($"[{nameof(Binding)}] Property {property.DeclaringType.Name}.{property.Name} is read-only and cannot be used for two-way binding.", host);
                return;
            } 

            if ( Converters.Count > 0 )
            {
                _lastConverterTargetToSource = (ITwoWayConverter<T>)_lastConverter; 
            }
            else
            {
                var setMethod = property.GetSetMethod();
                _directSetter = (Action<T>)Delegate.CreateDelegate( typeof(Action<T>), Source, setMethod );
            }
        }

        private ITwoWayConverter<T> _lastConverterTargetToSource;
    }
}