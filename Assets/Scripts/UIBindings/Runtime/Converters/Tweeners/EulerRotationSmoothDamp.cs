using System;
using System.Threading;
using UnityEngine;

namespace UIBindings.Tweeners
{
    /// <summary>
    /// Just smooths current value to target value.
    /// </summary>
    public class EulerRotationSmoothDamp : ConverterBase<Vector3, Vector3>, IDataReader<Vector3>
    {
        [Range(0.001f, 1f)]
        public float SmoothTime = 0.3f;

        public override Boolean IsTwoWay => false;

        public override ConverterBase GetReverseConverter( )
        {
            throw new NotImplementedException();
        }

        public EResult TryGetValue(out Vector3 value )
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
                    _velo = Vector3.zero;
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

        private static bool AlmostEquals(Vector3 angle1, Vector3 angle2, float relativeTolerance = 0.0005f, float absoluteTolerance = 0.0001f)
        {
            if (angle1 == angle2) return true;

            var x = Mathf.DeltaAngle(angle1.x, angle2.x);
            var y = Mathf.DeltaAngle(angle1.y, angle2.y);
            var z = Mathf.DeltaAngle(angle1.z, angle2.z);
            float diff = Math.Max( Math.Max( Math.Abs( x ), Math.Abs( y ) ), Math.Abs( z ));
            float tolerance = Math.Max(relativeTolerance * diff, absoluteTolerance);
            return diff <= tolerance;
        }

        private bool _isInited;
        private Vector3 _targetValue;
        private Vector3 _currentValue;
        private Vector3 _velo;
        private Awaitable _tweenAwaitable;

        private async Awaitable SmoothDampAwaitable( CancellationToken cancellationToken )
        {
            while ( !(AlmostEquals(_currentValue, _targetValue) || cancellationToken.IsCancellationRequested) ) 
            {
                var deltaTime = GetDeltaTime();
                _currentValue.x = Mathf.SmoothDampAngle( _currentValue.x, _targetValue.x, ref _velo.x, SmoothTime, Mathf.Infinity, deltaTime );
                _currentValue.y = Mathf.SmoothDampAngle( _currentValue.y, _targetValue.y, ref _velo.y, SmoothTime, Mathf.Infinity, deltaTime );
                _currentValue.z = Mathf.SmoothDampAngle( _currentValue.z, _targetValue.z, ref _velo.z, SmoothTime, Mathf.Infinity, deltaTime );
                await Awaitable.NextFrameAsync(  );
            }

            if( !cancellationToken.IsCancellationRequested )
            {
                _currentValue = _targetValue; //Ensure we reach target value
            }
        }
    }
}