using System;
using System.Linq;
using System.Reflection;
using Sisus;
using UIBindings.Adapters;
using UIBindings.Runtime;
using UIBindings.Runtime.Utils;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UIBindings
{
    [Serializable]
    public class CallBinding : BindingBase
    {
        public SerializableParam[] Params;                          //To store method's parameters

        public bool IsRuntimeValid => _isValid;

        //All call delegates lives here
        private Delegate _delegateCall;

        private Boolean _isValid;
        private ParameterInfo[] _methodParams;

        private MethodAdapter _methodAdapter;

        public void Init( object sourceObject = null )
        {
            if ( !Enabled )   
                return;

            if ( BindToType )
            {
                if ( sourceObject == null )
                {
                    Debug.LogError( $"[{nameof(CallBinding)}] Source object must be assigned for binding {GetBindingTargetInfo()} from the code. Assigned object must be {SourceType} type", _debugHost );
                    _isValid = false;
                    return;
                }
                SourceObject = sourceObject;
            }
            else
            {
                if ( !Source )
                {
                    if ( sourceObject != null )
                        SourceObject = sourceObject;
                    else
                    {
                        Debug.LogError( $"[{nameof(CallBinding)}] Source object must be assigned for binding {GetBindingTargetInfo()} from the Inspector or from the code.", _debugHost );
                        _isValid = false;
                        return;
                    }
                }
                else
                    SourceObject = Source;
            }

            InitInfrastructure();
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

            //Here we process deep binding
            var pathProcessor = new PathProcessor( SourceObject, Path, false, null );
            //if ( pathProcessor.IsComplexPath )
            {
                while ( pathProcessor.MoveNext( ) )
                {
                }

                if( pathProcessor.Current is not MethodAdapter methodAdapter )
                {
                    Debug.LogError( $"[{nameof(CallBinding)}] Path '{Path}' at {_debugHost} does not point to a method", _debugHost );
                    return;
                }
                else
                    _methodAdapter = methodAdapter;
            }

            _isValid      = true;
        }

        public Awaitable Call( )
        {
            if( !Enabled || !_isValid )
                return AwaitableUtility.CompletedAwaitable;

            CallMarker.Begin( ProfilerMarkerName );

            var task = _methodAdapter.CallAsync( Params );

            CallMarker.End();

            return task;
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

            //var state = $"{_awaitType} {_paramsType} {_delegateCall.GetType().Name}";
            var state = $"TODO";
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

