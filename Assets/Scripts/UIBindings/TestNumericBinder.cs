using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using Object = System.Object;

namespace UIBindings
{
    public class TestNumericBinder : MonoBehaviour
    {
        public BindingTwoWay<int>     IntBinding;
        public Binding<float>         FloatBinding;

        public bool WriteIntValue = false;
        private Int32 _intValue;

        void Awake() 
        {
            // IntBinding.Awake(this);
            // IntBinding.SourceChanged += OnIntValueChanged;
            // IntBinding.Subscribe();
            //
            // FloatBinding.Awake(this);
            // FloatBinding.SourceChanged += OnFloatValueChanged;
            // FloatBinding.Subscribe();

            StartCoroutine( DelayAwake() );
        }

        IEnumerator DelayAwake( )
        {
            yield return new WaitForSeconds( 0.5f );
            yield return null;
            
            IntBinding.SetDebugInfo( this, nameof(IntBinding) );
            IntBinding.Awake(  );
            IntBinding.SourceChanged += OnIntValueChanged;
            IntBinding.Subscribe();
            
            yield return new WaitForSeconds( 0.5f );
            yield return null;
            
            FloatBinding.SetDebugInfo( this, nameof(FloatBinding) );
            FloatBinding.Awake(  );
            FloatBinding.SourceChanged += OnFloatValueChanged;
            FloatBinding.Subscribe();

            // while ( true )
            // {
            //     yield return null;
            //
            //     Profiler.BeginSample( "Binding.test" );
            //     Profiler.EndSample();
            // }
        
        }

        private void Update( )
        {
            if ( WriteIntValue )
            {
                WriteIntValue = false;

                _intValue += 1;
                IntBinding.SetValue( _intValue );


            }
        }

        private void LateUpdate( )
        {
            IntBinding.CheckChanges();
            FloatBinding.CheckChanges();
        }


        private void OnIntValueChanged(Object sender, Int32 value )
        {
            Debug.Log( $"[{nameof(TestNumericBinder)}]-[{nameof(OnIntValueChanged)}] {value}" );
            _intValue = value;
        }
        private void OnFloatValueChanged(Object sender, float value )
        {
            Debug.Log( $"[{nameof(TestNumericBinder)}]-[{nameof(OnFloatValueChanged)}] {value}" );
        }

    }
}