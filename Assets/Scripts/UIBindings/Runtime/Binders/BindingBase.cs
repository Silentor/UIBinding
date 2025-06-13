using System;
using System.Reflection;
using UnityEngine;

namespace UIBindings
{
    [Serializable]
    public abstract class BindingBase
    {
        public        Boolean                   Enabled             = true;                        //Checked once on start!

        //Reference to Unity source object (if BindToType is false)
        public        UnityEngine.Object Source;
        //Type of source object (if BindToType is true)
        public        String             SourceType;
        //If true, binding will need to be inited with instance of type SourceType
        public        Boolean            BindToType;
        //Path to bindable property or method
        public        String                    Path;

        //Debug stuff

        /// <summary>
        /// Can be called before Init for useful logs in case of errors.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="bindingName"></param>
        public virtual void SetDebugInfo( MonoBehaviour host, String bindingName )
        {
            //Prepare binding source info. Binding target info is set in derived classes
            string sourceName = "?";
            Type sourceType = null;
            PropertyInfo sourceProperty = null;
            string pathName = string.Empty;
            string sourcePropType = string.Empty;
            if ( BindToType )
            {
                if ( !string.IsNullOrEmpty( SourceType ) )
                {
                    sourceType = Type.GetType( SourceType );
                    if ( sourceType != null )
                    {
                        sourceName     = sourceType.Name;
                        sourceProperty = sourceType.GetProperty( Path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
                    }
                    else
                        sourceName = SourceType;
                } 
            }
            else
            {
                if ( Source )
                {
                    sourceType = Source.GetType();
                    sourceName = $"'{Source.name}'({sourceType.Name})";
                    sourceProperty = sourceType.GetProperty( Path, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
                }
            }

            sourcePropType = sourceProperty != null ? sourceProperty.PropertyType.Name : "?";
            pathName = string.IsNullOrEmpty( Path ) ? "?" : Path;

            _debugHost = host;
            _debugSourceBindingInfo = $"{sourcePropType} {sourceName}.{pathName}";
        }

        public string GetBindingSourceInfo( ) => _debugSourceBindingInfo;
        public string GetBindingTargetInfo( ) => _debugTargetBindingInfo;
        public string GetBindingDirection( ) => _debugDirectionStr;
        public abstract string GetBindingState( );

        //Debug, log, inspector stuff
        protected MonoBehaviour _debugHost;        //Host of binder that contains this binding, for debug purposes
        protected string        _debugSourceBindingInfo = string.Empty;
        protected string        _debugTargetBindingInfo = string.Empty;
        protected string        _debugDirectionStr      = String.Empty;

    }
}