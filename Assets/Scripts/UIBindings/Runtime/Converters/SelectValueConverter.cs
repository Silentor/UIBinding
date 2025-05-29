 using System;
 using UIBindings.Runtime.Types;
 using UnityEngine;

namespace UIBindings
{
    public abstract class SelectValueConverter<TSerializable> : SimpleConverterOneWayBase<int, TSerializable>
    {
        public KeyValue<TSerializable>[] Values;
        public TSerializable DefaultValue;

        public override Type InputType  => typeof(int);
        public override Type OutputType => typeof(TSerializable);

        public override TSerializable Convert(int value )
        {
            foreach ( var keyValue in Values )
            {
                if( keyValue.Key == value )
                {
                    return keyValue.Value;
                }
            }

            return DefaultValue;
        }
    }

    public class SelectColorConverter : SelectValueConverter<Color>
    {
    }

    public class SelectSpriteConverter : SelectValueConverter<Sprite>
    {
    }

    public class SelectStringConverter : SelectValueConverter<String>
    {
    }

    public class SelectVector3Converter : SelectValueConverter<Vector3>
    {
    }

}