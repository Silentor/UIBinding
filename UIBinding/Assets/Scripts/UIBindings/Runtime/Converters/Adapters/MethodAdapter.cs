using System;
using System.Collections.Generic;
using UIBindings.Runtime;
using UnityEngine;

namespace UIBindings.Adapters
{
    public abstract class MethodAdapter : PathAdapter
    {
        protected MethodAdapter(PathAdapter sourceAdapter, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) : base( sourceAdapter, isTwoWayBinding, notifyPropertyChanged )
        {
        }

        protected MethodAdapter(Type sourceObjectType, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged ) : base( sourceObjectType, isTwoWayBinding, notifyPropertyChanged )
        {
        }

        public abstract Awaitable CallAsync( IReadOnlyList<SerializableParam> paramz );
    }
}