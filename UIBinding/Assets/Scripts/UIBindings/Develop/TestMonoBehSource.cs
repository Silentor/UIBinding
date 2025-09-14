using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UIBindings.Adapters;
using UIBindings.Runtime;
using UIBindings.Runtime.PlayerLoop;
using UIBindings.Runtime.Utils;
using UIBindings.SourceGen;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Profiling;
using UnityEngine.Search;
using Object = System.Object;

namespace UIBindings
{
    public class TestMonoBehSource : ObservableBehDebug//, INotifyPropertyChanged
    {
        public string TestStr;

        // You can specify an initial query, a search provider and some SearchViewFlags
        //[SearchContext("t:texture filtermode=0", "asset", SearchViewFlags.OpenInBuilderMode | SearchViewFlags.GridView)]
        [SearchContext("t:texture", "adb", SearchViewFlags.OpenInBuilderMode | SearchViewFlags.GridView)]
        public Texture pixelArtTexture;

        public GameObject DelayedCanvas; //For testing delayed canvas creation
        public Sprite TestSprite; 

        private Single  _sourceFloat;
        private bool    _targetBool = true;
        private int _sourceInt = 5;
        private EventType _sourceEnum = EventType.MouseMove;
        private string _sourceString = "Test string";

        private Func<StructEnum> _fastGetter;
        private Func<Int32> _boxedGetter;

        public TestComplexValue TestComplexSource
        {
            get => _testComplexValue;
            set => SetProperty( ref _testComplexValue, value );
        }

        private TestComplexValue _testComplexValue = new();

        public Single SourceFloat
        {
            get => _sourceFloat;
            set => SetProperty( ref _sourceFloat, value );
        }

        public Boolean TargetBool
        {
            get => _targetBool;
            set
            {
                //var oldValue = _targetBool;
                SetProperty( ref _targetBool, value );
                //Debug.Log( $"Changed bool from {oldValue} to {value}" );
            }
        }

        public int SourceInt
        {
            get => _sourceInt;
            set => SetProperty( ref _sourceInt, value );
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
            Debug.Log( "Call me called" );
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

        private IEnumerator Start( )
        {
            Application.targetFrameRate = 30;

            yield return new WaitForSeconds( 1 );
            if( DelayedCanvas && !DelayedCanvas.activeSelf )
            {
                DelayedCanvas.SetActive( true );
            }

            // var propInfo1 = GetType().GetProperty( nameof(TestComplexSource) );
            // var propAdapter = new PropertyAdapter<TestComplexValue>( this, propInfo1, false );
            // var propInfo2 = propInfo1.PropertyType.GetProperty( nameof(TestComplexValue.Value) );
            // var complexAdapter = new ComplexPropertyAdapter<TestComplexValue, float>(propAdapter, propInfo2, false);
            //
            // complexAdapter.TryGetValue( out var testValue );
            // Debug.Log( $"TestComplexSource.Value = {testValue}" );
            // TestComplexSource.Value = "43";
            // complexAdapter.TryGetValue( out testValue );
            // Debug.Log( $"TestComplexSource.Value = {testValue}" );
            //
            // var timer = System.Diagnostics.Stopwatch.StartNew();
            // var param1Type = typeof(PropertyAdapter<>).MakeGenericType( typeof(TestComplexValue) );
            // timer.Restart();
            // for ( int i = 0; i < 1000; i++ )
            // {
            //     param1Type = typeof(PropertyAdapter<>).MakeGenericType( typeof(TestComplexValue) );    
            // }
            // timer.Stop();
            // var param1TypeCreateTime = timer.Elapsed.TotalMicroseconds();
            //
            // var param2Type = typeof(ComplexPropertyAdapter<,>).MakeGenericType( typeof(TestComplexValue), typeof(float) );
            // timer.Restart();
            // for ( int i = 0; i < 1000; i++ )
            // {
            //     param2Type = typeof(ComplexPropertyAdapter<,>).MakeGenericType( typeof(TestComplexValue), typeof(float) );
            // }
            // timer.Stop();
            // var param2TypeCreateTime = timer.Elapsed.TotalMicroseconds();
            //
            // Debug.Log( $"param1 create type time {param1TypeCreateTime} mks, param2 create type time {param2TypeCreateTime} mks" );



            // var propInfo = GetType().GetProperty(nameof(SourceByte));
            // var closedDelegate = (Func<Byte>)Delegate.CreateDelegate( typeof(Func<Byte>), this, propInfo.GetGetMethod() );
            // var openDelegate = (Func<TestMonoBehSource, Byte>)Delegate.CreateDelegate( typeof(Func<TestMonoBehSource, Byte>), propInfo.GetGetMethod() );
            //
            // int closedValue = closedDelegate();
            // int openValue = openDelegate(this);
            //
            // var timer = System.Diagnostics.Stopwatch.StartNew();
            //
            // timer.Restart();
            // for ( int i = 0; i < 10000; i++ )
            // {
            //     closedDelegate = (Func<Byte>)Delegate.CreateDelegate( typeof(Func<Byte>), this, propInfo.GetGetMethod() );
            // }
            // timer.Stop();
            // var closedCreateTime = timer.Elapsed.TotalMicroseconds();
            //
            // timer.Restart();
            // for ( int i = 0; i < 10000; i++ )
            // {
            //     openDelegate = (Func<TestMonoBehSource, Byte>)Delegate.CreateDelegate( typeof(Func<TestMonoBehSource, Byte>), propInfo.GetGetMethod() );
            // }
            // timer.Stop();
            // var openCreateTime = timer.Elapsed.TotalMicroseconds();
            //
            // timer.Restart();
            // for ( int i = 0; i < 10000; i++ )
            // {
            //     closedValue += closedDelegate();
            // }
            // timer.Stop();
            // var closedTime = timer.Elapsed.TotalMicroseconds();
            //
            // timer.Restart();
            // for ( int i = 0; i < 10000; i++ )
            // {
            //     openValue += openDelegate(this);
            // }
            // timer.Stop();
            // var openTime = timer.Elapsed.TotalMicroseconds();
            //
            //
            // Debug.Log( $"closed create {closedCreateTime} mks, open create {openCreateTime}, closed exec {closedTime} mks, open exec {openTime} mks" );

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
            if( Time.frameCount % 10 == 0 )            //Test notifications
            {
                SourceFloat  = Time.time % 3f;

                TestComplexSource.Inner.Value2 = ((int)Time.time + 1).ToString();
                TestComplexSource.EnumValue    = (CameraType)(Time.frameCount % 4);
                //TestComplexSource.Inner = new TestComplex2(){Value2 = "42" };

            }

        }
    }

    //[INotifyPropertyChanged]
    public partial class TestComplexValue : ObservableObjectDebug
    {
        //public string Value { get; set; } = "42";

        //[ObservableProperty]
        public TestComplex2 Inner
        {
            get  => _inner;
            set => SetProperty( ref _inner, value );
        }
        private TestComplex2 _inner = new();

        public CameraType EnumValue
        {
            get => _enumValue;
            set => SetProperty( ref _enumValue, value );
        }

        private CameraType _enumValue;
    }

    public partial class TestComplex2 : ObservableObjectDebug
    {
        [ObservableProperty]
        private string _value2;

        public void CallMe( )
        {
            Debug.Log( "Call me complex" );
        }

        public void CallMe1( int value )
        {
            Debug.Log( $"Call me complex, {value}" );
        }

    }
}
