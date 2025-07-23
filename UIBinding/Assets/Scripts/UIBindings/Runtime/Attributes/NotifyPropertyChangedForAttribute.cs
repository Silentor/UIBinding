using System;
using System.Linq;

namespace UIBindings.SourceGen
{
    /// <summary>
    /// Should be used with ObservablePropertyAttribute. When decorated property is changed, also notifies about the change of the properties specified in this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public sealed class NotifyPropertyChangedForAttribute : Attribute
    {
        public readonly string[] PropertyNames;

        public NotifyPropertyChangedForAttribute(string propertyName)
        {
            PropertyNames = new[] { propertyName };
        }

        public NotifyPropertyChangedForAttribute(string propertyName, params string[] otherPropertyNames)
        {
            PropertyNames = new[] { propertyName }.Concat(otherPropertyNames).ToArray();
        }
    }
}