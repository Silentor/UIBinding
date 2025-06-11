using System;
using TMPro;
using UIBindings.Runtime.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Object = System.Object;

namespace UIBindings
{
    public class ButtonBinder : MonoBehaviour
    {
        public Button       Button;
        public TMP_Text     ButtonText;

        public CallBinding              CallBinding;
        public ValueBinding<bool>       CanExecuteBinding;
        public ValueBinding<string>     ButtonTextBinding; 

        public bool DisableButtonWhileExecuting = true;

        //For debug purposes
        public int AsyncExecutingCount => _asyncExecutingCount;

        private bool _isDisabledOnExecute = false;
        private bool _canExecute = true;
        private int _asyncExecutingCount;

        private void Awake()
        {
            if (!Button)
                Button = GetComponent<Button>();
            Assert.IsTrue(Button);

            if( ButtonTextBinding.Enabled && !ButtonText )
            {
                ButtonText = Button.GetComponentInChildren<TMP_Text>();
                Assert.IsTrue(ButtonText);
            }

            CanExecuteBinding.SetDebugInfo(this, nameof(CanExecuteBinding));
            CanExecuteBinding.Init( );
            CanExecuteBinding.SourceChanged += CanExecuteChanged;
            _canExecute = Button.interactable;

            CallBinding.Init(this);

            ButtonTextBinding.SetDebugInfo( this, nameof(ButtonTextBinding) );
            ButtonTextBinding.Init(  );
            ButtonTextBinding.SourceChanged += ProcessButtonText;

        }

        private void OnEnable()
        {
            CanExecuteBinding.Subscribe();
            ButtonTextBinding.Subscribe();
            Button.onClick.AddListener(OnButtonClick);
        }

        private void OnDisable()
        {
            CanExecuteBinding.Unsubscribe();
            ButtonTextBinding.Unsubscribe();
            Button.onClick.RemoveListener(OnButtonClick);
        }

#if UNITY_EDITOR
        private void Reset( )
        {
            ButtonTextBinding.Enabled = false;
        }
#endif        

        private void ProcessButtonText(Object sender, String value )
        {
            ButtonText.text = value;
        }

        private void CanExecuteChanged(object sender, bool value)
        {
            _canExecute = value; 
            UpdateInteractableInternal( );
        }

        private void UpdateInteractableInternal(  )
        {
            if (Button)
            {
                Button.interactable = _canExecute && !_isDisabledOnExecute;
            }
        }

        private void OnButtonClick()
        {
            if( !_canExecute || _isDisabledOnExecute )
                return;

            try
            {
                var executeTask = CallBinding.Call();
                ProcessAsyncCall( executeTask ).Forget( static ex => throw ex );
            }
            catch ( Exception ex )
            {
                Debug.LogError( $"[ButtonBinder] button binder {name} catch exception while call: {ex}", this ); 
            }
        }

        private async Awaitable ProcessAsyncCall( Awaitable executeTask )
        {
            if ( DisableButtonWhileExecuting )
            {
                _isDisabledOnExecute = true;
                UpdateInteractableInternal();
            }

            _asyncExecutingCount++;

            try
            {
                await executeTask;
            }
            // catch ( OperationCanceledException )
            // {
            //     //Swallow cancellation exception. its ok
            // }
            // catch ( Exception e )
            // {
            //     Debug.LogError( $"[{nameof(ButtonBinder)}] Exception during button {name} execution: {e.Message}", this );
            // }
            finally
            {
                if ( DisableButtonWhileExecuting )
                {
                    _isDisabledOnExecute = false;
                    UpdateInteractableInternal();
                }

                _asyncExecutingCount--;
                if ( _asyncExecutingCount < 0 )
                {
                    Debug.LogError( $"[{nameof(ButtonBinder)}] Async executing count of button {name} is negative: {_asyncExecutingCount}. This should not happen.", this );
                    _asyncExecutingCount = 0;
                }
            }
        }
    }
}

