using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIBindings.Runtime.Utils
{
    public static class BindingUtils
    {
        public static Object GetEffectiveSource( BindingBase binding, Object bindingHost )
        {
            if ( binding.Source )
                return binding.Source;

            if( bindingHost is Component componentHost )
                return GetParentSource( componentHost );

            return null;
        }
        
        public static Object GetParentSource( Component bindingHost )
        {
            return bindingHost.GetComponentInParent<ViewModel>();    //For now, we assume that the parent source is always a ViewModel component
        }     

        public static (Type, Object) GetSourceTypeAndObject( BindingBase binding, Object bindingHost )
        {
            if ( binding.BindToType )
            {
                if ( !string.IsNullOrEmpty( binding.SourceType ) )
                {
                    var type = Type.GetType( binding.SourceType, false );
                    return (type, null);    
                }
                return (null, null);
            }
            else
            {

                var sourceObject = GetEffectiveSource( binding, bindingHost );
                if( sourceObject )
                    return (sourceObject.GetType(), sourceObject);
                return (null, null);
            }
        }

    }
}