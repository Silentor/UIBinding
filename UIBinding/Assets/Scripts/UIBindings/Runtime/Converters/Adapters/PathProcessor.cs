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
        public Boolean IsComplexPath => _path.IndexOf( '.' ) >= 0;                         

        public bool IsFirstPart => _start == 0;

        public bool IsLastPart => _end == _path.Length;

        public bool IsNextToLastPart => !IsLastPart && _path.IndexOf( '.', _end + 1 ) == -1;

        /// <summary>
        /// Current parsed part
        /// </summary>
        public string CurrentPartName { get; private set; }  

        public PathAdapter CurrentAdapter => _lastAdapter;

        public Type CurrentOutputType => _lastAdapter?.OutputType;

        public PathProcessor( Type sourceObjectType, string path, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged )
        {
            _path = path;
            _isTwoWayBinding = isTwoWayBinding;
            _notifyPropertyChanged = notifyPropertyChanged;
            _sourceObjectType = sourceObjectType;
            _start = -1;
            _end  = -1;
            CurrentPartName = string.Empty;
            _lastAdapter = null;
            _currentSourceType = sourceObjectType;
        }

        public bool MoveNext( )
        {
            if ( _end >= _path.Length )
                return false;

            _start = _end + 1;
            _end   = _path.IndexOf('.', _start);
            if (_end == -1)
                _end = _path.Length;

            CurrentPartName     = _path.Substring(_start, _end - _start);
            var propertyInfo = _currentSourceType.GetProperty( CurrentPartName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
            if ( propertyInfo != null )
            {
                if( _lastAdapter != null )
                    _lastAdapter = PathAdapter.GetPropertyAdapter( _lastAdapter, propertyInfo, _isTwoWayBinding, _notifyPropertyChanged );
                else
                {
                    _lastAdapter = PathAdapter.GetPropertyAdapter( _sourceObjectType, propertyInfo, _isTwoWayBinding, _notifyPropertyChanged );
                }
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
                        _lastAdapter = PathAdapter.GetMethodAdapter( _sourceObjectType, methodInfo, _isTwoWayBinding, _notifyPropertyChanged );
                }
                else
                {
                    Assert.IsNotNull( propertyInfo, $"Member '{CurrentPartName}' not found in type '{_currentSourceType}'" );
                }

                // Process Func<> and fields and indexers
                
            }

            return _start < _end;
        }

        public PropertyInfo PeekNextPropertyInfo( )
        {
            if ( _end >= _path.Length )
                return null;

            var nextStart = _end + 1;
            var nextEnd   = _path.IndexOf('.', nextStart);
            if (nextEnd == -1)
                nextEnd = _path.Length;

            var nextPartName = _path.Substring(nextStart, nextEnd - nextStart);
            var propertyInfo = _currentSourceType.GetProperty( nextPartName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
            return propertyInfo;
        }

        private readonly Type   _sourceObjectType;
        private readonly string _path;
        private readonly bool _isTwoWayBinding;
        private readonly Action<object, string> _notifyPropertyChanged;

        private int             _start;
        private int             _end;
        private Type _currentSourceType;

        private PathAdapter _lastAdapter;
    }
}