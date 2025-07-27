using System;
using System.Linq;

namespace UIBindings.SourceGen
{
    /// <summary>
    /// Used on a class to generate boilerplate code for INotifyPropertyChanging interface. Must be used with <see cref="INotifyPropertyChangedAttribute"/>.
    /// Without it, no code  will be generated
    /// Used when its not possible to subclass <see cref="ObservableObject"/>
    /// </summary>
    //[AttributeUsage(AttributeTargets.Class, Inherited = false)]
    //public sealed class INotifyPropertyChangingAttribute : Attribute
    //{
    //}
}