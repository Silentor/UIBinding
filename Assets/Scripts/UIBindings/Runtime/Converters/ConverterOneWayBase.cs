using System;
using System.Reflection;
using Object = System.Object;

namespace UIBindings
{
    public abstract class ConverterOneWayBase<TInput, TOutput> : ConverterBase, IInput<TInput>
    {
        protected IInput<TOutput> _next;
        private Func<TInput> _getter;

        public abstract TOutput Convert( TInput value );

        public override void InitAttachToSourceProperty(Object source, PropertyInfo sourceProp )
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

        public override void OnSourceChange( )
        {
            var value = _getter();
            ProcessSourceToTarget( value );
        }
    }
}