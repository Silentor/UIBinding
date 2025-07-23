using System;

namespace UIBindings.SourceGen
{
    /// <summary>
    /// Converts a field into an observable property. Generates public getter and setter with change check and notify event firing.
    /// Needs to be used with ObservableObject or alike object that implements INotifyPropertyChanged (optionally INotifyPropertyChanging).
    /// </summary>
    [AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ObservablePropertyAttribute : System.Attribute
    {
    }
}