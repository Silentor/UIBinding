using System;
using Silentor.Runtime.Utils;

namespace UIBindings.Converters
{
    public class ImplicitByteConverter : DataProvider, IDataReadWriter<int>, IDataReadWriter<float>, IDataReadWriter<long>, IDataReadWriter<double>
    {
        public override Boolean IsTwoWay  => true;
        public override Type    InputType => typeof(byte);

        private readonly IDataReader<byte> _reader;
        private readonly IDataReadWriter<byte> _writer;

        public ImplicitByteConverter( DataProvider source )
        {
            _reader = (IDataReader<byte>)source;
            _writer = source as IDataReadWriter<byte>;
        }

        public Boolean TryGetValue(out Int32 value )
        {
            if ( _reader.TryGetValue( out var sourceValue ) )
            {
                value = sourceValue;
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

        public Boolean TryGetValue(out Int64 value )
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

        public void SetValue(Int32 value )
        {
            _writer.SetValue( value.ClampToByte() );
        }

        public void SetValue(Single value )
        {
            _writer.SetValue( value.ClampToByte() );
        }

        public void SetValue(Int64 value )
        {
            _writer.SetValue( value.ClampToByte() );
        }

        public void SetValue(Double value )
        {
            _writer.SetValue( value.ClampToByte() );
        }

    }
}