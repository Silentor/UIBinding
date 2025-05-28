using System;
using JetBrains.Annotations;
using UIBindings.Converters;
using Object = System.Object;

namespace UIBindings
{
    public abstract class ConverterOneWayBase<TInput, TOutput> : ConverterBase, IDataReader<TOutput>
    {
        public override Boolean IsTwoWay => false;

        public override Type InputType  => typeof(TInput);

        public override Type OutputType => typeof(TOutput);

        private IDataReader<TInput> _prev;
        //private TInput _lastValue;
        //private Boolean _isLastValueInited;

        public abstract TOutput Convert( TInput value );

        //Returns false if attach to source failed, probably incompatible types
        public override bool InitAttachToSource(  [NotNull] DataProvider prevConverter, Boolean isTwoWay )
        {
            if ( prevConverter == null ) throw new ArgumentNullException( nameof(prevConverter) );
            if ( prevConverter is IDataReader<TInput> properSource )
            {
                _prev =  properSource;
                OnAttachToSource( prevConverter, isTwoWay );
            }
            else
            {
                var implicitTypeConverter = ImplicitConversion.GetConverter( prevConverter, typeof(TInput) );
                if ( implicitTypeConverter != null )
                {
                    _prev = (IDataReader<TInput>)implicitTypeConverter;
                    OnAttachToSource( implicitTypeConverter, isTwoWay );
                }
            }

            return _prev != null;
        }

        public override ConverterBase GetReverseConverter( )
        {
            throw new NotImplementedException( "Not supported for one way converters" );
        }

        public virtual Boolean TryGetValue( out TOutput value )
        {
            if( _prev.TryGetValue( out var prevValue ))
            {
                value = Convert( prevValue );
                // if( !_isLastValueInited || !EqualityComparer<TOutput>.Default.Equals( value, _lastValue ) )
                // {
                //     _lastValue         = value;
                //     _isLastValueInited = true;
                // }
                return true;
            }

            value = default;
            return false;
        }

        protected virtual void OnAttachToSource( DataProvider prevConverter, Boolean isTwoWay ) { }
    }
}