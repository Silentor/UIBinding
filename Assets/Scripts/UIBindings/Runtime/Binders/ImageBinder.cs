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
        public Binding<Sprite>      SourceImageBinding;
        public Binding<Color>       ColorBinding            = new (){Enabled = false};

        protected void Awake( )
        {
            if ( !Image )
                Image = GetComponent<Image>();
            Assert.IsTrue( Image );

            SourceImageBinding.Awake( this );
            SourceImageBinding.SourceChanged += UpdateSourceImage;
            ColorBinding.Awake( this );
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

        private void LateUpdate( )
        {
            SourceImageBinding.CheckChanges();
            ColorBinding.CheckChanges();
        }

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
