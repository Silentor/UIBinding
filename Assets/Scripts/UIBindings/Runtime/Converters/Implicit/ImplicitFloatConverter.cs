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

        public EResult TryGetValue(out Int64 value )
        {            
            var result = _reader.TryGetValue( out var sourceValue );
            value = result != EResult.NotChanged ? sourceValue.ClampToInt64() : 0;
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