using System;
using Silentor.Runtime.Utils;

namespace UIBindings.Converters
{
    public class ImplicitIntConverter : DataProvider, IDataReadWriter<byte>, IDataReadWriter<float>, IDataReadWriter<long>, IDataReadWriter<double>
    {
        public override Boolean IsTwoWay  => true;
        public override Type    InputType => typeof(int);

        private readonly IDataReader<int> _reader;
        private readonly IDataReadWriter<int> _writer;

        public ImplicitIntConverter( DataProvider source )
        {
            _reader = (IDataReader<int>)source;
            _writer = source as IDataReadWriter<int>;
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

        public void SetValue(Byte value )
        {
            _writer.SetValue( value );
        }

        public void SetValue(Single value )
        {
            _writer.SetValue( value.ClampToInt32() );
        }

        public void SetValue(Int64 value )
        {
            _writer.SetValue( value.ClampToInt32() );
        }

        public void SetValue(Double value )
        {
            _writer.SetValue( value.ClampToInt32() );
        }

    }
}