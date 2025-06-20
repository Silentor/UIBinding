using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UIBindings.Runtime;
using UIBindings.Runtime.PlayerLoop;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Profiling;
using Object = System.Object;

namespace UIBindings
{
    public class TestMonoBehSource : MonoBehaviour//, INotifyPropertyChanged
    {
        public GameObject DelayedCanvas; //For testing delayed canvas creation
        public Sprite TestSprite; 

        private Single  _sourceFloat;
        private bool    _targetBool = true;
        private int _sourceInt = 5;
        private EventType _sourceEnum = EventType.MouseMove;
        private string _sourceString = "Test string";

        private Func<StructEnum> _fastGetter;
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

        public EventType SourceEnum
        {
            get => _sourceEnum;
            set => _sourceEnum = value;
        }

        public String SourceString
        {
            get => _sourceString;
            set
            {
                _sourceString = value;
                OnPropertyChanged(  );
            }
        }

        //Should be read by any int binding
        public byte SourceByte
        {
            get => (byte)_sourceEnum;
            set
            {
                _sourceEnum = (EventType)value;
                //Debug.Log( $"{nameof(SourceByte)} property is modified to {value}" );
            }
        }

        public String[] Options { get; } = new[] { "One", "Two", "Three" };


        

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

            var newValue = ((int)_sourceEnum + 1) % 16;
            _sourceEnum = (EventType)newValue;
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
            Application.targetFrameRate = 30;

            //EnumerateProfilerStats();

            // if ( DelayedCanvas )
            // {
            //     StartCoroutine( DelayedCanvasEnable() );
            // }
            // else
            // {
            //     OnPropertyChanged( null );          //Update all binders one time TODO consider some non manual way for init View
            // }

            //UpdateManager.RegisterUpdate( TestUpdate );
            //UpdateManager.RegisterUpdate( TestUpdate2 );
            //UpdateManager.RegisterUpdate( TestUpdate3 );
        }

        struct StatInfo
        {
            public ProfilerCategory       Cat;
            public string                 Name;
            public ProfilerMarkerDataUnit Unit;
        }

        static void EnumerateProfilerStats()
        {
            var availableStatHandles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(availableStatHandles);

            var availableStats = new List<StatInfo>(availableStatHandles.Count);
            foreach (var h in availableStatHandles)
            {
                var statDesc = ProfilerRecorderHandle.GetDescription(h);
                var statInfo = new StatInfo()
                               {
                                       Cat  = statDesc.Category,
                                       Name = statDesc.Name,
                                       Unit = statDesc.UnitType
                               };
                availableStats.Add(statInfo);
            }
            availableStats.Sort((a, b) =>
            {
                var result = string.Compare(a.Cat.ToString(), b.Cat.ToString());
                if (result != 0)
                    return result;

                return string.Compare(a.Name, b.Name);
            });

            var sb = new StringBuilder("Available stats:\n");
            foreach (var s in availableStats)
            {
                //sb.AppendLine($"{s.Cat}\t\t - {s.Name}\t\t - {s.Unit}");
                Debug.Log($"{s.Cat}\t\t - {s.Name}\t\t - {s.Unit}");
            }

            //Debug.Log(sb.ToString());
        }

        private void TestUpdate3( )
        {
            
        }

        private void TestUpdate2( )
        {
            UpdateManager.UnregisterUpdate( TestUpdate2 );

        }

        private void TestUpdate( )
        {
            

        }

        private IEnumerator DelayedCanvasEnable( )
        {
            yield return new WaitForSeconds( 1f );
            DelayedCanvas.SetActive( true );
            OnPropertyChanged( null );          //Update all binders one time TODO consider some non manual way for init View
            yield return null;yield return null;yield return null;
            SourceByte += 1; 
            OnPropertyChanged( null );
            yield return null;yield return null;yield return null;
            SourceByte += 1;
            OnPropertyChanged( null );
            yield return null;yield return null;yield return null;
            SourceByte += 1;
            OnPropertyChanged( null );
            yield return null;
            OnPropertyChanged( null );
        }

        void Update()
        {
            if( Time.frameCount % 2 == 0 )            //Test notifications
            {
                SourceFloat  = Time.time % 3f;
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
}
