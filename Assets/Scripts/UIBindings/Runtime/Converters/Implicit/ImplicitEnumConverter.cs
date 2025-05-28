using System;
using Silentor.Runtime.Utils;
using UIBindings.Runtime;
using UnityEngine.Assertions;

namespace UIBindings.Converters
{
    public class ImplicitEnumConverter : DataProvider, IDataReadWriter<int>, IDataReadWriter<byte>, IDataReadWriter<long>
    {
        public override Boolean IsTwoWay  => false;
        public override Type    InputType => typeof(StructEnum);

        private readonly IDataReader<StructEnum> _reader;
        private readonly IDataReadWriter<StructEnum> _writer;

        private Type _enumType;     //Need at least one read to discover enum type, we don't know it at the start. Before first read, writes will be unsuccessful.

        public ImplicitEnumConverter( DataProvider source )
        {
            _reader = (IDataReader<StructEnum>)source;
            _writer = source as IDataReadWriter<StructEnum>;
        }

        public Boolean TryGetValue(out Int32 value )
        {
            if ( _reader.TryGetValue( out var sourceValue ) )
            {
                _enumType = sourceValue.EnumType;
                value = sourceValue.Value;
                return true;
            }

            value = default;
            return false;
        }

        public Boolean TryGetValue(out Byte value )
        {
            if ( _reader.TryGetValue( out var sourceValue ) )
            {
                _enumType = sourceValue.EnumType;
                value = sourceValue.Value.ClampToByte();
                return true;
            }

            value = default;
            return false;
        }

        public Boolean TryGetValue(out Int64 value )
        {            
            if ( _reader.TryGetValue( out var sourceValue ) )
            {
                _enumType = sourceValue.EnumType;
                value = sourceValue.Value;
                return true;
            }

            value = default;
            return false;
        }

        public void SetValue(Int32 value )
        {
            Assert.IsNotNull( _enumType, "Enum type is not set. Make sure to read value before writing." );
            _writer.SetValue( new StructEnum(value, _enumType) );
        }

        public void SetValue(Byte value )
        {
            Assert.IsNotNull( _enumType, "Enum type is not set. Make sure to read value before writing." );
            _writer.SetValue( new StructEnum(value, _enumType) );
        }

        public void SetValue(Int64 value )
        {
            Assert.IsNotNull( _enumType, "Enum type is not set. Make sure to read value before writing." );
            _writer.SetValue( new StructEnum(value.ClampToInt32(), _enumType) );
        }
    }
}