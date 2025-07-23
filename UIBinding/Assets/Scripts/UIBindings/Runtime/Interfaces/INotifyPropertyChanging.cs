using System;

namespace UIBindings
{
    public interface INotifyPropertyChanging
    {
        public event Action<Object, String> PropertyChanging;
    }
}