using System;
using System.Threading;
using Colors;
using UnityEngine;
using Object = System.Object;

namespace UIBindings.Tweeners
{
    public class ColorTween : ConverterBase<bool, Color>, IDataReader<Color>
    {
        public Color  From      = Color.red;
        public Color  To        = Color.green;
        public Single TweenTime = 1;

        public override Boolean IsTwoWay => false;

        public EResult TryGetValue(out Color value )
        {
            var result = _prev.TryGetValue( out var newState );

            //Get new target
            if ( result == EResult.Changed )
            {
                var convertedValue = newState ? To : From;
                if ( !_isInited )
                {
                    //Initialize state without tween
                    _isInited     = true;
                    _targetState  = newState;
                    value = convertedValue;
                    return EResult.Changed;
                }

                //New value is definitely distinct, start tween
                if ( newState != _targetState  )
                {
                    if( _tweenAwaitable != null && !_tweenAwaitable.IsCompleted )
                    {
                        //Cancel previous tween if it is still in progress
                        _cancellationTokenSource?.Cancel();
                    }

                    _cancellationTokenSource = new CancellationTokenSource();
                    _tweenAwaitable = Tween( _targetState ? To : From, convertedValue, TweenTime, _cancellationTokenSource.Token );
                    _targetState = newState;
                    value        = _currentValue;
                    return EResult.Tweened;
                }
                else
                {
                    value = default;
                    return EResult.NotChanged;
                }
            }
            //Source not changed but we continue tween
            else if ( result == EResult.NotChanged )
            {
                if ( _tweenAwaitable != null )
                {
                    value = _currentValue;
                    if ( _tweenAwaitable.IsCompleted )
                    {
                        _tweenAwaitable.GetAwaiter().GetResult();       //Release awaitable
                        _tweenAwaitable = null;             //Do not animate next frame
                    }
                    return EResult.Tweened;
                }
                else
                {
                    value = _currentValue;
                    return EResult.NotChanged;
                }
            }
            //Source tweened itself?
            else
            {
                //Propagate tween state. Consider should we tween on tweened source?
                value  = newState ? To : From;
                return EResult.Tweened;
            }
        }

        private Boolean _isInited;
        private Color   _currentValue;
        private bool   _targetState;
        private Awaitable _tweenAwaitable;
        private CancellationTokenSource _cancellationTokenSource;

        private async Awaitable Tween( Color from, Color to, Single duration, CancellationToken cancel )
        {
            _currentValue = from;
            var playTime = 0f;
            var fromHSV = from.ToHSV();
            var toHSV = to.ToHSV();

            while ( playTime <= duration && !cancel.IsCancellationRequested )
            {
                var deltaTime = GetDeltaTime();
                playTime      += deltaTime;
                var currentHSV = ColorHSV.LerpHSV( fromHSV, toHSV, playTime / duration );
                _currentValue =  currentHSV;
                await Awaitable.NextFrameAsync( );
            }

            if( !cancel.IsCancellationRequested )
            {
                _currentValue = to;
            }
        }

        private static bool IsApproximatelyEqual(Color a, Color b)
        {
            var diff = a - b;
            return Math.Abs( diff.r ) < 0.0001f &&
                   Math.Abs( diff.g ) < 0.0001f &&
                   Math.Abs( diff.b ) < 0.0001f &&
                   Math.Abs( diff.a ) < 0.0001f;
        }
    }
}