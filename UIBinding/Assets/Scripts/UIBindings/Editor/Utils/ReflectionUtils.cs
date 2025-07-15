using System;
using System.Reflection;
using System.Threading;

namespace UIBindings.Editor.Utils
{
    public static class ReflectionUtils
    {
        // public static (Boolean isAwaitable, Boolean isCancellable) IsAwaitable( MethodInfo mi )
        // {
        //     var returnType = mi.ReturnType;
        //     _getAwaiterMethod = returnType.GetMethod( "GetAwaiter" );
        //     if ( _getAwaiterMethod != null )
        //     {
        //         var awaiterType         = _getAwaiterMethod.ReturnType;
        //         var isCompletedProperty = awaiterType.GetProperty( "IsCompleted" );
        //         var getResultMethod     = awaiterType.GetMethod( "GetResult" );
        //         if ( isCompletedProperty != null && getResultMethod != null )
        //         {
        //             _isCompletedProperty = isCompletedProperty;
        //             _getResultMethod     = getResultMethod;
        //             _isResultPresent     = getResultMethod.ReturnType != typeof( void );
        //             
        //             var isCancellable = false;
        //             var paramz        = mi.GetParameters( );
        //             if( paramz.Count( p => p.ParameterType == typeof(CancellationToken) ) == 1 )
        //             {
        //                 _cancellationTokenParamIndex = Array.FindIndex( paramz, p => p.ParameterType == typeof(CancellationToken) );
        //                 isCancellable                = true;
        //             }
        //
        //             return (true, isCancellable);
        //         }
        //     }
        //
        //     return (false, false);
        // }        
    }
}