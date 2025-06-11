using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Object = System.Object;

namespace UIBindings
{
    public class ImageBinder : MonoBehaviour
    {
        public Image Image;
        public ValueBinding<Sprite>      SourceImageBinding;
        public ValueBinding<Color>       ColorBinding;                      //Optional

        protected void Awake( )
        {
            if ( !Image )
                Image = GetComponent<Image>();
            Assert.IsTrue( Image );

            SourceImageBinding.SetDebugInfo( this, nameof(SourceImageBinding) );
            SourceImageBinding.Init(  );
            SourceImageBinding.SourceChanged += UpdateSourceImage;
            ColorBinding.SetDebugInfo( this, nameof(ColorBinding) );        
            ColorBinding.Init( );
            ColorBinding.SourceChanged += UpdateColor;
        }

        protected void OnEnable( )
        {
            SourceImageBinding.Subscribe();
            ColorBinding.Subscribe();
        }

        private void OnDisable( )
        {
            SourceImageBinding.Unsubscribe();
            ColorBinding.Unsubscribe();
        }
#if UNITY_EDITOR
        private void Reset( )
        {
            ColorBinding.Enabled = false;
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
    }
}
