using System;

namespace UIBindings.Runtime
{
    /// <summary>
    /// To pass around any enums without boxing.
    /// </summary>
    public readonly struct StructEnum : IEquatable<StructEnum>
    {
        public readonly int  Value;
        public readonly Type EnumType;

        public StructEnum( int value, Type type )
        {
            Value    = value;
            EnumType = type;
        }

        public static explicit operator int( StructEnum value )
        {
            return value.Value;
        }

        public override string ToString()
        {
            var @enum = Enum.ToObject( EnumType, Value );
            return $"{@enum.ToString()} ({EnumType.Name})";
        }

        public bool Equals(StructEnum other)
        {
            return Value == other.Value && EnumType == other.EnumType;
        }

        public override bool Equals(object obj)
        {
            return obj is StructEnum other && Equals( other );
        }

        public override int GetHashCode( )
        {
            return HashCode.Combine( Value, EnumType );
        }

        public static bool operator ==(StructEnum left, StructEnum right)
        {
            return left.Equals( right );
        }

        public static bool operator !=(StructEnum left, StructEnum right)
        {
            return !left.Equals( right );
        }
    }
}