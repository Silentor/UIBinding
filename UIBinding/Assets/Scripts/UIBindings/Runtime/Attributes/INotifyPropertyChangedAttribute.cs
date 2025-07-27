using System;
using System.Linq;

namespace UIBindings.SourceGen
{
    /// <summary>
    /// Used on a class to generate boilerplate code for INotifyPropertyChanged interface. Used when its not possible to subclass <see cref="ObservableObject"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class INotifyPropertyChangedAttribute : Attribute
    {
    }
}