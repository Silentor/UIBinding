using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using UIBindings.Runtime;
using UIBindings.Runtime.Utils;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Object = System.Object;

#if UIBINDINGS_UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace UIBindings
{
    [Serializable]
    public class CallBinding : Binding
    {
        public SerializableParam[] Params; 

        //Async call settings
        public Boolean DisableButtonDuringAsyncCall = true;

        //Optimized delegates for simple cases lives here
        private Delegate _delegateCall;

        //All unoptimized calls uses reflection
        private MethodInfo                      _callReflection;

        //private AwaitableAction                 _callAwaitable;

        private EParamsType _paramsType;
        private EAwaitableType _awaitType;

        private Boolean _isValid;
        private String _hostName;
        private ParameterInfo[] _methodParams;
        private String _debugProfileMarkerName;

        protected static readonly ProfilerMarker CallMarker = new ( ProfilerCategory.Scripts,  $"{nameof(CallBinding)}.Call", MarkerFlags.Script );

        public void Awake( MonoBehaviour host )
        {
            if ( !Enabled )   
                return;

            if ( !Source )
            {
                Debug.LogError( $"[{nameof(Binding)}] Source is not assigned at {host.name}", host );
                return;
            }

            if ( String.IsNullOrEmpty( Path ) )
            {
                Debug.LogError( $"[{nameof(Binding)}] Path is not assigned at {host.name}", host );
                return;
            }

            var sourceType = Source.GetType();
            var method   = sourceType.GetMethod( Path );

            if ( method == null )
            {
                Debug.LogError( $"[{nameof(Binding)}] Method {Path} not found in {sourceType.Name}", host );
                return;
            }

            _hostName = host.name;

            var methodParams = method.GetParameters();

            if ( method.ReturnType == typeof(void) )
            {
                //Optimized
                if ( methodParams.Length == 0 )
                {
                    _delegateCall = (Action)Delegate.CreateDelegate( typeof(Action), Source, method );
                    _paramsType = EParamsType.Void;
                }
                else if ( methodParams.Length == 1 )
                {
                    //Optimized
                    if ( methodParams[ 0 ].ParameterType == typeof(int) )
                    {
                        //var timer = Stopwatch.StartNew();
                        _delegateCall = (Action<int>)Delegate.CreateDelegate( typeof(Action<int>), Source, method );
                        //timer.Stop();
                        //Debug.Log($"Create delegate {timer.Elapsed.TotalMicroseconds()} mks");
                        _paramsType  = EParamsType.Int;
                    }
                    else       //Not optimized
                    {
                        _delegateCall = ConstructAction1( Source, method );
                        _paramsType     = EParamsType.Boxed1Param;
                    }
                }
                else if( methodParams.Length == 2 )      //Not optimized              
                {
                    //Construct 2 params delegate with boxing, see https://codeblog.jonskeet.uk/2008/08/09/making-reflection-fly-and-exploring-delegates/
                    //var timer = Stopwatch.StartNew();
                    _delegateCall = ConstructAction2( Source, method );
                    //timer.Stop();
                    //Debug.Log($"Construct generic method {timer.Elapsed.TotalMicroseconds()} mks");

                    _paramsType = EParamsType.Boxed2Params;
                }
            }
            else    //Methods with result (awaitable methods)
            {
                if ( methodParams.Length == 0 )     //Most common awaitable types, no params, optimized
                {
                    if ( method.ReturnType == typeof(Awaitable) )
                    {
                        _delegateCall = (Func<Awaitable>)Delegate.CreateDelegate( typeof(Func<Awaitable>), Source, method );
                        _paramsType      = EParamsType.Void;
                        _awaitType = EAwaitableType.Awaitable;
                    }
                    else if ( method.ReturnType == typeof(Task) )
                    {
                        _delegateCall = (Func<Task>)Delegate.CreateDelegate( typeof(Func<Task>), Source, method );
                        _paramsType = EParamsType.Void;
                        _awaitType = EAwaitableType.Task;
                    }
                    else if ( method.ReturnType == typeof(ValueTask) )
                    {
                        _delegateCall = (Func<ValueTask>)Delegate.CreateDelegate( typeof(Func<ValueTask>), Source, method );
                        _paramsType      = EParamsType.Void;
                        _awaitType = EAwaitableType.ValueTask;
                    }
#if UIBINDINGS_UNITASK_SUPPORT
                    else if( method.ReturnType == typeof(UniTask) )
                    {
                        _delegateCall = (Func<UniTask>)Delegate.CreateDelegate( typeof(Func<UniTask>), Source, method );
                        _paramsType    = EParamsType.Void;
                        _awaitType = EAwaitableType.UniTask;
                    }
                    else if( method.ReturnType == typeof(UniTaskVoid) )
                    {
                        _delegateCall = (Func<UniTaskVoid>)Delegate.CreateDelegate( typeof(Func<UniTaskVoid>), Source, method );
                        _paramsType        = EParamsType.Void;
                        _awaitType = EAwaitableType.UniTaskVoid;
                    }
#endif
                }
                else if ( methodParams.Length == 1 )    //int param optimized, others - no
                {
                    if ( methodParams[ 0 ].ParameterType == typeof(int) )      //Optimized awaitable calls with int param
                    {
                        if ( method.ReturnType == typeof(Awaitable) )
                        {
                            _delegateCall = (Func<int, Awaitable>)Delegate.CreateDelegate( typeof(Func<int, Awaitable>), Source, method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.Awaitable;
                        }
                        else if ( method.ReturnType == typeof(Task) )
                        {
                            _delegateCall = (Func<int, Task>)Delegate.CreateDelegate( typeof(Func<int, Task>), Source, method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.Task;
                        }
                        else if ( method.ReturnType == typeof(ValueTask) )
                        {
                            _delegateCall = (Func<int, ValueTask>)Delegate.CreateDelegate( typeof(Func<int, ValueTask>), Source, method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.ValueTask;
                        }
#if UIBINDINGS_UNITASK_SUPPORT
                        else if( method.ReturnType == typeof(UniTask) )
                        {
                            _delegateCall = (Func<int, UniTask>)Delegate.CreateDelegate( typeof(Func<int, UniTask>), Source, method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.UniTask;
                        }
                        else if( method.ReturnType == typeof(UniTaskVoid) )
                        {
                            _delegateCall = (Func<int, UniTaskVoid>)Delegate.CreateDelegate( typeof(Func<int, UniTaskVoid>), Source, method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.UniTaskVoid;
                        }
#endif
                    }
                    else        //Awaitable with 1 param other than int, non optimized
                    {
                        if ( method.ReturnType == typeof(Awaitable) )
                        {
                            _delegateCall = ConstructFunc1<Awaitable>( Source, method );
                            _paramsType   = EParamsType.Boxed1Param;
                            _awaitType    = EAwaitableType.Awaitable;
                        }
                        else if ( method.ReturnType == typeof(Task) )
                        {
                            _delegateCall = ConstructFunc1<Task>( Source, method );
                            _paramsType   = EParamsType.Boxed1Param;
                            _awaitType    = EAwaitableType.Task;
                        }
                        else if ( method.ReturnType == typeof(ValueTask) )
                        {
                            _delegateCall = ConstructFunc1<ValueTask>( Source, method );
                            _paramsType   = EParamsType.Boxed1Param;
                            _awaitType    = EAwaitableType.ValueTask;
                        }
#if UIBINDINGS_UNITASK_SUPPORT
                        else if( method.ReturnType == typeof(UniTask) )
                        {
                            _delegateCall = ConstructFunc1<UniTask>( Source, method );
                            _paramsType   = EParamsType.Boxed1Param;
                            _awaitType    = EAwaitableType.UniTask;
                        }
                        else if( method.ReturnType == typeof(UniTaskVoid) )
                        {
                            _delegateCall = ConstructFunc1<UniTaskVoid>( Source, method );
                            _paramsType   = EParamsType.Boxed1Param;
                            _awaitType    = EAwaitableType.UniTaskVoid;
                        }
#endif
                    }
                }
                else if ( methodParams.Length == 2 )   //Awaitable with 2 params, not optimized
                {
                    if ( method.ReturnType == typeof(Awaitable) )
                    {
                        _delegateCall = ConstructFunc2<Awaitable>( Source, method );
                        _paramsType   = EParamsType.Boxed2Params;
                        _awaitType    = EAwaitableType.Awaitable;
                    }
                    else if ( method.ReturnType == typeof(Task) )
                    {
                        _delegateCall = ConstructFunc2<Task>( Source, method );
                        _paramsType   = EParamsType.Boxed2Params;
                        _awaitType    = EAwaitableType.Task;
                    }
                    else if ( method.ReturnType == typeof(ValueTask) )
                    {
                        _delegateCall = ConstructFunc2<ValueTask>( Source, method );
                        _paramsType   = EParamsType.Boxed2Params;
                        _awaitType    = EAwaitableType.ValueTask;
                    }
#if UIBINDINGS_UNITASK_SUPPORT
                    else if( method.ReturnType == typeof(UniTask) )
                    {
                        _delegateCall = ConstructFunc2<UniTask>( Source, method );
                        _paramsType   = EParamsType.Boxed2Params;
                        _awaitType    = EAwaitableType.UniTask;
                    }
                    else if( method.ReturnType == typeof(UniTaskVoid) )
                    {
                        _delegateCall = ConstructFunc2<UniTaskVoid>( Source, method );
                        _paramsType   = EParamsType.Boxed2Params;
                        _awaitType    = EAwaitableType.UniTaskVoid;
                    }
#endif
                }
            }

            Assert.IsTrue( _paramsType != EParamsType.Unknown, $"[{nameof(CallBinding)}] Method {Path} has unsupported signature at {_hostName}" );
           
            _debugProfileMarkerName = $"{_hostName} -> {Source.name}.{Path}()";
            _methodParams = methodParams;
            _isValid = true;
        }

        public void Call( )
        {
            if( !Enabled || !_isValid )
                return;

            CallMarker.Begin( _debugProfileMarkerName );

            if ( _awaitType == EAwaitableType.Sync )
            {
                if ( _paramsType == EParamsType.Void )
                {
                    ((Action)_delegateCall).Invoke();
                }
                else if ( _paramsType == EParamsType.Int )
                {
                    ((Action<int>)_delegateCall).Invoke( Params[0].GetInt() );
                }
                else if ( _paramsType == EParamsType.Boxed1Param )
                {
                    ((Action<object>)_delegateCall).Invoke( Params[0].GetBoxedValue( _methodParams[0].ParameterType ) );
                }
                else if ( _paramsType == EParamsType.Boxed2Params )
                {
                    ((Action<object, object>)_delegateCall).Invoke( Params[0].GetBoxedValue( _methodParams[0].ParameterType ), Params[1].GetBoxedValue( _methodParams[1].ParameterType ) );
                }
            }
            else       //Async
            {
#if UIBINDINGS_UNITASK_SUPPORT
                if ( _awaitType == EAwaitableType.UniTaskVoid ) //Not awaitable, actually
                {
                    UniTaskVoid task = default;
                    if( _paramsType == EParamsType.Void )
                        task = ((Func<UniTaskVoid>)_delegateCall)();
                    else if( _paramsType == EParamsType.Int )
                        task = ((Func<int, UniTaskVoid>)_delegateCall)( Params[0].GetInt() );
                    else if( _paramsType == EParamsType.Boxed1Param )
                        task = (UniTaskVoid)((Func<Object, Object>)_delegateCall)( Params[0].GetBoxedValue( _methodParams[0].ParameterType ) );
                    else if( _paramsType == EParamsType.Boxed2Params )
                        task = (UniTaskVoid)((Func<Object, Object, Object>)_delegateCall)( Params[0].GetBoxedValue( _methodParams[0].ParameterType ), Params[1].GetBoxedValue( _methodParams[1].ParameterType ) );
                    task.Forget();
                }else
#endif
                ProcessAwaitableCall();
            }

            CallMarker.End();
        }

        // private AwaitableAction GetAwaitableWrapper( MethodInfo method )
        // {
        //     var awaitableType = method.ReturnType;
        //     var getAwaiterMethod = awaitableType.GetMethod( "GetAwaiter" );
        //     if ( getAwaiterMethod == null )
        //         return default;
        //
        //     AwaitableAction result;
        //     result.AwaitableType = awaitableType;
        //     result.GetAwaitableMethod = (Func<Object>)Delegate.CreateDelegate( typeof(Func<Object>), Source, method );
        //     result.AwaiterType = getAwaiterMethod.ReturnType;
        //     result.GetAwaiterMethod = (Func<Object, Object>)Delegate.CreateDelegate( typeof(Func<Object, Object>), getAwaiterMethod );
        //     var isCompletedProperty = result.AwaiterType.GetProperty( "IsCompleted" );
        //     if ( isCompletedProperty == null || isCompletedProperty.PropertyType != typeof(bool) || isCompletedProperty.GetMethod == null )
        //         return default;
        //     result.IsCompletedProperty = (Func<Object, Boolean>)Delegate.CreateDelegate( typeof(Func<Object, Boolean>), isCompletedProperty.GetMethod );
        //     var getResultMethod = result.AwaiterType.GetMethod( "GetResult" );
        //     if( getResultMethod == null )
        //         return default;
        //     result.GetResultMethod = (Action<Object>)Delegate.CreateDelegate( typeof(Action<Object>), getResultMethod );
        //
        //     return result;
        // }

        private async void ProcessAwaitableCall(  )
        {
            switch ( _awaitType )
            {
                case EAwaitableType.Awaitable:
                    Awaitable task = null;
                    if( _paramsType == EParamsType.Void )
                        task = ((Func<Awaitable>)_delegateCall)();
                    else if( _paramsType == EParamsType.Int )
                        task = ((Func<int, Awaitable>)_delegateCall)( Params[0].GetInt() );
                    else if( _paramsType == EParamsType.Boxed1Param )
                        task = (Awaitable)((Func<Object, Object>)_delegateCall)( Params[0].GetBoxedValue( _methodParams[0].ParameterType ) );
                    else if( _paramsType == EParamsType.Boxed2Params )
                        task = (Awaitable)((Func<Object, Object, Object>)_delegateCall)( Params[0].GetBoxedValue( _methodParams[0].ParameterType ), Params[1].GetBoxedValue( _methodParams[1].ParameterType ) );
                    await task;
                    break;

                case EAwaitableType.Task:
                    Task t = null;
                    if( _paramsType == EParamsType.Void )
                        t = ((Func<Task>)_delegateCall)();
                    else if( _paramsType == EParamsType.Int )
                        t = ((Func<int, Task>)_delegateCall)( Params[0].GetInt() );
                    else if( _paramsType == EParamsType.Boxed1Param )
                        t = (Task)((Func<Object, Object>)_delegateCall)( Params[0].GetBoxedValue( _methodParams[0].ParameterType ) );
                    else if( _paramsType == EParamsType.Boxed2Params )
                        t = (Task)((Func<Object, Object, Object>)_delegateCall)( Params[0].GetBoxedValue( _methodParams[0].ParameterType ), Params[1].GetBoxedValue( _methodParams[1].ParameterType ) );
                    await t;
                    break;

                case EAwaitableType.ValueTask:
                    ValueTask vt = default;
                    if( _paramsType == EParamsType.Void )
                        vt = ((Func<ValueTask>)_delegateCall)();
                    else if( _paramsType == EParamsType.Int )
                        vt = ((Func<int, ValueTask>)_delegateCall)( Params[0].GetInt() );
                    else if( _paramsType == EParamsType.Boxed1Param )
                        vt = (ValueTask)((Func<Object, Object>)_delegateCall)( Params[0].GetBoxedValue( _methodParams[0].ParameterType ) );
                    else if( _paramsType == EParamsType.Boxed2Params )
                        vt = (ValueTask)((Func<Object, Object, Object>)_delegateCall)( Params[0].GetBoxedValue( _methodParams[0].ParameterType ), Params[1].GetBoxedValue( _methodParams[1].ParameterType ) );
                    await vt;
                    break;

#if UIBINDINGS_UNITASK_SUPPORT
                case EAwaitableType.UniTask:
                    UniTask ut = default;
                    if( _paramsType == EParamsType.Void )
                        ut = ((Func<UniTask>)_delegateCall)();
                    else if( _paramsType == EParamsType.Int )
                        ut = ((Func<int, UniTask>)_delegateCall)( Params[0].GetInt() );
                    else if( _paramsType == EParamsType.Boxed1Param )
                        ut = (UniTask)((Func<Object, Object>)_delegateCall)( Params[0].GetBoxedValue( _methodParams[0].ParameterType ) );
                    else if( _paramsType == EParamsType.Boxed2Params )
                        ut = (UniTask)((Func<Object, Object, Object>)_delegateCall)( Params[0].GetBoxedValue( _methodParams[0].ParameterType ), Params[1].GetBoxedValue( _methodParams[1].ParameterType ) );
                    await ut;
                    break;
#endif
            }

            // var awaitable = awaitableMethod.GetAwaitableMethod();
            // var awaiter = awaitableMethod.GetAwaiterMethod( awaitable );
            //
            // //Wait for completion
            // while ( !awaitableMethod.IsCompletedProperty( awaiter ) )
            // {
            //     await Awaitable.NextFrameAsync(  );
            // }
            //
            // //Get result, unwind exception
            // awaitableMethod.GetResultMethod( awaiter );
        }

        //Construct 1 params instance delegate with boxing
        private static Action<Object> ConstructAction1( Object source, MethodInfo method )
        {
            var paramz              = method.GetParameters();
            var type1               = paramz[ 0 ].ParameterType;
            var convertMethod       = typeof(CallBinding).GetMethod( nameof( ConvertAction1 ), BindingFlags.NonPublic | BindingFlags.Static );
            var closedConvertMethod = convertMethod.MakeGenericMethod( type1 );
            var result              = (Action<Object>)closedConvertMethod.Invoke( null, new [] { source, method } );
            return result;
        }

        private static Action<Object> ConvertAction1<TParam1>( Object source, MethodInfo method )
        {
            var  strong = (Action<TParam1>) Delegate.CreateDelegate( typeof(Action<TParam1>), source, method );
            Action<Object> weak   = (p1) => strong( (TParam1)p1 );
            return weak;
        }

        //Construct 2 params instance delegate with boxing
        private static Action<Object, Object> ConstructAction2( Object source, MethodInfo method )
        {
            var paramz = method.GetParameters();
            var type1 = paramz[ 0 ].ParameterType;
            var type2 = paramz[ 1 ].ParameterType;
            var convertMethod = typeof(CallBinding).GetMethod( nameof( ConvertAction2 ), BindingFlags.NonPublic | BindingFlags.Static );
            var closedConvertMethod = convertMethod.MakeGenericMethod( type1, type2 );
            var result = (Action<Object, Object>)closedConvertMethod.Invoke( null, new [] { source, method } );
            return result;
        }

        private static Action<Object, Object> ConvertAction2<TParam1, TParam2>( Object source, MethodInfo method )
        {
            var strong = (Action<TParam1, TParam2>) Delegate.CreateDelegate( typeof(Action<TParam1, TParam2>), source, method );
            Action<Object, Object> weak = (p1, p2) => strong( (TParam1)p1, (TParam2)p2 );
            return weak;
        }

        //Construct 1 params instance func delegate with boxing
        private static Func<Object, Object> ConstructFunc1<TAwaitable>( Object source, MethodInfo method )
        {
            var paramz              = method.GetParameters();
            var type1               = paramz[ 0 ].ParameterType;
            var convertMethod       = typeof(CallBinding).GetMethod( nameof( ConvertFunc1 ), BindingFlags.NonPublic | BindingFlags.Static );
            var closedConvertMethod = convertMethod.MakeGenericMethod( type1, typeof(TAwaitable) );
            var result              = (Func<Object, Object>)closedConvertMethod.Invoke( null, new [] { source, method } );
            return result;
        }

        private static Func<Object, Object> ConvertFunc1<TParam1, TAwaitable>( Object source, MethodInfo method )
        {
            var            strong = (Func<TParam1, TAwaitable>) Delegate.CreateDelegate( typeof(Func<TParam1, TAwaitable>), source, method );
            Func<Object, Object> weak   = (p1) => (Object)strong( (TParam1)p1 );
            return weak;
        }

        //Construct 2 params instance func delegate with boxing
        private static Func<Object, Object, Object> ConstructFunc2<TAwaitable>( Object source, MethodInfo method )
        {
            var paramz              = method.GetParameters();
            var type1               = paramz[ 0 ].ParameterType;
            var type2               = paramz[ 1 ].ParameterType;
            var convertMethod       = typeof(CallBinding).GetMethod( nameof( ConvertFunc2 ), BindingFlags.NonPublic | BindingFlags.Static );
            var closedConvertMethod = convertMethod.MakeGenericMethod( type1, type2, typeof(TAwaitable) );
            var result              = (Func<Object, Object, Object>)closedConvertMethod.Invoke( null, new [] { source, method } );
            return result;
        }

        private static Func<Object, Object, Object> ConvertFunc2<TParam1, TParam2, TAwaitable>( Object source, MethodInfo method )
        {
            var strong = (Func<TParam1, TParam2, TAwaitable>) Delegate.CreateDelegate( typeof(Func<TParam1, TParam2, TAwaitable>), source, method );
            Func<Object, Object, Object> weak   = (p1, p2) => (Object)strong( (TParam1)p1, (TParam2)p2 );
            return weak;
        }


        public enum EParamsType
        {
            Unknown,
            Void,
            Int,
            Boxed1Param,
            Boxed2Params, 
        }

        public enum EAwaitableType
        {
            Sync,
            Awaitable,
            Task,
            ValueTask,
            UniTask,
            UniTaskVoid,
        }


        public struct AwaitableAction
        {
            public Type AwaitableType;
            public Type AwaiterType;
            public Func<Object> GetAwaitableMethod;              //Closed delegate on Source instance
            public Func<Object, Object> GetAwaiterMethod; //Open delegate on AwaitableType instance
            public Func<Object, Boolean> IsCompletedProperty;    //Open delegate on AwaiterType instance
            public Action<Object> GetResultMethod;               //Open delegate on AwaiterType instance


        }
    }
}

