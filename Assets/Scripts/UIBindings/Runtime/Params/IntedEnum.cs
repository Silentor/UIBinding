using System;

namespace UIBindings.Runtime
{
    public readonly struct IntedEnum : IEquatable<IntedEnum>
    {
        public readonly int  Value;
        public readonly Type EnumType;

        public IntedEnum( int value, Type type )
        {
            Value    = value;
            EnumType = type;
        }

        public static explicit operator int( IntedEnum value )
        {
            return value.Value;
        }

        public override string ToString()
        {
            var @enum = Enum.ToObject( EnumType, Value );
            return @enum.ToString();
        }

        public bool Equals(IntedEnum other)
        {
            return Value == other.Value && EnumType.Equals( other.EnumType );
        }

        public override bool Equals(object obj)
        {
            return obj is IntedEnum other && Equals( other );
        }

        public override int GetHashCode( )
        {
            return HashCode.Combine( Value, EnumType );
        }

        public static bool operator ==(IntedEnum left, IntedEnum right)
        {
            return left.Equals( right );
        }

        public static bool operator !=(IntedEnum left, IntedEnum right)
        {
            return !left.Equals( right );
        }
    }
}