using System;
using UIBindings.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UIBindings
{
    public class BinderBase : MonoBehaviour
    {
        protected  Object GetParentSource( )
        {
            if ( !_isParentSourceChecked )
            {
                _isParentSourceChecked = true;
                _parentSource = transform.GetComponentInParent<ViewModel>();
            }

            return _parentSource;
        }

        protected int GetUpdateOrder( )
        {
            if ( _updateOrder == Int32.MinValue)                
                _updateOrder = CalculateDepth();

            return _updateOrder;
        }

        private int CalculateDepth( )
        {
            int depth = 0;
            var parent = transform.parent;
            while ( parent != null )
            {
                depth++;
                parent = parent.parent;
            }
            return depth;
        }

        private Object _parentSource;
        private bool _isParentSourceChecked;
        private int _updateOrder = Int32.MinValue;
    }
}