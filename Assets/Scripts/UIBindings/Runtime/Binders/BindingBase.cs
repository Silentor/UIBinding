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
        public        Boolean                   Enabled             = true;                        //Checked once on start!

        //Reference to Unity source object (if BindToType is false)
        public        UnityEngine.Object        Source;
        //Type of source object (if BindToType is true)
        public        String                    SourceType;
        //If true, binding will need to be inited with instance of type SourceType
        public        Boolean                   BindToType;
        //Path to bindable property or method
        public        String                    Path;

        public object SourceObject { get; protected set; } 

        //Debug stuff

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
        /// Should return debug info about source. Looks like it common for all bindings
        /// </summary>
        /// <returns></returns>
        public string GetBindingSourceInfo( )
        {
            if ( SourceObject.IsNotAssigned() )
            {
                return "?";
            }
            else
            {
                var sourceType = SourceObject.GetType();
                var sourceProp = sourceType.GetProperty( Path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
                var sourceObjectName = SourceObject is UnityEngine.Object unityObject ? $"'{unityObject.name}'" : SourceObject.GetType().ToString();
                if( sourceProp != null )
                {
                    return $"{sourceProp.PropertyType.Name} {sourceObjectName}.{Path}";
                }
                else
                {
                    return $"{sourceObjectName}.{Path}?";
                }
            }
        }

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

        //Debug, log, inspector stuff
        protected MonoBehaviour _debugHost;                 //Host of binder that contains this binding, for debug purposes
        protected string        _debugBindingName            ;  //Name of binding property, for debug purposes
        protected string        _debugTargetBindingInfo;


    }
}