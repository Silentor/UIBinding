using System;
using Silentor.Runtime.Utils;

namespace UIBindings.Converters
{
    public class ImplicitFloatConverter : DataProvider, IDataReadWriter<byte>, IDataReadWriter<int>, IDataReadWriter<long>, IDataReadWriter<double>
    {
        public override Boolean IsTwoWay  => true;
        public override Type    InputType => typeof(float);

        private readonly IDataReader<float> _reader;
        private readonly IDataReadWriter<float> _writer;

        public ImplicitFloatConverter( DataProvider source )
        {
            _reader = (IDataReader<float>)source;
            _writer = source as IDataReadWriter<float>;
        }

        public Boolean TryGetValue(out Byte value )
        {
            if ( _reader.TryGetValue( out var sourceValue ) )
            {
                value = sourceValue.ClampToByte();
                return true;
            }

            value = default;
            return false;
        }

        public Boolean TryGetValue(out int value )
        {
            if ( _reader.TryGetValue( out var sourceValue ) )
            {
                value = sourceValue.ClampToInt32();
                return true;
            }

            value = default;
            return false;

        }

        public Boolean TryGetValue(out Int64 value )
        {            
            if ( _reader.TryGetValue( out var sourceValue ) )
            {
                value = sourceValue.ClampToInt64();
                return true;
            }

            value = default;
            return false;

        }

        public Boolean TryGetValue(out Double value )
        {
            if ( _reader.TryGetValue( out var sourceValue ) )
            {
                value = sourceValue;
                return true;
            }

            value = default;
            return false;

        }

        public void SetValue(Byte value )
        {
            _writer.SetValue( value );
        }

        public void SetValue(Int32 value )
        {
            _writer.SetValue( value );
        }

        public void SetValue(Int64 value )
        {
            _writer.SetValue( value );
        }

        public void SetValue(Double value )
        {
            _writer.SetValue( value.ClampToFloat() );
        }

    }
}