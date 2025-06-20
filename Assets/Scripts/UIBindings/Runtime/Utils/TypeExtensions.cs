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

        public static bool IsNotAssigned( this object plainOrUnityObject )
        {
            if ( plainOrUnityObject is UnityEngine.Object unityObject )
                return !unityObject;

            if ( ReferenceEquals( plainOrUnityObject, null ) )
                return true;

            return false;
        }

        public static string GetPrettyName( this Type type )
        {
            if ( type == null )
                return "null";

            if ( type.IsGenericType )
            {
                var genericTypeName = type.GetGenericTypeDefinition().Name[ ..^2 ];
                var genericArgs = string.Join( ", ", Array.ConvertAll( type.GetGenericArguments(), t => t.GetPrettyName() ) );
                return $"{genericTypeName}<{genericArgs}>";
            }

            return type.Name;
        }
    }
}