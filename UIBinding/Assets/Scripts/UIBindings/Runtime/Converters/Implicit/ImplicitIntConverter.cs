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

        public EResult TryGetValue(out Byte value )
        {
            var result = _reader.TryGetValue( out var sourceValue );
            value = result != EResult.NotChanged ? sourceValue.ClampToByte() : (Byte)0;
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