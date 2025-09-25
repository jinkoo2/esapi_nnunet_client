using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace nnunet_client.viewmodels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private static System.Timers.Timer _saveTimer;
        private static Action _saveAction;

        /// <summary>
        /// Call this once in the derived ViewModel constructor to define what happens on save.
        /// </summary>
        protected void InitializeAutoSave(Action saveAction)
        {
            _saveAction = saveAction;

            // Set up timer once
            _saveTimer = new System.Timers.Timer(3000); // 3 seconds debounce
            _saveTimer.AutoReset = false; // run only once after interval
            _saveTimer.Elapsed += (s, e) =>
            {
                _saveAction?.Invoke();
            };
        }

        /// <summary>
        /// Helper method to set a property and raise PropertyChanged only if value changed.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);

            // Schedule save if configured
            if (_saveAction != null)
                ScheduleSave();

            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected async void ScheduleSave()
        {
            if (_saveTimer != null)
            {
                // Restart the timer — cancels any pending save
                _saveTimer.Stop();
                _saveTimer.Start();
            }
        }

    }
}
