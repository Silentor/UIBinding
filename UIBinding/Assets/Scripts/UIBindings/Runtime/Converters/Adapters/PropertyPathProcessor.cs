using System;
using System.Reflection;
using UnityEngine.Assertions;

namespace UIBindings.Adapters
{
    public struct PropertyPathProcessor
    {
        private readonly Object _sourceObject;
        private readonly String _propertyPath;

        private int _start;
        private int _end;
        private Type _currentSourceType;
        private PropertyAdapter _lastPropertyAdapter;

        public Boolean IsComplexPath => _propertyPath.IndexOf( '.' ) >= 0;

        public PropertyInfo CurrentPropertyInfo { get; private set; }

        public PropertyAdapter CreatePropertyAdapter( Boolean isTwoWayBinding, Action<Object, String> notifyPropertyChanged )
        {
            if ( _start == -1 )
            {
                throw new InvalidOperationException("Call MoveNext() before creating PropertyAdapter.");
            }

            if( _start == 0 )
                _lastPropertyAdapter = PropertyAdapter.GetPropertyAdapter( _sourceObject, CurrentPropertyInfo, isTwoWayBinding, notifyPropertyChanged );
            else
                _lastPropertyAdapter = PropertyAdapter.GetInnerPropertyAdapter( _lastPropertyAdapter, CurrentPropertyInfo, isTwoWayBinding, notifyPropertyChanged );

            return _lastPropertyAdapter;
        }

        public PropertyPathProcessor( object sourceObject, string propertyPath )
        {
            _sourceObject = sourceObject;
            _propertyPath = propertyPath;
            _currentSourceType = sourceObject.GetType();
            _start = -1;
            _end  = -1;

            CurrentPropertyInfo = null;
            _lastPropertyAdapter = null;
        }

        public bool MoveNext()
        {
            if (_end == _propertyPath.Length)
                return false;
            _start = _end + 1;
            _end   = _propertyPath.IndexOf('.', _start);
            if (_end == -1)
                _end = _propertyPath.Length;

            var propertyName = _propertyPath.Substring(_start, _end - _start);
            var propInfo = _currentSourceType.GetProperty( propertyName );
            Assert.IsNotNull( propInfo, $"Property '{propertyName}' not found in type '{_currentSourceType.Name}'" );
            Assert.IsTrue( propInfo.CanRead, $"Property '{propertyName}' in type '{_currentSourceType.Name}' is not readable" );

            CurrentPropertyInfo = propInfo;
            _currentSourceType = propInfo.PropertyType;

            return _start < _end;
        }

    }
}