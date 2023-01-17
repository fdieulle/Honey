using System;

namespace Domain.ViewModels
{
    public class ModalViewModel
    {
        private Action _onAccept;
        public string Purpose { get; set; }
        public bool IsVisible { get; private set; }

        public void Show(Action onAccept)
        {
            _onAccept = onAccept;
            IsVisible = true;
        }

        public void Accept()
        {
            _onAccept?.Invoke();
            IsVisible = false;
        }

        public void Cancel() => IsVisible = false;
    }
}
