using System;
using Silentor.Runtime.Utils;

namespace UIBindings.Converters
{
    public class ImplicitBoolConverter : DataProvider, IDataReadWriter<byte>, IDataReadWriter<int>, IDataReadWriter<long>
    {
        public override Boolean IsTwoWay  => true;
        public override Type    InputType => typeof(bool);

        private readonly IDataReader<bool> _reader;
        private readonly IDataReadWriter<bool> _writer;

        public ImplicitBoolConverter( DataProvider source )
        {
            _reader = (IDataReader<bool>)source;
            _writer = source as IDataReadWriter<bool>;
        }

        public Boolean TryGetValue(out Byte value )
        {
            if ( _reader.TryGetValue( out var sourceValue ) )
            {
                value = sourceValue ? (Byte)1 : (Byte)0;
                return true;
            }

            value = default;
            return false;
        }

        public Boolean TryGetValue(out int value )
        {
            if ( _reader.TryGetValue( out var sourceValue ) )
            {
                value = sourceValue ? 1 : 0;
                return true;
            }

            value = default;
            return false;
        }

        public Boolean TryGetValue(out long value )
        {            
            if ( _reader.TryGetValue( out var sourceValue ) )
            {
                value = sourceValue ? 1L : 0L;
                return true;
            }

            value = default;
            return false;
        }

        public void SetValue(Byte value )
        {
            _writer.SetValue( value != 0 );
        }

        public void SetValue(int value )
        {
            _writer.SetValue( value != 0 );
        }

        public void SetValue(Int64 value )
        {
            _writer.SetValue( value != 0 );
        }
    }
}