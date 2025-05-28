using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UIBindings.Runtime;
using UIBindings.Runtime.Utils;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public class TestMonoBehSource : MonoBehaviour, INotifyPropertyChanged 
    {
        public Sprite TestSprite; 

        private Single  _sourceFloat;
        private bool    _targetBool = false;
        private int _sourceInt = 5;
        private ETestEnum _sourceEnum = ETestEnum.Value2;

        private Func<IntedEnum> _fastGetter;
        private Func<Int32> _boxedGetter;


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

        public ETestEnum SourceEnum
        {
            get => _sourceEnum;
            //set => _sourceEnum = value;
        }

        //Should be read by any int binding
        public byte SourceByte
        {
            get => (byte)_sourceEnum;
            set
            {
                _sourceEnum = (ETestEnum)value;
                //Debug.Log( $"{nameof(SourceByte)} property is modified to {value}" );
            }
        }

        public void CallMe( )
        {
            //var timer = System.Diagnostics.Stopwatch.StartNew();
            var i = _fastGetter();  //Read enum property directly to int without boxing
            //timer.Stop();
            //Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallMe)}] Getter took {timer.Elapsed.TotalMicroseconds()} mks, value={i}" );

            //timer.Restart();
            //var j = _boxedGetter();
            //timer.Stop();
            //Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallMe)}] Boxed Getter took {timer.Elapsed.TotalMicroseconds()} mks, value={j}" );

            //Debug.Log( i );

            //throw new OperationCanceledException( "Test exception" );
            //throw new Exception( "Test exception" );

            var newValue = ((int)_sourceEnum + 1) % 3;
            _sourceEnum = (ETestEnum)newValue;
            //Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallMe)}] " );

            OnPropertyChanged( nameof(SourceByte) );
        }

        public async Awaitable CallMeAsync( )
        {
            await Awaitable.WaitForSecondsAsync( 1f );
            TargetBool = !TargetBool;
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallMeAsync)}] " );
        }

        public async Awaitable CallMeAsyncInt( int value )
        {
            await Awaitable.WaitForSecondsAsync( 1f );
            TargetBool = !TargetBool;
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallMeAsyncInt)}] {value}" );
        }

        public async Awaitable CallMeAsyncFloat( float value )
        {
            await Awaitable.WaitForSecondsAsync( 1f );
            TargetBool = !TargetBool;
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallMeAsyncFloat)}] {value}" );
        }

        public async Awaitable CallMeAsync2Params( float value, String value2 )
        {


            await Awaitable.WaitForSecondsAsync( 1f );
            //TargetBool = !TargetBool;
 
            throw new OperationCanceledException( "Test exception" );
            throw new Exception( "Test exception" );

            
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallMeAsync2Params)}] {value}, {value2}" );
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

        public void CallParamInt( int value )
        {
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallParamInt)}] value={value}" );
        }

        public void CallParamBool( bool value )
        {
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallParamBool)}] value={value}" );
        }

        public void CallParamFloat( float value )
        {
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallParamFloat)}] value={value}" );
        }

        public void CallParamString( String value )
        {
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallParamString)}] value={value}" );
        }

        public void CallParamStr2( string value1, string value2 )
        {
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallParamStr2)}] value={value1}, value2={value2}" );
        }

        public void CallParamInt2( int value1, int value2 )         //Will box
        {
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallParamInt2)}] value={value1}, value2={value2}" );
        }


        public void CallParamTextureBool( Texture2D value1, bool value2 )
        {
            Debug.Log( $"[{nameof(TestMonoBehSource)}]-[{nameof(CallParamTextureBool)}] value={value1.format}, value2={value2}" );
        }


        private void Start( )
        {
            OnPropertyChanged( null );          //Update all binders one time TODO consider some non manual way for init View

            var enumProp = GetType().GetProperty( nameof(SourceEnum), BindingFlags.Public | BindingFlags.Instance );
            var getter = enumProp.GetGetMethod( true );
            var getterInt = ConstructFunc1( this, getter );
            _fastGetter = getterInt;

            var boxedGetter = Delegate.CreateDelegate( typeof(Func<>).MakeGenericType(enumProp.PropertyType), this, getter );
            Func<int> boxedGetterToEnum = ( ) => (int)boxedGetter.DynamicInvoke(  );
            _boxedGetter = boxedGetterToEnum;
        }

        //Construct 1 params instance func delegate with boxing
        private static Func<IntedEnum> ConstructFunc1( Object source, MethodInfo method )
        {
            var type1               = method.ReturnType;
            var convertMethod       = typeof(TestMonoBehSource).GetMethod( nameof( ConvertFunc1 ), BindingFlags.NonPublic | BindingFlags.Static );
            var closedConvertMethod = convertMethod.MakeGenericMethod( type1 );
            var result              = (Func<IntedEnum>)closedConvertMethod.Invoke( null, new [] { source, method } );
            return result;
        }

        private static Func<IntedEnum> ConvertFunc1<TEnum>( Object source, MethodInfo method ) where TEnum : struct, Enum, IConvertible //where TNumeric : struct, IConvertible
        {
            var enm = (Func<TEnum>) Delegate.CreateDelegate( typeof(Func<TEnum>), source, method );
            Func<IntedEnum> num  = () => new IntedEnum(UnsafeUtility.EnumToInt( enm( )), typeof(TEnum));
            return num;
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
            PropertyChanged?.Invoke( this, propertyName );
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

    public enum ETestEnum
    {
        Value1,
        Value2,
        Value3
    }
}
