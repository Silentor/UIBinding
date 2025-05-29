using System;
using System.Threading;
using UnityEngine;

namespace UIBindings.Tweeners
{
    /// <summary>
    /// Just smooths current value to target value.
    /// </summary>
    public class FloatSmoothDamp : ConverterBase<float, float>, IDataReader<float>
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
                    _isInited = true;
                    value = sourceValue;
                    return EResult.Changed;
                }

                //Work as usual, but no need to tween
                if ( AlmostEquals( sourceValue, _currentValue ) )
                {
                    value = sourceValue;
                    return EResult.Changed;
                }

                //New value is definitely distinct, start tween
                _targetValue = sourceValue;
                value = _currentValue;

                //If no active tween, start new one
                if ( _tweenAwaitable == null )
                    _tweenAwaitable = SmoothDampAwaitable( CancellationToken.None );
                else if ( _tweenAwaitable.IsCompleted )
                {
                    _tweenAwaitable.GetAwaiter().GetResult();
                    _tweenAwaitable = SmoothDampAwaitable( CancellationToken.None );
                }

                return EResult.Tweened;
            }
            //Source not changed but we continue tween
            else if ( result == EResult.NotChanged )
            {
                //Check tweening state
                if ( _tweenAwaitable != null )
                {
                    if ( _tweenAwaitable.IsCompleted )
                    {
                        _tweenAwaitable.GetAwaiter().GetResult();
                        _tweenAwaitable = null;
                    }
                    value = _currentValue;
                    return EResult.Tweened;
                }
                else
                {
                    value = default;
                    return EResult.NotChanged;
                }
            }
            //Source tweened itself?
            else
            {
                //Propagate tween state. Consider should we tween on tweened source? But we cannot detect start of tweening and calculate proper deltaTime
                value  = sourceValue;
                return EResult.Tweened;
            }
        }

        private static bool AlmostEquals(float a, float b, float relativeTolerance = 0.0005f, float absoluteTolerance = 0.0001f)
        {
            if (a == b) return true;
            if (float.IsNaN(a)      || float.IsNaN(b)) return false;
            if (float.IsInfinity(a) || float.IsInfinity(b)) return false;

            float diff = Math.Abs(a - b);
            float tolerance = Math.Max(relativeTolerance * Math.Max(Math.Abs(a), Math.Abs(b)), absoluteTolerance);
            return diff <= tolerance;
        }

        private bool _isInited;
        private float _targetValue;
        private float _currentValue;
        private float _velo;
        private Awaitable _tweenAwaitable;

        private async Awaitable SmoothDampAwaitable( CancellationToken cancellationToken )
        {
            while ( !(AlmostEquals(_currentValue, _targetValue) || cancellationToken.IsCancellationRequested) ) 
            {
                _currentValue = Mathf.SmoothDamp( _currentValue, _targetValue, ref _velo, SmoothTime, Mathf.Infinity, Time.deltaTime );
                await Awaitable.NextFrameAsync(  );
            }

            if( !cancellationToken.IsCancellationRequested )
            {
                _currentValue = _targetValue; //Ensure we reach target value
            }
        }
    }
}