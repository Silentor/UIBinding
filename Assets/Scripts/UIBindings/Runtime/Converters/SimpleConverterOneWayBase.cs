using System;

namespace UIBindings
{
    /// <summary>
    /// Overriders just need to implement Convert method for one way conversion.
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    public abstract class SimpleConverterOneWayBase<TInput, TOutput> : ConverterBase<TInput, TOutput>, IDataReader<TOutput>
    {
        public override Boolean IsTwoWay => false;

        public abstract TOutput Convert( TInput value );

        public override ConverterBase GetReverseConverter( )
        {
            throw new NotImplementedException( "Not supported for one way converters" );
        }

        public virtual EResult TryGetValue( out TOutput value )
        {
            var result = _prev.TryGetValue( out var prevValue );
            value = result != EResult.NotChanged ? Convert( prevValue ) : default; 
            return result;
        }
    }
}