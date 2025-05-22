using System;
using System.Reflection;
using UnityEngine.Assertions;

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

        public virtual void ProcessTargetToSource(TOutput value )
        {
            var convertedValue = ConvertBack( value );

            if( _prev != null )
                _prev.ProcessTargetToSource( convertedValue );
            else
                _setter.Invoke( convertedValue );
        }

        public override ConverterBase GetReverseConverter( )
        {
            return new ReverseMode( this );
        }

        public abstract TInput ConvertBack( TOutput value );


        /// <summary>
        /// Wrapper to make this two way converter to be used in reverse mode (swap input and output types)
        /// </summary>
        public class ReverseMode : ConverterTwoWayBase<TOutput, TInput>
        {
            private readonly ConverterTwoWayBase<TInput, TOutput> _myConverter;

            public ReverseMode( ConverterTwoWayBase<TInput, TOutput> myConverter )
            {
                _myConverter = myConverter;
            }

            public override void InitAttachToSourceProperty( Object source, PropertyInfo sourceProp )
            {
                _getter = (Func<TOutput>)Delegate.CreateDelegate( typeof(Func<TOutput>),     source, sourceProp.GetGetMethod() );
                _setter = (Action<TOutput>)Delegate.CreateDelegate( typeof(Action<TOutput>), source, sourceProp.GetSetMethod() );
            }

            public override void InitSourceToTarget( Object nextConverter )
            {
                _next = (IInput<TInput>)nextConverter;
            }

            public override void InitTargetToSource( Object prevConverter )
            {
                _prev = (IOutput<TOutput>)prevConverter;
            }

            public override TInput Convert( TOutput value )
            {
                return _myConverter.ConvertBack( value );
            }

            public override TOutput ConvertBack( TInput value )
            {
                return _myConverter.Convert( value );
            }

            public override void ProcessSourceToTarget( TOutput value )
            {
                var convertedValue = Convert( value );
                _next.ProcessSourceToTarget( convertedValue );
            }

            public override void ProcessTargetToSource(TInput value )
            {
                var convertedValue = ConvertBack( value );

                if( _prev != null )
                    _prev.ProcessTargetToSource( convertedValue );
                else
                    _setter.Invoke( convertedValue );
            }

            public override void OnSourcePropertyChanged( )
            {
                Assert.IsTrue( _getter != null, $"Converter is not connected to source property" );
                var value = _getter();
                ProcessSourceToTarget( value );
            }
        }
    }
}