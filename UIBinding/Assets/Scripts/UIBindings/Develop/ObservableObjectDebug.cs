using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public class ObservableObjectDebug : INotifyPropertyChanging, INotifyPropertyChanged
    {
        protected bool SetProperty<T>(/*[NotNullIfNotNull( nameof(newValue) )]*/ ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
                return false;

            OnPropertyChanging(propertyName);
            field = newValue;
            OnPropertyChanged(propertyName);

            return true;
        }

        protected bool SetProperty<T>(/*[NotNullIfNotNull(nameof(newValue))]*/ ref T field, T newValue, IEqualityComparer<T> comparer, [CallerMemberName] string? propertyName = null)
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
            _propertyChanging?.Invoke( this, propertyName );
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            _propertyChanged?.Invoke( this, propertyName );
        }


        public event Action<Object, String> PropertyChanging
        {
            add
            {
                _propertyChanging = (Action<object, string>)Delegate.Combine( _propertyChanging, value );
                Debug.Log( $"[ObservableObjectDebug]-[{GetType().Name}] PropertyChanging subscripted, count {_propertyChanging.GetInvocationList().Length}" );
            }
            remove
            {
                _propertyChanging = (Action<object, string>)Delegate.Remove( _propertyChanging, value );
                var subsCount = _propertyChanging?.GetInvocationList().Length ?? 0;
                Debug.Log( $"[ObservableObjectDebug]-[{GetType().Name}] PropertyChanging unsubscripted, count {subsCount}" );
            }
        }

        public event Action<Object, String> PropertyChanged
        {
            add
            {
                _propertyChanged = (Action<object, string>)Delegate.Combine( _propertyChanged, value );
                Debug.Log( $"[ObservableObjectDebug]-[{GetType().Name}] PropertyChanged subscripted, count {_propertyChanged.GetInvocationList().Length}" );
            }
            remove
            {
                _propertyChanged = (Action<object, string>)Delegate.Remove( _propertyChanged, value );
                var subsCount = _propertyChanged?.GetInvocationList().Length ?? 0;
                Debug.Log( $"[ObservableObjectDebug]-[{GetType().Name}] PropertyChanged unsubscripted, count {subsCount}" );
            }
        }

        ~ObservableObjectDebug( )
        {
            if(PropertyChangedCount > 0)
                Debug.LogError( $"[ObservableObjectDebug]-[{GetType().Name}] PropertyChanged has {PropertyChangedCount} subscribers at finalization" );
            if(PropertyChangingCount > 0)
                Debug.LogError( $"[ObservableObjectDebug]-[{GetType().Name}] PropertyChanging has {PropertyChangingCount} subscribers at finalization" );
        }

        public int PropertyChangingCount => _propertyChanging?.GetInvocationList().Length ?? 0;

        public int PropertyChangedCount => _propertyChanged?.GetInvocationList().Length ?? 0;

        private Action<Object, string> _propertyChanging;
        private Action<Object, string> _propertyChanged;
    }
}