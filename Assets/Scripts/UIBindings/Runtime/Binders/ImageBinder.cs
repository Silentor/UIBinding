using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Object = System.Object;

namespace UIBindings
{
    public class ImageBinder : BinderBase
    {
        public Image Image;
        public ValueBinding<Sprite>      SourceImageBinding;
        public ValueBinding<Color>       ColorBinding;                      //Optional
        public ValueBinding<Boolean>     EnabledBinding;                    //Optional

        protected void Awake( )
        {
            if ( !Image )
                Image = GetComponent<Image>();
            Assert.IsTrue( Image );

            SourceImageBinding.SetDebugInfo( this, nameof(SourceImageBinding) );
            SourceImageBinding.Init( GetSource(SourceImageBinding) );
            SourceImageBinding.SourceChanged += UpdateSourceImage;

            ColorBinding.SetDebugInfo( this, nameof(ColorBinding) );        
            ColorBinding.Init( GetSource(ColorBinding) );
            ColorBinding.SourceChanged += UpdateColor;

            EnabledBinding.SetDebugInfo( this, nameof(ColorBinding) );        
            EnabledBinding.Init( GetSource(EnabledBinding) );
            EnabledBinding.SourceChanged += UpdateEnabled;
        }

        protected void OnEnable( )
        {
            SourceImageBinding.Subscribe( GetUpdateOrder() );
            ColorBinding.Subscribe( GetUpdateOrder() );
            EnabledBinding.Subscribe( GetUpdateOrder() );
        }

        private void OnDisable( )
        {
            SourceImageBinding.Unsubscribe();
            ColorBinding.Unsubscribe();
            EnabledBinding.Unsubscribe();
        }
#if UNITY_EDITOR
        private void Reset( )
        {
            ColorBinding.Enabled = false;
            EnabledBinding.Enabled = false;
        }
#endif

        private void UpdateSourceImage(Object sender, Sprite sprite )
        {
            Image.sprite = sprite;
        }

        private void UpdateColor(Object sender, Color color )
        {
            Image.color = color;
        }

        private void UpdateEnabled(Object sender, Boolean isEnabled )
        {
            Image.enabled = isEnabled;
        }

    }
}
