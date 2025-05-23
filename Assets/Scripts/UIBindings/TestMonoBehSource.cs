using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;
using Object = System.Object;

namespace UIBindings
{
    public class TestMonoBehSource : MonoBehaviour, INotifyPropertyChanged 
    {
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
                var oldValue = _targetBool;
                SetField( ref _targetBool, value );
                Debug.Log( $"Changed bool from {oldValue} to {value}" );
            }
        }

        public int SourceInt
        {
            get => _sourceInt;
            set => SetField( ref _sourceInt, value );
        }

        public float    TestConvertF { get; set; } = 5.5f;
        public int      TestConvertI { get; set; } = 3;

        private void Start( )
        {
            var f2i = new FloatToIntConverter();
            var i2f = (FloatToIntConverter.ReverseModeWrapper)f2i.GetReverseConverter();

            f2i.InitAttachToSourceProperty( this, GetType().GetProperty( nameof(TestConvertF) ) );
            f2i.InitSourceToTarget( new DebugInt() );
            f2i.OnSourcePropertyChanged();//Check source to target
            f2i.ProcessTargetToSource( 42 ); //Check target to source
            Assert.IsTrue( TestConvertF == 42 );

            i2f.InitAttachToSourceProperty( this, GetType().GetProperty( nameof(TestConvertI) ) );
            i2f.InitSourceToTarget( new DebugFloat() );
            i2f.OnSourcePropertyChanged();//Check source to target
            i2f.ProcessTargetToSource( 42.1f ); //Check target to source
            Assert.IsTrue( TestConvertI == 42 );
            
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

        public class DebugFloat : IInput<float>
        {
            public void ProcessSourceToTarget( Single value )
            {
                Debug.Log( $"Debug float {value}" );
            }
        }

        public class DebugInt : IInput<int>
        {
            public void ProcessSourceToTarget( int value )
            {
                Debug.Log( $"Debug int {value}" );
            }
        }
    }
}
