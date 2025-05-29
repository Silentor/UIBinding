using System;
using UnityEngine;

namespace UIBindings.Tweeners
{
    /// <summary>
    /// Doesnt need to know from and to value, just smooths current value to target value.
    /// </summary>
    public class SmoothDamp : ConverterBase<float, float>, IDataReader<float>
    {
        [Range(0.001f, 5f)]
        public float SmoothTime = 0.3f;

        public override Boolean IsTwoWay => false;

        public override ConverterBase GetReverseConverter( )
        {
            throw new NotImplementedException();
        }

        public EResult TryGetValue(out float value )
        {
            var result = _prev.TryGetValue( out var sourceValue );

            //Get new target
            if ( result == EResult.Changed )
            {
                if( !_isInited )
                {
                    //Initialize state without tween because we have no previous value
                    _currentValue = sourceValue;
                    _targetValue = sourceValue;
                    _velo = 0f;
                    _time = Time.timeAsDouble;
                    _isInited = true;
                    value = sourceValue;
                    return EResult.Changed;
                }

                //Work as usual, but no need to tween
                if ( Mathf.Approximately( sourceValue, _currentValue ) )
                {
                    value = sourceValue;
                    return EResult.Changed;
                }

                //New value is definitely distinct, start tween
                _targetValue = sourceValue;
                _time = Time.timeAsDouble;
                value = _currentValue;
                return EResult.Tweened;
            }
            //Source not changed but we continue tween
            else if ( result == EResult.NotChanged )
            {
                //Stop tween if target is reached
                if ( Math.Abs( _targetValue - _currentValue ) < 0.0001f )
                {
                    value = sourceValue;
                    return EResult.NotChanged;
                }

                //Tweening in progress
                var deltaTime = Time.timeAsDouble - _time;
                _currentValue = Mathf.SmoothDamp( _currentValue, _targetValue, ref _velo, SmoothTime, Mathf.Infinity, (float)deltaTime );

                //Return target if target is reached as a last value
                if ( Math.Abs( _targetValue - _currentValue ) < 0.0001f )
                {
                    _currentValue = _targetValue;
                    value = _targetValue;
                    return EResult.Tweened;
                }

                value = _currentValue;
                return EResult.Tweened;
            }
            //Source tweened itself?
            else
            {
                //Propagate tween state. Consider should we tween on tweened source? But we cannot detect start of tweening and calculate proper deltaTime
                value  = sourceValue;
                return EResult.Tweened;
            }
        }

        private bool _isInited;
        private float _targetValue;
        private float _currentValue;
        private float _velo;
        private double _time;
    }
}