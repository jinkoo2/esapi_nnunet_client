using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Xceed.Wpf.AvalonDock.Controls;
using static nnunet_client.ConstraintSet;
using nnunet_client.models;

namespace nnunet_client.views
{
    public partial class ConstraintSetEditorControl : UserControl, INotifyPropertyChanged
    {

        // Implementation of INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static readonly DependencyProperty TitleProperty =
    DependencyProperty.Register(
        "Title",
        typeof(string),
        typeof(ConstraintSetEditorControl),
        new PropertyMetadata("Untitled")); // default value

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public ObservableCollection<Prescription> Prescriptions { get; set; }

        // Use a private backing field for the property
        private ObservableCollection<string> _planContourIds;
        public ObservableCollection<string> PlanContourIds
        {
            get { return _planContourIds; }
            set
            {
                if (_planContourIds != value)
                {
                    _planContourIds = value;
                    // THIS IS THE KEY: We notify the UI that the property has changed
                    OnPropertyChanged(nameof(PlanContourIds));
                }
            }
        }

        public ObservableCollection<ConstraintSet.ContourConstraint> ContourConstraints { get; set; }

        public ConstraintSetEditorControl()
        {
            InitializeComponent();

            // Dummy example - replace with real data
            Prescriptions = new ObservableCollection<Prescription>
            {
                new Prescription { Id = "Default", TotalDose = 3000 }
            };

            PlanContourIds = new ObservableCollection<string>
            {
                "PTV1",
                "PTV2"
            };

            ContourConstraints = new ObservableCollection<ConstraintSet.ContourConstraint>
            {
                new ConstraintSet.ContourConstraint
                {
                    Id = "PTV",
                    PlanContourId = "PTV1",
                    Type = ContourType.Target,
                    Prescription = Prescriptions[0],
                    Constraints = new[] {
                        new ConstraintSet.Constraint
                        {
                            Type = ConstraintType.Max,
                            Limit = "Max<110%",
                            Comment = "Upper dose limit"
                        }
                    }
                }
            };

            this.DataContext = this;
        }


        public void SaveAsJson(string path)
        {
            var constraintSet = new ConstraintSet
            {
                Title = this.Title,
                Prescriptions = this.Prescriptions.ToArray(),
                PlanContourIds = this.PlanContourIds.ToArray(),
                ContourConstraints = this.ContourConstraints.ToArray()
            };

            // Serialize to JSON
            var json = JsonConvert.SerializeObject(constraintSet, Formatting.Indented);

            // Save to file
            System.IO.File.WriteAllText(path, json);
        }

        public void LoadFromJson(string path)
        {
            var json = System.IO.File.ReadAllText(path);
            var loadedConstraintSet = JsonConvert.DeserializeObject<ConstraintSet>(json);

            if (loadedConstraintSet == null)
            {
                Console.WriteLine("Failed to load constraint set from JSON.");
                return;
            }

            // Assign values to control properties
            this.Title = loadedConstraintSet.Title;

            // Replace ObservableCollections so WPF bindings update properly
            this.Prescriptions = new ObservableCollection<Prescription>(
                loadedConstraintSet.Prescriptions ?? Array.Empty<Prescription>()
            );

            this.PlanContourIds = new ObservableCollection<string>(
                loadedConstraintSet.PlanContourIds ?? Array.Empty<string>()
            );

            this.ContourConstraints = new ObservableCollection<ConstraintSet.ContourConstraint>(
                loadedConstraintSet.ContourConstraints ?? Array.Empty<ConstraintSet.ContourConstraint>()
            );

            // Validate constraints
            if (loadedConstraintSet.Validate(out var errors))
            {
                Console.WriteLine("All constraints are valid.");
            }
            else
            {
                Console.WriteLine("Errors:");
                foreach (var err in errors)
                    Console.WriteLine(err);
            }
        }


    }
}
