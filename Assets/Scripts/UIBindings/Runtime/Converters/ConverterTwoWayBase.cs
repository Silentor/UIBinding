using System;
using System.Reflection;
using UnityEngine.Assertions;

namespace UIBindings
{
    public abstract class ConverterTwoWayBase<TInput, TOutput> : ConverterOneWayBase<TInput, TOutput>, ITwoWayConverter<TOutput>
    {
        private ITwoWayConverter<TInput> _prev;
        private Action<TInput>  _setter;

        public override void InitAttachToSourceProperty(Object source, PropertyInfo sourceProp )
        {
            base.InitAttachToSourceProperty( source, sourceProp );

            _setter = (Action<TInput>)Delegate.CreateDelegate( typeof(Action<TInput>), source, sourceProp.GetSetMethod() );
        }

        public override void InitAttachToSourceConverter(Object prevConverter )
        {
            base.InitAttachToSourceConverter( prevConverter );
            _prev = (ITwoWayConverter<TInput>)prevConverter;
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
            return new ReverseModeWrapper( this );
        }

        public abstract TInput ConvertBack( TOutput value );


        /// <summary>
        /// Wrapper to make this two way converter to be used in reverse mode (swap input and output types)
        /// </summary>
        public class ReverseModeWrapper : ConverterTwoWayBase<TOutput, TInput>
        {
            private readonly ConverterTwoWayBase<TInput, TOutput> _myConverter;

            public ReverseModeWrapper( ConverterTwoWayBase<TInput, TOutput> myConverter )
            {
                _myConverter = myConverter;
            }

            // public override void InitAttachToSourceProperty( Object source, PropertyInfo sourceProp )
            // {
            //     _getter = (Func<TOutput>)Delegate.CreateDelegate( typeof(Func<TOutput>),     source, sourceProp.GetGetMethod() );
            //     _setter = (Action<TOutput>)Delegate.CreateDelegate( typeof(Action<TOutput>), source, sourceProp.GetSetMethod() );
            // }

            // public override void InitAttachToSourceConverter( Object prevConverter )
            // {
            //     _prev = (ITwoWayConverter<TOutput>)prevConverter;
            // }

            public override TInput Convert( TOutput value )
            {
                return _myConverter.ConvertBack( value );
            }

            public override TOutput ConvertBack( TInput value )
            {
                return _myConverter.Convert( value );
            }


            // public override void ProcessTargetToSource(TInput value )
            // {
            //     var convertedValue = ConvertBack( value );
            //
            //     if( _prev != null )
            //         _prev.ProcessTargetToSource( convertedValue );
            //     else
            //         _setter.Invoke( convertedValue );
            // }
        }
    }
}