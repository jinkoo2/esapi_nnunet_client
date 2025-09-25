using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nnunet_client.viewmodels
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.Win32;
    using Newtonsoft.Json;

    public class ListManagerViewModel<T> : BaseViewModel where T : class, new()
    {
        public ObservableCollection<T> Items { get; } = new ObservableCollection<T>();

        private T _selectedItem;
        public T SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        // Commands
        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand DuplicateCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ImportCommand { get; }

        private readonly Func<T, T> _duplicateFunc;

        public ListManagerViewModel(Func<T, T> duplicateFunc = null)
        {
            _duplicateFunc = duplicateFunc ?? (item => new T());

            AddCommand = new RelayCommand(Add);
            RemoveCommand = new RelayCommand(Remove);
            DuplicateCommand = new RelayCommand(Duplicate);
            ExportCommand = new RelayCommand(Export);
            ImportCommand = new RelayCommand(Import);
        }

        private void Add()
        {
            Items.Add(new T());
        }

        private void Remove()
        {
            if (SelectedItem != null)
                Items.Remove(SelectedItem);
        }

        private void Duplicate()
        {
            if (SelectedItem != null)
            {
                var copy = _duplicateFunc(SelectedItem);
                Items.Add(copy);
            }
        }

        private void Export()
        {
            var dlg = new SaveFileDialog { Filter = "JSON files (*.json)|*.json" };
            if (dlg.ShowDialog() == true)
            {
                var json = JsonConvert.SerializeObject(Items, Formatting.Indented);
                File.WriteAllText(dlg.FileName, json);
                MessageBox.Show("Exported successfully.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Import()
        {
            var dlg = new OpenFileDialog { Filter = "JSON files (*.json)|*.json" };
            if (dlg.ShowDialog() == true)
            {
                var json = File.ReadAllText(dlg.FileName);
                var imported = JsonConvert.DeserializeObject<ObservableCollection<T>>(json);
                if (imported != null)
                {
                    Items.Clear();
                    foreach (var item in imported) Items.Add(item);
                }
                MessageBox.Show("Imported successfully.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

}
