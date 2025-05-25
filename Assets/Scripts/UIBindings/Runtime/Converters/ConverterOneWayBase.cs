using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public abstract class ConverterOneWayBase<TInput, TOutput> : ConverterBase, IOneWayConverter<TOutput>
    {
        private IOneWayConverter<TInput> _prev;
        protected Func<TInput> _getter;
        private TInput _lastValue;
        private Boolean _isLastValueInited;

        public abstract TOutput Convert( TInput value );

        public override void InitAttachToSourceProperty(Object source, PropertyInfo sourceProp )
        {
            _getter = (Func<TInput>)Delegate.CreateDelegate( typeof(Func<TInput>), source, sourceProp.GetGetMethod() );
        }

        public override void InitAttachToSourceConverter(Object prevConverter )
        {
            _prev = (IOneWayConverter<TInput>) prevConverter;
        }

        public override ConverterBase GetReverseConverter( )
        {
            throw new NotImplementedException( "Not supported for one way converters" );
        }

        public virtual Boolean TryGetValueFromSource( out TOutput value )
        {
            if ( _prev != null )
            {
                if( _prev.TryGetValueFromSource( out var prevValue ))
                {
                    value = Convert( prevValue );
                    return true;
                }
            }
            else
            {
                var sourceValue = _getter.Invoke();
                if( !_isLastValueInited || !EqualityComparer<TInput>.Default.Equals( sourceValue, _lastValue ) )
                {
                    _lastValue         = sourceValue;
                    _isLastValueInited = true;
                    value              = Convert( sourceValue );
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}