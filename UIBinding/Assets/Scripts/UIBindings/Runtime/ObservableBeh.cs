using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public class ObservableBeh : MonoBehaviour, INotifyPropertyChanging, INotifyPropertyChanged
    {
        protected bool SetProperty<T>([NotNullIfNotNull( nameof(newValue) )] ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
                return false;

            OnPropertyChanging(propertyName);
            field = newValue;
            OnPropertyChanged(propertyName);

            return true;
        }

        protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, IEqualityComparer<T> comparer, [CallerMemberName] string? propertyName = null)
        {
            Assert.IsNotNull( comparer );

            if (comparer.Equals(field, newValue))
                return false;

            OnPropertyChanging(propertyName);
            field = newValue;
            OnPropertyChanged(propertyName);

            return true;
        }

        protected bool SetProperty<TModel, T>(T oldValue, T newValue, TModel model, Action<TModel, T> callback, [CallerMemberName] string propertyName = null )
                where TModel : class
        {
            Assert.IsNotNull( model );
            Assert.IsNotNull( callback );

            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
                return false;

            OnPropertyChanging(propertyName);
            callback(model, newValue);
            OnPropertyChanged(propertyName);

            return true;
        }

        protected bool SetProperty<TModel, T>(T oldValue, T newValue, IEqualityComparer<T> comparer, TModel model, Action<TModel, T> callback, [CallerMemberName] string propertyName = null )
                where TModel : class
        {
            Assert.IsNotNull( model );
            Assert.IsNotNull( callback );
            Assert.IsNotNull( comparer );

            if (comparer.Equals(oldValue, newValue))
                return false;

            OnPropertyChanging(propertyName);
            callback(model, newValue);
            OnPropertyChanged(propertyName);

            return true;
        }

        protected virtual void OnPropertyChanging([CallerMemberName] string propertyName = null)
        {
            PropertyChanging?.Invoke( this, propertyName );
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke( this, propertyName );
        }


        public event Action<Object, String> PropertyChanging;
        public event Action<Object, String> PropertyChanged;
    }
}