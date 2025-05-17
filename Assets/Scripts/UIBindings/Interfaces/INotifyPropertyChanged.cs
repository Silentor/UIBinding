using System;

namespace UIBindings
{
    public interface INotifyPropertyChanged
    {
        public event Action<Object, String> PropertyChanged;
    }
}