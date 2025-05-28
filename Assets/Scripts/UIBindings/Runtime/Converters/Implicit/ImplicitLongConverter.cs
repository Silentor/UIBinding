using System;
using Silentor.Runtime.Utils;

namespace UIBindings.Converters
{
    public class ImplicitLongConverter : DataProvider, IDataReadWriter<byte>, IDataReadWriter<int>, IDataReadWriter<float>, IDataReadWriter<double>
    {
        public override Boolean IsTwoWay  => true;
        public override Type    InputType => typeof(long);

        private readonly IDataReader<long> _reader;
        private readonly IDataReadWriter<long> _writer;

        public ImplicitLongConverter( DataProvider source )
        {
            _reader = (IDataReader<long>)source;
            _writer = source as IDataReadWriter<long>;
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

        public Boolean TryGetValue(out Int32 value )
        {
            if ( _reader.TryGetValue( out var sourceValue ) )
            {
                value = sourceValue.ClampToInt32();
                return true;
            }

            value = default;
            return false;
        }

        public Boolean TryGetValue(out Single value )
        {
            if ( _reader.TryGetValue( out var sourceValue ) )
            {
                value = sourceValue;
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

        public void SetValue(Single value )
        {
            _writer.SetValue( (long)value );
        }

        public void SetValue(Double value )
        {
            _writer.SetValue( (long)value );
        }
    }
}

