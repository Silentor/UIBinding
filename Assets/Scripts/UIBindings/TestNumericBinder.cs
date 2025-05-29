using System;
using System.Collections;
using UIBindings.Runtime;
using UnityEngine;
using UnityEngine.Profiling;
using Object = System.Object;

namespace UIBindings
{
    public class TestNumericBinder : MonoBehaviour
    {
        public BindingTwoWay<int>     IntBinding;
        public Binding<float>         FloatBinding;
        public BindingTwoWay<StructEnum>     EnumBinding;

        public bool WriteEnumValue = false;
        private Int32 _intValue;
        private Type _enumType;

        void Awake() 
        {
            IntBinding.SetDebugInfo( this, nameof(IntBinding) );
            IntBinding.Awake();
            IntBinding.SourceChanged += OnIntValueChanged;
            IntBinding.Subscribe();
            
            FloatBinding.SetDebugInfo( this, nameof(FloatBinding) );
            FloatBinding.Awake();
            FloatBinding.SourceChanged += OnFloatValueChanged;
            FloatBinding.Subscribe();

            EnumBinding.SetDebugInfo( this, nameof(EnumBinding) );
            EnumBinding.Awake(  );
            EnumBinding.SourceChanged += OnEnumValueChanged;
            EnumBinding.Subscribe();


            //StartCoroutine( DelayAwake() );
        }

        private void OnEnumValueChanged(Object arg1, StructEnum arg2 )
        {
            _enumType = arg2.EnumType;
            Debug.Log( arg2 );
        }

        // IEnumerator DelayAwake( )
        // {
        //     yield return new WaitForSeconds( 0.5f );
        //     yield return null;
        //     
        //     IntBinding.SetDebugInfo( this, nameof(IntBinding) );
        //     IntBinding.Awake(  );
        //     IntBinding.SourceChanged += OnIntValueChanged;
        //     IntBinding.Subscribe();
        //     
        //     yield return new WaitForSeconds( 0.5f );
        //     yield return null;
        //     
        //     FloatBinding.SetDebugInfo( this, nameof(FloatBinding) );
        //     FloatBinding.Awake(  );
        //     FloatBinding.SourceChanged += OnFloatValueChanged;
        //     FloatBinding.Subscribe();
        // }

        private void Update( )
        {
            if ( WriteEnumValue )
            {
                WriteEnumValue = false;

                _intValue += 1;
                EnumBinding.SetValue( new StructEnum( _intValue, _enumType ) );


            }
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