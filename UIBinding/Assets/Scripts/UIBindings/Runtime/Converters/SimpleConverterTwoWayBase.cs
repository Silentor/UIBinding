using System;
using System.Reflection;
using UnityEngine.Assertions;

namespace UIBindings
{
    /// <summary>
    /// Just implement Convert and ConvertBack methods to create a simple two-way converter. Its support automatic reverse mode, so it can swap input and output types.
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    public abstract class SimpleConverterTwoWayBase<TInput, TOutput> : SimpleConverterOneWayBase<TInput, TOutput>, IDataReadWriter<TOutput>
    {
        public override Boolean IsTwoWay => true;

        private IDataReadWriter<TInput> _prevWriter;

        public virtual void SetValue(TOutput value )
        {
            var convertedValue = ConvertBack( value );
            _prevWriter.SetValue( convertedValue );
        }

        public abstract TInput ConvertBack( TOutput value );

        protected override void OnAttachToSource(DataProvider prevConverter, Boolean isTwoWay )
        {
            if ( isTwoWay )
            {
                Assert.IsTrue( prevConverter.IsTwoWay );
                _prevWriter = (IDataReadWriter<TInput>)prevConverter;
            }
        }

        /// <summary>
        /// Wrapper to make this two way converter to be used in reverse mode (swap input and output types)
        /// </summary>
        // public class ReverseModeWrapper : SimpleConverterTwoWayBase<TOutput, TInput>
        // {
        //     private readonly SimpleConverterTwoWayBase<TInput, TOutput> _myConverter;
        //
        //     public ReverseModeWrapper( SimpleConverterTwoWayBase<TInput, TOutput> myConverter )
        //     {
        //         _myConverter = myConverter;
        //     }
        //
        //     public override TInput Convert( TOutput value )
        //     {
        //         return _myConverter.ConvertBack( value );
        //     }
        //
        //     public override TOutput ConvertBack( TInput value )
        //     {
        //         return _myConverter.Convert( value );
        //     }
        //
        //     public override String ToString( )
        //     {
        //         return $"{_myConverter.GetType().Name} reverse (Input: {InputType.Name}, Output: {OutputType.Name}, IsTwoWay: {IsTwoWay})";
        //     }
        // }
    }
}