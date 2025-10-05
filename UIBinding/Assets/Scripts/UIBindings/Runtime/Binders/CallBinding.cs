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

        public bool IsRuntimeValid => _isInited;

        //All call delegates lives here
        private Delegate _delegateCall;

        private Boolean _isInited;
        private ParameterInfo[] _methodParams;

        private MethodAdapter _methodAdapter;

        public void Init( object sourceObject = null )
        {
            if ( !Enabled )   
                return;

            Type sourceType = null;
            if(sourceObject != null )                   // Parameter has highest priority (as well as SourceObject property)
                sourceType = sourceObject.GetType();
            else if ( !BindToType && Source )
            {
                sourceType   = Source.GetType();
                sourceObject = Source;
            }
            else if ( BindToType && !string.IsNullOrEmpty( SourceType ) )
            {
                sourceType = Type.GetType( SourceType, throwOnError: false );
                // sourceObject is null, but its okay, so binding will returns default value. Only type is crucial for init
            }

            if ( sourceType == null )
            {
                if(BindToType)
                    Debug.LogError( $"[{nameof(CallBinding)}] Failed to get source type, binding not inited. Provide correct type in property SourceType or set sourceObject parameter of method Init(). Binding: {GetBindingTargetInfo()}", _debugHost );
                else
                    Debug.LogError( $"[{nameof(CallBinding)}] Failed to get source type, binding not inited. Provide correct source object reference in property Source or set sourceObject parameter of method Init(). Binding: {GetBindingTargetInfo()}", _debugHost );
                return;
            }

            InitInfrastructure( sourceType );
            SetSourceObjectWithoutNotify( sourceObject );
        }

        public Awaitable Call( )
        {
            if( !Enabled || !_isInited )
                return AwaitableUtility.CompletedAwaitable;

            CallMarker.Begin( ProfilerMarkerName );

            var task = _methodAdapter.CallAsync( Params );

            CallMarker.End();

            return task;
        }

        private void InitInfrastructure( Type sourceObjectType )
        {
            if ( String.IsNullOrEmpty( Path ) )
            {
                Debug.LogError( $"[{nameof(BindingBase)}] Path is not assigned at {_debugHost}", _debugHost );
                return;
            }

            //Here we process deep binding
            var pathProcessor = new PathProcessor( sourceObjectType, Path, false, null );
            //if ( pathProcessor.IsComplexPath )
            {
                while ( pathProcessor.MoveNext( ) )
                {
                }

                if( pathProcessor.CurrentAdapter is not MethodAdapter methodAdapter )
                {
                    Debug.LogError( $"[{nameof(CallBinding)}] Path '{Path}' at {_debugHost} does not point to a method", _debugHost );
                    return;
                }
                else
                    _methodAdapter = methodAdapter;
            }

            _isInited      = true;
        }

        protected override void OnSetSourceObject( object oldValue, object value )
        {
             _methodAdapter.SetSourceObject( value );
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
            if ( !_isInited )
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

