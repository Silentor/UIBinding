using System;
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

        private void Awake()
        {
            if (!Button)
                Button = GetComponent<Button>();
            Assert.IsTrue(Button);

            CanExecuteBinding.Awake(this);
            CanExecuteBinding.SourceChanged += UpdateInteractable;
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

        private void LateUpdate()
        {
            CanExecuteBinding.CheckChanges();
        }

        private void UpdateInteractable(object sender, bool value)
        {
            if (Button)
                Button.interactable = value;
        }

        private void OnButtonClick()
        {
            CallBinding.Call();
        }
    }
}

