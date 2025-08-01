using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Sisus;
using UIBindings.Runtime;
using UIBindings.Runtime.Utils;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Object = System.Object;

#if UIBINDINGS_UNITASK_SUPPORT
using Cysharp.Threading.Tasks;
#endif

namespace UIBindings
{
    [Serializable]
    public class CallBinding : BindingBase
    {
        public SerializableParam[] Params; 

        public bool IsRuntimeValid => _isValid;

        //All call delegates lives here
        private Delegate _delegateCall;

        private EParamsType _paramsType;
        private EAwaitableType _awaitType;

        private Boolean _isValid;
        private ParameterInfo[] _methodParams;



        public void Init( object sourceObject = null )
        {
            if ( !Enabled )   
                return;

            if ( BindToType )
            {
                if ( sourceObject == null )
                {
                    Debug.LogError( $"[{nameof(CallBinding)}] Source object must be assigned for binding {GetBindingTargetInfo()} from the code. Assigned object must be {SourceType} type", _debugHost );
                    return;
                }
                SourceObject = sourceObject;
            }
            else
            {
                if ( !Source )
                {
                    var unityObjectSource = sourceObject as UnityEngine.Object;
                    if( !unityObjectSource )
                    {
                        Debug.LogError( $"[{nameof(CallBinding)}] Source object must be assigned for binding {GetBindingTargetInfo()} from the Inspector", _debugHost );
                        return;
                    }
                    SourceObject = unityObjectSource;
                }
                else
                    SourceObject = Source;
            }

            InitInfrastructure();

            Debug.Log( $"Inited callbinding {GetHashCode()} on {_debugHost.name} ({_debugHost.GetHashCode()})" );
        }

        private void InitInfrastructure( )
        {
            if ( SourceObject == null )
            {
                Debug.LogError( $"[{nameof(BindingBase)}] Source is not assigned at {_debugHost}", _debugHost );
                return;
            }

            if ( String.IsNullOrEmpty( Path ) )
            {
                Debug.LogError( $"[{nameof(BindingBase)}] Path is not assigned at {_debugHost}", _debugHost );
                return;
            }

            var sourceType = SourceObject.GetType();
            var method     = sourceType.GetMethod( Path );

            if ( method == null )
            {
                Debug.LogError( $"[{nameof(BindingBase)}] Method {Path} not found in {sourceType.Name}", _debugHost );
                return;
            }

            var methodParams = method.GetParameters();

            if ( method.ReturnType == typeof(void) )
            {
                //Optimized
                if ( methodParams.Length == 0 )
                {
                    _delegateCall = (Action)Delegate.CreateDelegate( typeof(Action), SourceObject, method );
                    _paramsType   = EParamsType.Void;
                }
                else if ( methodParams.Length == 1 )
                {
                    //Optimized
                    if ( methodParams[ 0 ].ParameterType == typeof(int) )
                    {
                        //var timer = Stopwatch.StartNew();
                        _delegateCall = (Action<int>)Delegate.CreateDelegate( typeof(Action<int>), SourceObject, method );
                        //timer.Stop();
                        //Debug.Log($"Create delegate {timer.Elapsed.TotalMicroseconds()} mks");
                        _paramsType  = EParamsType.Int;
                    }
                    else       //Not optimized
                    {
                        _delegateCall = ConstructAction1( SourceObject, method );
                        _paramsType   = EParamsType.Boxed1Param;
                    }
                }
                else if( methodParams.Length == 2 )      //Not optimized              
                {
                    //Construct 2 params delegate with boxing, see https://codeblog.jonskeet.uk/2008/08/09/making-reflection-fly-and-exploring-delegates/
                    //var timer = Stopwatch.StartNew();
                    _delegateCall = ConstructAction2( SourceObject, method );
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
                        _delegateCall = (Func<Awaitable>)Delegate.CreateDelegate( typeof(Func<Awaitable>), SourceObject, method );
                        _paramsType   = EParamsType.Void;
                        _awaitType    = EAwaitableType.Awaitable;
                    }
                    else if ( method.ReturnType == typeof(Task) )
                    {
                        _delegateCall = (Func<Task>)Delegate.CreateDelegate( typeof(Func<Task>), SourceObject, method );
                        _paramsType   = EParamsType.Void;
                        _awaitType    = EAwaitableType.Task;
                    }
                    else if ( method.ReturnType == typeof(ValueTask) )
                    {
                        _delegateCall = (Func<ValueTask>)Delegate.CreateDelegate( typeof(Func<ValueTask>), SourceObject, method );
                        _paramsType   = EParamsType.Void;
                        _awaitType    = EAwaitableType.ValueTask;
                    }
#if UIBINDINGS_UNITASK_SUPPORT
                    else if( method.ReturnType == typeof(UniTask) )
                    {
                        _delegateCall = (Func<UniTask>)Delegate.CreateDelegate( typeof(Func<UniTask>), SourceObject, method );
                        _paramsType   = EParamsType.Void;
                        _awaitType    = EAwaitableType.UniTask;
                    }
                    else if( method.ReturnType == typeof(UniTaskVoid) )
                    {
                        _delegateCall = (Func<UniTaskVoid>)Delegate.CreateDelegate( typeof(Func<UniTaskVoid>), SourceObject, method );
                        _paramsType   = EParamsType.Void;
                        _awaitType    = EAwaitableType.UniTaskVoid;
                    }
#endif
                }
                else if ( methodParams.Length == 1 )    //int param optimized, others - no
                {
                    if ( methodParams[ 0 ].ParameterType == typeof(int) )      //Optimized awaitable calls with int param
                    {
                        if ( method.ReturnType == typeof(Awaitable) )
                        {
                            _delegateCall = (Func<int, Awaitable>)Delegate.CreateDelegate( typeof(Func<int, Awaitable>), SourceObject, method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.Awaitable;
                        }
                        else if ( method.ReturnType == typeof(Task) )
                        {
                            _delegateCall = (Func<int, Task>)Delegate.CreateDelegate( typeof(Func<int, Task>), SourceObject, method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.Task;
                        }
                        else if ( method.ReturnType == typeof(ValueTask) )
                        {
                            _delegateCall = (Func<int, ValueTask>)Delegate.CreateDelegate( typeof(Func<int, ValueTask>), SourceObject, method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.ValueTask;
                        }
#if UIBINDINGS_UNITASK_SUPPORT
                        else if( method.ReturnType == typeof(UniTask) )
                        {
                            _delegateCall = (Func<int, UniTask>)Delegate.CreateDelegate( typeof(Func<int, UniTask>), SourceObject, method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.UniTask;
                        }
                        else if( method.ReturnType == typeof(UniTaskVoid) )
                        {
                            _delegateCall = (Func<int, UniTaskVoid>)Delegate.CreateDelegate( typeof(Func<int, UniTaskVoid>), SourceObject, method );
                            _paramsType   = EParamsType.Int;
                            _awaitType    = EAwaitableType.UniTaskVoid;
                        }
#endif
                    }
                    else        //Awaitable with 1 param other than int, non optimized
                    {
                        if ( method.ReturnType == typeof(Awaitable) )
                        {
                            _delegateCall = ConstructFunc1<Awaitable>( SourceObject, method );
                            _paramsType   = EParamsType.Boxed1Param;
                            _awaitType    = EAwaitableType.Awaitable;
                        }
                        else if ( method.ReturnType == typeof(Task) )
                        {
                            _delegateCall = ConstructFunc1<Task>( SourceObject, method );
                            _paramsType   = EParamsType.Boxed1Param;
                            _awaitType    = EAwaitableType.Task;
                        }
                        else if ( method.ReturnType == typeof(ValueTask) )
                        {
                            _delegateCall = ConstructFunc1<ValueTask>( SourceObject, method );
                            _paramsType   = EParamsType.Boxed1Param;
                            _awaitType    = EAwaitableType.ValueTask;
                        }
#if UIBINDINGS_UNITASK_SUPPORT
                        else if( method.ReturnType == typeof(UniTask) )
                        {
                            _delegateCall = ConstructFunc1<UniTask>( SourceObject, method );
                            _paramsType   = EParamsType.Boxed1Param;
                            _awaitType    = EAwaitableType.UniTask;
                        }
                        else if( method.ReturnType == typeof(UniTaskVoid) )
                        {
                            _delegateCall = ConstructFunc1<UniTaskVoid>( SourceObject, method );
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
                        _delegateCall = ConstructFunc2<Awaitable>( SourceObject, method );
                        _paramsType   = EParamsType.Boxed2Params;
                        _awaitType    = EAwaitableType.Awaitable;
                    }
                    else if ( method.ReturnType == typeof(Task) )
                    {
                        _delegateCall = ConstructFunc2<Task>( SourceObject, method );
                        _paramsType   = EParamsType.Boxed2Params;
                        _awaitType    = EAwaitableType.Task;
                    }
                    else if ( method.ReturnType == typeof(ValueTask) )
                    {
                        _delegateCall = ConstructFunc2<ValueTask>( SourceObject, method );
                        _paramsType   = EParamsType.Boxed2Params;
                        _awaitType    = EAwaitableType.ValueTask;
                    }
#if UIBINDINGS_UNITASK_SUPPORT
                    else if( method.ReturnType == typeof(UniTask) )
                    {
                        _delegateCall = ConstructFunc2<UniTask>( SourceObject, method );
                        _paramsType   = EParamsType.Boxed2Params;
                        _awaitType    = EAwaitableType.UniTask;
                    }
                    else if( method.ReturnType == typeof(UniTaskVoid) )
                    {
                        _delegateCall = ConstructFunc2<UniTaskVoid>( SourceObject, method );
                        _paramsType   = EParamsType.Boxed2Params;
                        _awaitType    = EAwaitableType.UniTaskVoid;
                    }
#endif
                }
            }

            if (_paramsType == EParamsType.Unknown)
            {
                Debug.LogError($"[{nameof(CallBinding)}] Method {Path} has unsupported signature at {_debugHost}", _debugHost);
                return;
            }
           
            _methodParams = methodParams;
            _isValid      = true;
        }

        public Awaitable Call( )
        {
            if( !Enabled || !_isValid )
                return AwaitableUtility.CompletedAwaitable;

            CallMarker.Begin( ProfilerMarkerName );

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
                }
                else
#endif
                {
                    //Truly async call, return awaitable
                    var task = ProcessAwaitableCall();
                    CallMarker.End();
                    return task;
                }
            }

            CallMarker.End();
            return AwaitableUtility.CompletedAwaitable;
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

        private async Awaitable ProcessAwaitableCall(  )
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

#region Debug stuff

        protected static readonly ProfilerMarker CallMarker = new ( ProfilerCategory.Scripts,  $"{nameof(CallBinding)}.Call", MarkerFlags.Script );
        protected string ProfilerMarkerName = String.Empty;

        public override void SetDebugInfo( MonoBehaviour host, String bindingName )
        {
            base.SetDebugInfo( host, bindingName );

            _debugTargetBindingInfo = $"CallBinding '{host.name}'({host.GetType().Name}).{bindingName}";
            ProfilerMarkerName = GetBindingTargetInfo( );
        }

        public override string GetBindingSourceInfo( )
        {
            if ( SourceObject.IsNotAssigned() )
            {
                return "?";
            }
            else
            {
                var sourceType       = SourceObject.GetType();
                var sourceMethod     = sourceType.GetMethod( Path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
                var sourceObjectName = SourceObject is UnityEngine.Object unityObject ? $"'{unityObject.name}'({sourceType.Name})" : $"({sourceType.Name})";
                if( sourceMethod != null )
                {
                    var paramsInfo = sourceMethod.GetParameters().Select( p => p.ParameterType.Name ).ToArray().JoinToString(  );
                    return $"{sourceMethod.ReturnType.Name} {sourceObjectName}.{sourceMethod.Name}({paramsInfo})";
                }
                else
                {
                    return $"{sourceObjectName}.{Path}()?";
                }
            }
        }

        public override String GetBindingTargetInfo( )
        {
            if( _debugTargetBindingInfo != null )
                return _debugTargetBindingInfo;
            return $"Call binding";
        }

        public override String GetBindingDirection( )
        {
            return "<--";
        }

        public override String GetBindingState( )
        {
            if ( !Enabled )
                return "Disabled";
            if ( !_isValid )
                return "Invalid";

            var state = $"{_awaitType} {_paramsType} {_delegateCall.GetType().Name}";
            return state;
        }

        public override String GetSourceState( )
        {
            if ( SourceObject == null )
                return "Source not assigned";
        
            return string.Empty;
        }

        public override String GetFullRuntimeInfo( )
        {
            var sourceInfo  = GetBindingSourceInfo();
            var targetInfo  = GetBindingTargetInfo();
            var direction   = GetBindingDirection();
            var targetState = GetBindingState();
            var sourceState = GetSourceState();

            if( !string.IsNullOrEmpty( sourceState ) )                
                sourceState = $" <{sourceState}>";

            return $"{sourceInfo}{sourceState} {direction} {targetInfo} <{targetState}>";
        }

#endregion

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


        /*
        public struct AwaitableAction
        {
            public Type AwaitableType;
            public Type AwaiterType;
            public Func<Object> GetAwaitableMethod;              //Closed delegate on Source instance
            public Func<Object, Object> GetAwaiterMethod; //Open delegate on AwaitableType instance
            public Func<Object, Boolean> IsCompletedProperty;    //Open delegate on AwaiterType instance
            public Action<Object> GetResultMethod;               //Open delegate on AwaiterType instance


        }
        */
    }
}

