using System;

namespace UIBindings.Runtime.Utils
{
    public static class TypeExtensions
    {
        public static bool IsDerivedFrom( this Type type, Type baseType )
        {
            if ( type == null || baseType == null )
                return false;

            if ( type == baseType )
                return true;

            if ( type.IsGenericType && type.GetGenericTypeDefinition() == baseType )
                return true;

            return type.IsSubclassOf( baseType );
        }
    }
}