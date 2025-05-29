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

        public EResult TryGetValue(out Byte value )
        {
            var result = _reader.TryGetValue( out var sourceValue );
            if ( result != EResult.NotChanged )
                value = sourceValue ? (Byte)1 : (Byte)0;
            else
                value = 0;
            return result;
        }

        public EResult TryGetValue(out Int32 value )
        {
            var result = _reader.TryGetValue( out var sourceValue );
            if ( result != EResult.NotChanged )
                value = sourceValue ? 1 : 0;
            else
                value = 0;
            return result;
        }

        public EResult TryGetValue(out Int64 value )
        {            
            var result = _reader.TryGetValue( out var sourceValue );
            if ( result != EResult.NotChanged )
                value = sourceValue ? 1 : 0;
            else
                value = 0;
            return result;
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