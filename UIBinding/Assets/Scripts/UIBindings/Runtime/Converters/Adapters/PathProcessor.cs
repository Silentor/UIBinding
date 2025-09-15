using System;
using System.Reflection;
using UIBindings.Runtime.Utils;
using UnityEngine.Assertions;

namespace UIBindings.Adapters
{
    /// <summary>
    /// Helps to parse property path, create access infrastructure
    /// </summary>
    public struct PathProcessor
    {
        public Boolean IsComplexPath => _propertyPath.IndexOf( '.' ) >= 0;                         

        public bool IsFirstPart => _start == 0;

        public bool IsLastPart => _end == _propertyPath.Length;

        public bool IsNextToLastPart => !IsLastPart && _propertyPath.IndexOf( '.', _end + 1 ) == -1;

        /// <summary>
        /// Current parsed part
        /// </summary>
        public string CurrentPartName { get; private set; }  

        public PathAdapter CurrentAdapter => _lastAdapter;

        public Type CurrentOutputType => _lastAdapter?.OutputType;

        public PathProcessor( object sourceObject, string propertyPath, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged )
        {
            _sourceObject = sourceObject;
            _propertyPath = propertyPath;
            _isTwoWayBinding = isTwoWayBinding;
            _notifyPropertyChanged = notifyPropertyChanged;
            _currentSourceType = sourceObject.GetType();
            _start = -1;
            _end  = -1;
            CurrentPartName = string.Empty;
            _lastAdapter = null;
        }

        public bool MoveNext( )
        {
            if ( _end >= _propertyPath.Length )
                return false;

            _start = _end + 1;
            _end   = _propertyPath.IndexOf('.', _start);
            if (_end == -1)
                _end = _propertyPath.Length;

            CurrentPartName     = _propertyPath.Substring(_start, _end - _start);
            var propertyInfo = _currentSourceType.GetProperty( CurrentPartName );
            if ( propertyInfo != null )
            {
                if( _lastAdapter != null )
                    _lastAdapter = PathAdapter.GetPropertyAdapter( _lastAdapter, propertyInfo, _isTwoWayBinding, _notifyPropertyChanged );
                else
                    _lastAdapter = PathAdapter.GetPropertyAdapter( _sourceObject, propertyInfo, _isTwoWayBinding, _notifyPropertyChanged );
                _currentSourceType = propertyInfo.PropertyType;
            }
            else
            {
                var methodInfo =  _currentSourceType.GetMethod( CurrentPartName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
                if ( methodInfo != null )   //Read value func or call method
                {
                    if( _lastAdapter != null )
                        _lastAdapter = PathAdapter.GetMethodAdapter( _lastAdapter, methodInfo, _isTwoWayBinding, _notifyPropertyChanged );
                    else
                        _lastAdapter = PathAdapter.GetMethodAdapter( _sourceObject, methodInfo, _isTwoWayBinding, _notifyPropertyChanged );
                }
                else
                {
                    Assert.IsNotNull( propertyInfo, $"Member '{CurrentPartName}' not found in type '{_currentSourceType}'" );
                }

                // Process Func<> and fields and indexers
                
            }

            return _start < _end;
        }

        private readonly object _sourceObject;
        private readonly string _propertyPath;
        private readonly bool _isTwoWayBinding;
        private readonly Action<object, string> _notifyPropertyChanged;

        private int             _start;
        private int             _end;
        private Type            _currentSourceType;

        private PathAdapter _lastAdapter;
    }
}