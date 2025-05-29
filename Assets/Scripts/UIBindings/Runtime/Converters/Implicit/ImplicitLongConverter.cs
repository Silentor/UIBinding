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

        public EResult TryGetValue(out Byte value )
        {
            var result = _reader.TryGetValue( out var sourceValue );
            value = result != EResult.NotChanged ? sourceValue.ClampToByte() : (Byte)0;
            return result;
        }

        public EResult TryGetValue(out Int32 value )
        {
            var result = _reader.TryGetValue( out var sourceValue );
            value = result != EResult.NotChanged ? sourceValue.ClampToInt32() : 0;
            return result;
        }

        public EResult TryGetValue(out Single value )
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

