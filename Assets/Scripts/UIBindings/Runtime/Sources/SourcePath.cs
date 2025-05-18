using System;
using System.Reflection;
using UnityEngine;
using Object = System.Object;

namespace UIBindings.Runtime
{
    [Serializable]
    public struct SourcePath
    {
        public String Path;
        
        public Boolean IsAssigned => !String.IsNullOrEmpty( Path );

        public static implicit operator string(SourcePath sourcePath) => sourcePath.Path;

        private PropertyInfo _property;
    }
}