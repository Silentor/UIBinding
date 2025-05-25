using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public class TestMonoBehSource : MonoBehaviour//, INotifyPropertyChanged 
    {
        public Sprite TestSprite; 

        private Single  _sourceFloat;
        private bool    _targetBool = false;
        private int _sourceInt = 5;

        public Single SourceFloat
        {
            get => _sourceFloat;
            set => SetField( ref _sourceFloat, value );
        }

        public Boolean TargetBool
        {
            get => _targetBool;
            set
            {
                //var oldValue = _targetBool;
                SetField( ref _targetBool, value );
                //Debug.Log( $"Changed bool from {oldValue} to {value}" );
            }
        }

        public int SourceInt
        {
            get => _sourceInt;
            set => SetField( ref _sourceInt, value );
        }

        public Sprite SourceSprite => TestSprite;

        public void CallMe( )
        {
            TargetBool = !TargetBool;
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallMe)}] " );
        }

        public async Awaitable CallMeAsync( )
        {
            await Awaitable.WaitForSecondsAsync( 1f );
            TargetBool = !TargetBool;
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallMeAsync)}] " );
        }

        public async Task CallMeAsyncTask( )
        { 
            await Task.Delay( 1000 );
            TargetBool = !TargetBool;
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallMeAsyncTask)}] " );
        }

        public async ValueTask CallMeAsyncVTask( )
        {
            await Task.Delay( 1000 );
            TargetBool = !TargetBool;
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallMeAsyncVTask)}] " );
        }

        public async UniTask CallMeAsyncUniTask( )
        { 
            await Task.Delay( 1000 );
            TargetBool = !TargetBool;
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallMeAsyncUniTask)}] " );
        }

        public async UniTaskVoid CallMeAsyncUniTaskVoid( )
        {
            await Task.Delay( 1000 );
            TargetBool = !TargetBool;
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallMeAsyncUniTaskVoid)}] " );
        }

        public async void CallMeAsyncVoid( )
        {
            await Task.Delay( 1000 );
            TargetBool = !TargetBool;
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallMeAsyncVoid)}] " );
        }

        private void Start( )
        {
            OnPropertyChanged( null );          //Update all binders one time TODO consider some non manual way for init View
        }

        void Update()
        {
            if( Time.frameCount % 2 == 0 )            //Test notifications
            {
                SourceFloat  = Time.time;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] String propertyName = null)
        {
            //PropertyChanged?.Invoke( this, propertyName );
        }

         protected Boolean SetField<T>(ref T field, T value, [CallerMemberName] String propertyName = null)
         {
             if ( EqualityComparer<T>.Default.Equals( field, value ) ) return false;
             field = value;
             OnPropertyChanged( propertyName );
             return true;
         }

        public event Action<Object, String> PropertyChanged;

    }
}
