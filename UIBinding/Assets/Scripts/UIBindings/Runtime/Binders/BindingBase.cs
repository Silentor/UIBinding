using System;
using System.Reflection;
using Cysharp.Threading.Tasks.Triggers;
using UIBindings.Runtime.Utils;
using UnityEngine;
using Object = System.Object;

namespace UIBindings
{
    [Serializable]
    public abstract class BindingBase
    {
        //Is binding works at all, checked once on start
        public        Boolean                   Enabled             = true;                        

        //Reference to Unity source object
        public        UnityEngine.Object        Source;
        //Type of source object (if no Unity source object reference provided)
        public        String                    SourceType;
        //If true, get binding type from <see cref="SourceType"/>, otherwise from <see cref="Source"/>
        public        Boolean                   BindToType;
        //Path to bindable property or method
        public        String                    Path;

        public object  SourceObject
        {
            get => _sourceObject;
            set
            {
                if ( _sourceObject != value )
                {
                    var oldValue = _sourceObject;
                    _sourceObject = value;
                    OnSetSourceObject( oldValue, value );
                }
            }
        }

        private object _sourceObject;

        /// <summary>
        /// Make sure binding correctly changes source object
        /// </summary>
        protected abstract void OnSetSourceObject( object oldValue, object value );

        protected void SetSourceObjectWithoutNotify( object value )
        {
            _sourceObject = value;
        }

#region Runtime debug stuff

        /// <summary>
        /// Can be called before Init for useful logs in case of errors.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="bindingName"></param>
        public virtual void SetDebugInfo( MonoBehaviour host, String bindingName )
        {
            _debugHost = host;
            _debugBindingName = bindingName;
        }

        /// <summary>
        /// Should return runtime debug info about source. Looks like it common for all bindings
        /// </summary>
        /// <returns></returns>
        public abstract string GetBindingSourceInfo( );

        /// <summary>
        /// Should return info about self
        /// </summary>
        /// <returns></returns>
        public abstract string GetBindingTargetInfo( ) ;

        /// <summary>
        /// Should return direction of binding in form of arrows and converters count
        /// </summary>
        /// <returns></returns>
        public abstract string GetBindingDirection( );

        /// <summary>
        /// Should return current state of binding (failed or valid) and last value if any
        /// </summary>
        /// <returns></returns>
        public abstract string GetBindingState( );

        /// <summary>
        /// Should return current state of source object, like value of source property
        /// </summary>
        /// <returns></returns>
        public abstract string GetSourceState( );

        /// <summary>
        /// Gets full runtime info about binding, including source, target, direction and state.
        /// </summary>
        /// <returns></returns>
        public abstract string GetFullRuntimeInfo( );

        //Debug, log, inspector stuff
        protected MonoBehaviour _debugHost;                 //Host of binder that contains this binding, for debug purposes
        protected string        _debugBindingName            ;  //Name of binding property, for debug purposes
        protected string        _debugTargetBindingInfo;
        

        public override String ToString( )
        {
            return $"{GetBindingSourceInfo()} {GetBindingDirection()} {GetBindingTargetInfo()}";
        }

#endregion
    }
}