using System;
using System.Reflection;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public abstract class ConverterOneWayBase<TInput, TOutput> : ConverterBase, IOneWayConverter<TOutput>
    {
        private IOneWayConverter<TInput> _prev;
        protected Func<TInput> _getter;

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

        public virtual TOutput GetValueFromSource( )
        {
            if ( _prev != null )
            {
                var value = _prev.GetValueFromSource();
                return Convert( value );
            }
            else
            {
                var value = _getter.Invoke();
                return Convert( value );
            }
        }
    }
}