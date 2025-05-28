using System;
using Silentor.Runtime.Utils;

namespace UIBindings.Converters
{
    public class ImplicitDoubleConverter : DataProvider, IDataReadWriter<byte>, IDataReadWriter<int>, IDataReadWriter<long>, IDataReadWriter<float>
    {
        public override Boolean IsTwoWay  => true;
        public override Type    InputType => typeof(double);

        private readonly IDataReader<double> _reader;
        private readonly IDataReadWriter<double> _writer;

        public ImplicitDoubleConverter( DataProvider source )
        {
            _reader = (IDataReader<double>)source;
            _writer = source as IDataReadWriter<double>;
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

        public Boolean TryGetValue(out float value )
        {
            if ( _reader.TryGetValue( out var sourceValue ) )
            {
                value = sourceValue.ClampToFloat();
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

        public void SetValue(Single value )
        {
            _writer.SetValue( value );
        }

    }
}