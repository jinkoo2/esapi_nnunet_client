using nnunet_client.models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace nnunet_client.viewmodels
{
    public class StepNavigatorViewModel : BaseModel
    {
        private int _currentIndex=-1;
        private ObservableCollection<StepPage> _stepPages = new ObservableCollection<StepPage>();
        public ObservableCollection<StepPage> StepPages {
            get => _stepPages;
            set {

                if (_stepPages == value) return;

                SetProperty(ref _stepPages, value, nameof(StepPages));
                CurrentIndex = 0;
                
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(CanGoNext));
                OnPropertyChanged(nameof(CanGoPrevious));
            }
        }

        public int CurrentIndex
        {
            get => _currentIndex;
            set
            {
                if (_currentIndex != value)
                {
                    Console.WriteLine($"Setting page index to {_currentIndex}");
                    _currentIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentPage));
                    OnPropertyChanged(nameof(CanGoNext));
                    OnPropertyChanged(nameof(CanGoPrevious));
                }
            }
        }

        public StepPage CurrentPage =>
            (CurrentIndex >= 0 && CurrentIndex < StepPages.Count) ? StepPages[CurrentIndex] : null;

        public bool CanGoNext => CurrentIndex < StepPages.Count - 1;
        public bool CanGoPrevious => CurrentIndex > 0;

        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }

        public StepNavigatorViewModel()
        {
            NextCommand = new RelayCommand2(_ => GoNext(), _ => CanGoNext);
            PreviousCommand = new RelayCommand2(_ => GoPrevious(), _ => CanGoPrevious);
        }

        private void GoNext()
        {
            if (CanGoNext) CurrentIndex++;
        }

        private void GoPrevious()
        {
            if (CanGoPrevious) CurrentIndex--;
        }

    }

  
}
