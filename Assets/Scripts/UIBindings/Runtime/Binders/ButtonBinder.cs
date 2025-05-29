using System;
using UIBindings.Runtime.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UIBindings
{
    public class ButtonBinder : MonoBehaviour
    {
        public Button Button;
        public CallBinding CallBinding;
        public Binding<bool> CanExecuteBinding;

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

            CanExecuteBinding.SetDebugInfo(this, nameof(CanExecuteBinding));
            CanExecuteBinding.Awake( );
            CanExecuteBinding.SourceChanged += CanExecuteChanged;
            _canExecute = Button.interactable;

            CallBinding.Awake(this);
        }

        private void OnEnable()
        {
            CanExecuteBinding.Subscribe();
            Button.onClick.AddListener(OnButtonClick);
        }

        private void OnDisable()
        {
            CanExecuteBinding.Unsubscribe();
            Button.onClick.RemoveListener(OnButtonClick);
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

