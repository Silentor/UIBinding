using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using Object = System.Object;

namespace UIBindings
{
    [Serializable]
    public class CallBinding : Binding
    {
        private String _hostName;
        private Action _caller;
        private Boolean _isValid;

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

            _caller = (Action)Delegate.CreateDelegate( typeof(Action), Source, method );

            _isValid = true;
        }

        public void Call( )
        {
            if( !Enabled || !_isValid )
                return;

            _caller.Invoke();

        }
    }
}