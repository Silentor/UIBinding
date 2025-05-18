using System;
using System.Reflection;

namespace UIBindings
{
    public abstract class ConverterTwoWayBase<TInput, TOutput> : ConverterOneWayBase<TInput, TOutput>, IOutput<TOutput>
    {
        private Action<TInput>  _setter;
        private IOutput<TInput> _prev;

        public override void InitTargetToSource(Object prevConverter )
        {
            //base.InitTargetToSource( prevConverter );

            _prev = (IOutput<TInput>) prevConverter;
        }

        public override void InitAttachToSourceProperty(Object source, PropertyInfo sourceProp )
        {
            base.InitAttachToSourceProperty( source, sourceProp );

            _setter = (Action<TInput>)Delegate.CreateDelegate( typeof(Action<TInput>), source, sourceProp.GetSetMethod() );
        }

        public void ProcessTargetToSource(TOutput value )
        {
            var convertedValue = ConvertBack( value );

            if( _prev != null )
                _prev.ProcessTargetToSource( convertedValue );
            else
                _setter.Invoke( convertedValue );
        }

        public abstract TInput ConvertBack( TOutput value );
    }
}