using System;
using System.Reflection;
using UnityEngine.Assertions;

namespace UIBindings
{
    public abstract class ConverterTwoWayBase<TInput, TOutput> : ConverterOneWayBase<TInput, TOutput>, IDataReadWriter<TOutput>
    {
        public override Boolean IsTwoWay => true;

        public override Type InputType => !ReverseMode ? typeof(TInput) : typeof(TOutput);
        public override Type OutputType => !ReverseMode ? typeof(TOutput) : typeof(TInput);

        private IDataReadWriter<TInput> _prev;

        public override DataProvider InitAttachToSource(  DataProvider prevConverter, Boolean isTwoWay )
        {
            prevConverter = base.InitAttachToSource( prevConverter, isTwoWay ); //May be modified
            if ( isTwoWay )
            {
                Assert.IsTrue( prevConverter.IsTwoWay );
                _prev = (IDataReadWriter<TInput>)prevConverter;
            }

            return prevConverter;
        }

        public virtual void SetValue(TOutput value )
        {
            var convertedValue = ConvertBack( value );
            _prev.SetValue( convertedValue );
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

            public override TInput Convert( TOutput value )
            {
                return _myConverter.ConvertBack( value );
            }

            public override TOutput ConvertBack( TInput value )
            {
                return _myConverter.Convert( value );
            }
        }
    }
}