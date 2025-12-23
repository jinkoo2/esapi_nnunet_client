using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using nnunet;
using nnunet_client.models;
using nnunet_client.services;
using static esapi.esapi;
using itk.simple;
using VMS.TPS.Common.Model.Types;
using VMSStructure = VMS.TPS.Common.Model.API.Structure;
using VMSStructureSet = VMS.TPS.Common.Model.API.StructureSet;
using VMSImage = VMS.TPS.Common.Model.API.Image;

namespace nnunet_client.viewmodels
{
    public class LabelStructureMapping : INotifyPropertyChanged
    {
        private string _labelName;
        private int _labelValue;
        private VMSStructure _selectedStructure;

        public string LabelName
        {
            get => _labelName;
            set
            {
                if (_labelName != value)
                {
                    _labelName = value;
                    OnPropertyChanged(nameof(LabelName));
                }
            }
        }

        public int LabelValue
        {
            get => _labelValue;
            set
            {
                if (_labelValue != value)
                {
                    _labelValue = value;
                    OnPropertyChanged(nameof(LabelValue));
                }
            }
        }

        public VMSStructure SelectedStructure
        {
            get => _selectedStructure;
            set
            {
                if (_selectedStructure != value)
                {
                    _selectedStructure = value;
                    OnPropertyChanged(nameof(SelectedStructure));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SubmitImageAndLabelsViewModel : BaseViewModel
    {
        private nnUNetServicClient _client;
        private string _nnUNetServerURL;
        private JobQueueService _jobQueueService;

        public SubmitImageAndLabelsViewModel()
        {
            _nnUNetServerURL = global.appConfig.nnunet_server_url;
            _client = new nnUNetServicClient(_nnUNetServerURL, global.appConfig.nnunet_server_auth_token);
            _jobQueueService = new JobQueueService();

            Datasets = new ObservableCollection<dynamic>();
            LabelMappings = new ObservableCollection<LabelStructureMapping>();

            LoadDatasetsCommand = new RelayCommand(async () => await LoadDatasetsAsync());

            // Load datasets on initialization
            LoadDatasetsAsync();
        }

        private VMSStructureSet _structureSet;
        public VMSStructureSet StructureSet
        {
            get => _structureSet;
            set
            {
                if (_structureSet == value) return;
                SetProperty(ref _structureSet, value, nameof(StructureSet));
                UpdateAvailableStructures();
            }
        }

        private ObservableCollection<dynamic> _datasets;
        public ObservableCollection<dynamic> Datasets
        {
            get => _datasets;
            set => SetProperty(ref _datasets, value, nameof(Datasets));
        }

        private dynamic _selectedDataset;
        public dynamic SelectedDataset
        {
            get => _selectedDataset;
            set
            {
                if (_selectedDataset == value) return;
                SetProperty(ref _selectedDataset, value, nameof(SelectedDataset));
                UpdateLabelMappings();
            }
        }

        private ObservableCollection<LabelStructureMapping> _labelMappings;
        public ObservableCollection<LabelStructureMapping> LabelMappings
        {
            get => _labelMappings;
            set => SetProperty(ref _labelMappings, value, nameof(LabelMappings));
        }

        private ObservableCollection<VMSStructure> _availableStructures;
        public ObservableCollection<VMSStructure> AvailableStructures
        {
            get => _availableStructures;
            set => SetProperty(ref _availableStructures, value, nameof(AvailableStructures));
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value, nameof(IsLoading));
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value, nameof(ErrorMessage));
        }

        private ObservableCollection<string> _imageForOptions = new ObservableCollection<string> { "Train", "Test" };
        public ObservableCollection<string> ImageForOptions
        {
            get => _imageForOptions;
            set => SetProperty(ref _imageForOptions, value, nameof(ImageForOptions));
        }

        private string _selectedImageFor = "Train";
        public string SelectedImageFor
        {
            get => _selectedImageFor;
            set => SetProperty(ref _selectedImageFor, value, nameof(SelectedImageFor));
        }

        public ICommand LoadDatasetsCommand { get; }
        private RelayCommand _submitCommand;
        public ICommand SubmitCommand
        {
            get
            {
                if (_submitCommand == null)
                {
                    _submitCommand = new RelayCommand(async () => await SubmitAsync(), () => CanSubmit());
                }
                return _submitCommand;
            }
        }

        private async Task LoadDatasetsAsync()
        {
            IsLoading = true;
            ErrorMessage = "";

            try
            {
                helper.log("Loading datasets from API...");
                var response = await _client.GetDatasetJsonListAsync();
                
                if (response is Newtonsoft.Json.Linq.JArray array)
                {
                    Datasets = new ObservableCollection<dynamic>(array.Cast<dynamic>());
                    helper.log($"Loaded {Datasets.Count} datasets");
                }
                else if (response is Newtonsoft.Json.Linq.JObject obj && obj["datasets"] != null)
                {
                    var datasetsArray = obj["datasets"] as Newtonsoft.Json.Linq.JArray;
                    if (datasetsArray != null)
                    {
                        Datasets = new ObservableCollection<dynamic>(datasetsArray.Cast<dynamic>());
                        helper.log($"Loaded {Datasets.Count} datasets");
                    }
                }
                else
                {
                    helper.log("Unexpected response format from API");
                    ErrorMessage = "Unexpected response format from API";
                }
            }
            catch (Exception ex)
            {
                helper.log($"Error loading datasets: {ex.Message}");
                ErrorMessage = $"Error loading datasets: {ex.Message}";
                MessageBox.Show($"Failed to load datasets:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateAvailableStructures()
        {
            if (StructureSet != null)
            {
                AvailableStructures = new ObservableCollection<VMSStructure>(StructureSet.Structures);
                helper.log($"Updated available structures: {AvailableStructures.Count} structures");
            }
            else
            {
                AvailableStructures = new ObservableCollection<VMSStructure>();
            }
        }

        private void UpdateLabelMappings()
        {
            LabelMappings.Clear();

            if (SelectedDataset == null || SelectedDataset.labels == null)
            {
                RaiseSubmitCommandCanExecuteChanged();
                return;
            }

            try
            {
                var labels = SelectedDataset.labels as Newtonsoft.Json.Linq.JObject;
                if (labels != null)
                {
                    foreach (var label in labels)
                    {
                        int labelValue = (int)label.Value;
                        // Skip background label (value = 0)
                        if (labelValue == 0)
                            continue;

                        var mapping = new LabelStructureMapping
                        {
                            LabelName = label.Key,
                            LabelValue = labelValue
                        };
                        // Subscribe to property changes to update CanExecute
                        mapping.PropertyChanged += (s, e) => RaiseSubmitCommandCanExecuteChanged();
                        LabelMappings.Add(mapping);
                    }
                    helper.log($"Updated label mappings: {LabelMappings.Count} labels (excluding background)");
                }
            }
            catch (Exception ex)
            {
                helper.log($"Error updating label mappings: {ex.Message}");
                ErrorMessage = $"Error updating label mappings: {ex.Message}";
            }
            
            RaiseSubmitCommandCanExecuteChanged();
        }

        private void RaiseSubmitCommandCanExecuteChanged()
        {
            _submitCommand?.RaiseCanExecuteChanged();
        }

        private bool CanSubmit()
        {
            if (SelectedDataset == null)
                return false;

            if (string.IsNullOrEmpty(SelectedImageFor))
                return false;

            if (LabelMappings == null || LabelMappings.Count == 0)
                return false;

            if (StructureSet == null)
                return false;

            // Check if all labels have a structure selected
            return LabelMappings.All(m => m.SelectedStructure != null);
        }

        private async Task SubmitAsync()
        {
            if (!CanSubmit())
            {
                MessageBox.Show("Please select a dataset, image for (Train/Test), and map all labels to structures.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            ErrorMessage = "";

            try
            {
                helper.log("Creating submission job...");

                if (StructureSet == null || StructureSet.Image == null)
                {
                    throw new Exception("Structure set or image is not available.");
                }

                var image = StructureSet.Image;
                string datasetId = SelectedDataset.id;
                string imagesFor = SelectedImageFor;

                // Create job object
                var job = new SubmitJob
                {
                    DatasetId = datasetId,
                    ImagesFor = imagesFor,
                    PatientId = global.vmsPatient?.Id,
                    StructureSetId = StructureSet.Id,
                    StructureSetUID = StructureSet.UID,
                    ImageId = image.Id,
                    ImageUID = image.UID,
                    ImageFOR = image.FOR,
                    LabelMappings = LabelMappings
                        .Where(m => m.SelectedStructure != null)
                        .Select(m => new SubmitJob.LabelMapping
                        {
                            LabelName = m.LabelName,
                            LabelValue = m.LabelValue,
                            StructureId = m.SelectedStructure.Id
                        })
                        .ToList()
                };

                // Enqueue the job
                string jobId = _jobQueueService.EnqueueJob(job);
                helper.log($"Job enqueued successfully: {jobId}");
                helper.log($"Queue directory: {_jobQueueService.QueueDirectory}");

                MessageBox.Show(
                    $"Job submitted successfully!\n\n" +
                    $"Job ID: {jobId}\n" +
                    $"Status: Pending\n" +
                    $"Queue Directory: {_jobQueueService.QueueDirectory}\n\n" +
                    $"The job will be processed by the worker program.",
                    "Job Submitted",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                helper.log($"Error submitting: {ex.Message}");
                ErrorMessage = $"Error submitting: {ex.Message}";
                MessageBox.Show($"Failed to submit:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ConvertImageToInt32(string inputPath, string outputPath)
        {
            helper.log($"ConvertImageToInt32(input={inputPath}, output={outputPath})");

            // Read the exported image
            itk.simple.ImageFileReader reader = new itk.simple.ImageFileReader();
            reader.SetFileName(inputPath);
            itk.simple.Image inputImage = reader.Execute();

            // Convert to Int32
            itk.simple.Image int32Image = SimpleITK.Cast(inputImage, itk.simple.PixelIDValueEnum.sitkInt32);

            // Save with compression
            itk.simple.ImageFileWriter writer = new itk.simple.ImageFileWriter();
            writer.Execute(int32Image, outputPath, true);
            helper.log($"Image converted to Int32 and saved to {outputPath}");
        }

        private void CombineMasksIntoLabelImage(List<(string path, int labelValue)> maskInfoList, string outputPath)
        {
            helper.log($"CombineMasksIntoLabelImage(output={outputPath})");

            if (maskInfoList == null || maskInfoList.Count == 0)
            {
                throw new Exception("No mask paths provided");
            }

            // Read first mask to get image properties
            itk.simple.ImageFileReader reader = new itk.simple.ImageFileReader();
            reader.SetFileName(maskInfoList[0].path);
            itk.simple.Image firstMask = reader.Execute();

            // Create Int32 label image initialized to 0 (background)
            itk.simple.Image labelImage = SimpleITK.Cast(firstMask, itk.simple.PixelIDValueEnum.sitkInt32);
            // Initialize to 0
            labelImage = SimpleITK.Multiply(labelImage, 0.0);
            labelImage = SimpleITK.Cast(labelImage, itk.simple.PixelIDValueEnum.sitkInt32);

            // For each mask, multiply by label value and add to label image
            for (int i = 0; i < maskInfoList.Count; i++)
            {
                string maskPath = maskInfoList[i].path;
                int labelValue = maskInfoList[i].labelValue;

                helper.log($"Processing mask {i + 1}/{maskInfoList.Count}: label value={labelValue} from {maskPath}");

                // Read mask
                reader.SetFileName(maskPath);
                itk.simple.Image mask = reader.Execute();

                // Multiply mask by label value (result will be float, then cast to Int32)
                itk.simple.Image scaledMask = SimpleITK.Multiply(mask, (double)labelValue);
                itk.simple.Image scaledMaskInt32 = SimpleITK.Cast(scaledMask, itk.simple.PixelIDValueEnum.sitkInt32);

                // Add to label image (this combines masks, with later structures potentially overwriting earlier ones where they overlap)
                labelImage = SimpleITK.Add(labelImage, scaledMaskInt32);
                labelImage = SimpleITK.Cast(labelImage, itk.simple.PixelIDValueEnum.sitkInt32);
            }

            // Save with compression
            itk.simple.ImageFileWriter writer = new itk.simple.ImageFileWriter();
            writer.Execute(labelImage, outputPath, true);
            helper.log($"Labels image created successfully at {outputPath}");
        }

        private double[] GetBoundingBox(VMSStructure s)
        {
            var mesh = s.MeshGeometry;
            if (mesh == null || mesh.Positions == null || mesh.Positions.Count == 0)
            {
                // Return a very large bounding box if no mesh
                return new double[] { double.MinValue, double.MaxValue, double.MinValue, double.MaxValue, double.MinValue, double.MaxValue };
            }

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            double minZ = double.MaxValue, maxZ = double.MinValue;

            foreach (System.Windows.Media.Media3D.Point3D pos in mesh.Positions)
            {
                if (pos.X < minX) minX = pos.X;
                if (pos.X > maxX) maxX = pos.X;
                if (pos.Y < minY) minY = pos.Y;
                if (pos.Y > maxY) maxY = pos.Y;
                if (pos.Z < minZ) minZ = pos.Z;
                if (pos.Z > maxZ) maxZ = pos.Z;
            }

            return new double[] { minX, maxX, minY, maxY, minZ, maxZ };
        }
    }
}

