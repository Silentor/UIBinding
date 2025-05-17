using System;
using System.Reflection;
using UnityEngine;
using Object = System.Object;

namespace UIBindings
{
    public abstract class ConverterBase : MonoBehaviour
    {
        //For first converter in chain (attached to source property)
        public abstract void InitAttachToSource( System.Object source, PropertyInfo sourceProp );
        
        //For all other converters in chain
        public abstract void InitSourceToTarget( Object nextConverter );

        //For all other converters in chain to reverse the conversion (If it supports reverse conversion)
        public abstract void InitTargetToSource( Object prevConverter );

        public abstract void OnChange( );
    }

    public abstract class ConverterOneWayBase<TInput, TOutput> : ConverterBase, IInput<TInput>
    {
        protected IInput<TOutput> _next;
        private Func<TInput> _getter;
        private TInput _lastValue;
        

        public abstract TOutput Convert( TInput value );

        public override void InitAttachToSource(Object source, PropertyInfo sourceProp )
        {
            _getter = (Func<TInput>)Delegate.CreateDelegate( typeof(Func<TInput>), source, sourceProp.GetGetMethod() );
        }

        public override void InitSourceToTarget(Object nextConverter )
        {
            _next = (IInput<TOutput>) nextConverter;
        }

        public override void InitTargetToSource(Object prevConverter )
        {
            throw new NotImplementedException( "Not supported for one way converters" );
        }

        public void ProcessSourceToTarget(TInput value )
        {
            var convertedValue = Convert( value );
            _next.ProcessSourceToTarget( convertedValue );
        }

        public override void OnChange( )
        {
            var value = _getter();
            ProcessSourceToTarget( value );
        }
    }
}