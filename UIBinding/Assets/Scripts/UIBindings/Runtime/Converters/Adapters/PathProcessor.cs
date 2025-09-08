using System;
using System.Reflection;
using UnityEngine.Assertions;

namespace UIBindings.Adapters
{
    /// <summary>
    /// Helps to parse property path, create access infrastructure
    /// </summary>
    public struct PathProcessor
    {
        public Boolean IsComplexPath => _propertyPath.IndexOf( '.' ) >= 0;                         

        /// <summary>
        /// Current parsed part
        /// </summary>
        public string PartName { get; private set; }  


        public bool IsFirstPart => _start == 0;

        public bool IsLastPart => _end == _propertyPath.Length;

        public PathProcessor( object sourceObject, string propertyPath, bool isTwoWayBinding, Action<object, string> notifyPropertyChanged )
        {
            _sourceObject = sourceObject;
            _propertyPath = propertyPath;
            _isTwoWayBinding = isTwoWayBinding;
            _notifyPropertyChanged = notifyPropertyChanged;
            _currentSourceType = sourceObject.GetType();
            _start = -1;
            _end  = -1;
            PartName = string.Empty;
            _lastAdapter = null;
        }

        public bool MoveNext( out PathAdapter pathPartAdapter )
        {
            if ( _end == _propertyPath.Length )
            {
                pathPartAdapter = null;
                return false;
            }
            _start = _end + 1;
            _end   = _propertyPath.IndexOf('.', _start);
            if (_end == -1)
                _end = _propertyPath.Length;

            PartName     = _propertyPath.Substring(_start, _end - _start);
            var propertyInfo = _currentSourceType.GetProperty( PartName );
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
                // Process Func<> and fields and indexers

                Assert.IsNotNull( propertyInfo, $"Member '{PartName}' not found in type '{_currentSourceType}'" );
            }

            pathPartAdapter = _lastAdapter;

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