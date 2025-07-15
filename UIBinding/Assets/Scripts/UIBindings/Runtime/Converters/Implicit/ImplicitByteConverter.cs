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

        public EResult TryGetValue(out Int32 value )
        {
            var result = _reader.TryGetValue( out var sourceValue );
            value = result != EResult.NotChanged ? sourceValue : 0;
            return result;
        }

        public EResult TryGetValue(out Single value )
        {
            var result = _reader.TryGetValue( out var sourceValue );
            value = result != EResult.NotChanged ? sourceValue : 0;
            return result;
        }

        public EResult TryGetValue(out Int64 value )
        {            
            var result = _reader.TryGetValue( out var sourceValue );
            value = result != EResult.NotChanged ? sourceValue : 0;
            return result;
        }

        public EResult TryGetValue(out Double value )
        {
            var result = _reader.TryGetValue( out var sourceValue );
            value = result != EResult.NotChanged ? sourceValue : 0;
            return result;
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