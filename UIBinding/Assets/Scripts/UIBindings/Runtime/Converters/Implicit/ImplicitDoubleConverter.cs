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

        public EResult TryGetValue(out Single value )
        {
            var result = _reader.TryGetValue( out var sourceValue );
            value = result != EResult.NotChanged ? sourceValue.ClampToFloat() : 0;
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

        public void SetValue(Single value )
        {
            _writer.SetValue( value );
        }

    }
}