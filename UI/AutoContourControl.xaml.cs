using esapi;
using Newtonsoft.Json;
using nnunet;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VMS.TPS.Common.Model.API;
using static esapi.SegmentationTemplate;
using VMSCourse = VMS.TPS.Common.Model.API.Course;
using VMSHospital = VMS.TPS.Common.Model.API.Hospital;
using VMSImage = VMS.TPS.Common.Model.API.Image;
using VMSPatient = VMS.TPS.Common.Model.API.Patient;
using VMSReferencePoint = VMS.TPS.Common.Model.API.ReferencePoint;
using VMSRegistration = VMS.TPS.Common.Model.API.Registration;
using VMSSeries = VMS.TPS.Common.Model.API.Series;
using VMSStructure = VMS.TPS.Common.Model.API.Structure;
using VMSStructureSet = VMS.TPS.Common.Model.API.StructureSet;
using VMSStudy = VMS.TPS.Common.Model.API.Study;

namespace nnunet_client.UI
{
    public partial class AutoContourControl : UserControl
    {
        private Dictionary<string, esapi.SegmentationTemplate> _templates;

        private VMSImage _image=null;
        public Dictionary<string, string> NewContourIdDictionary;

        public AutoContourControl()
        {
            InitializeComponent();
            LoadTemplates();
        }

        public void SetImage(VMSImage image)
        {
            _image = image;
            ImageId.Content = "Image: " + _image.Id;
        }

        private void LoadTemplates()
        {
            string dataDir = System.Configuration.ConfigurationManager.AppSettings["data_dir"];
            string templateDir = Path.Combine(dataDir, "seg", "templates");

            if (!Directory.Exists(templateDir))
            {
                MessageBox.Show($"Template folder not found:\n{templateDir}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TemplateSelector.ItemsSource = null;
                _templates = new Dictionary<string, SegmentationTemplate>();
                return;
            }

            try
            {
                TemplateManager templateManager = new TemplateManager();
                templateManager.LoadTemplates(templateDir);
                _templates = templateManager.Templates;
                TemplateSelector.ItemsSource = _templates.Keys;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load templates:\n{ex.Message}",
                                "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _templates = new Dictionary<string, SegmentationTemplate>();
                TemplateSelector.ItemsSource = null;
            }

        }


        esapi.SegmentationTemplate _selectedTemplate = null;
        private void TemplateSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TemplateSelector.SelectedItem is string selectedName &&
                _templates.TryGetValue(selectedName, out var template))
            {
                SegTemplateEditor.SetTemplate(template);

                _selectedTemplate = template;
            }
        }
        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            // Ensure image is selected
            if (_image == null)
            {
                MessageBox.Show("No image selected.");
                return;
            }

            string dataDir = System.Configuration.ConfigurationManager.AppSettings["data_dir"];
            string casesDir = Path.Combine(dataDir, "seg", "cases");
            string reqImageId = $"{_image.Id}!{_image.UID}!{_image.FOR}!{global.vmsPatient.Id}";
            string caseDir = Path.Combine(casesDir, global.vmsPatient.Id, reqImageId);

            helper.log($"caseDir={caseDir}");

            if (!Directory.Exists(caseDir))
            {
                helper.log("caseDir not found. Creating directory...");
                Directory.CreateDirectory(caseDir);
            }

            helper.log("Exporting image...");
            esapi.exporter.export_image(_image, caseDir, "image");
            helper.log("Image exported.");

            SegmentationTemplate template = _selectedTemplate;

            List<string> uniqueModelIds = template.ContourList
                                                  .Select(c => c.ModelId)
                                                  .Where(id => !string.IsNullOrEmpty(id))
                                                  .Distinct()
                                                  .ToList();

            helper.log($"uniqueModelIds={string.Join(", ", uniqueModelIds)}");

            foreach (string modelId in uniqueModelIds)
            {
                try
                {
                    string reqDir = Path.Combine(caseDir, modelId);
                    Directory.CreateDirectory(reqDir);

                    string datasetId = modelId.Split('.')[0]; // Removes .lowres or similar suffix
                    string imagePath = Path.Combine(caseDir, "image.mha");

                    var response = await PostImageForPredection(imagePath, reqImageId, datasetId);
                    helper.log($"Response for {modelId}: {response}");

                    // save response
                    string json = JsonConvert.SerializeObject(response, Formatting.Indented);
                    string responseFile = Path.Combine(reqDir, "req.response.json");
                    File.WriteAllText(responseFile, json);

                    // save template
                    string templateFile = Path.Combine(reqDir, "template.json");
                    File.WriteAllText(templateFile, JsonConvert.SerializeObject(template, Formatting.Indented));
                }
                catch (Exception ex)
                {
                    helper.log($"Error during prediction for {modelId}: {ex.Message}");
                }
            }

            MessageBox.Show("Prediction requests submitted.");
        }



        private async Task<dynamic> PostImageForPredection(string imagePath, string imageId, string datasetId)
        {
            helper.log($"PostImageForPredection(imagePath={imagePath},imageId={imageId},datasetId={datasetId})");

            // submit for auto contouring using nnunet api client
            string nnunetServerUrl = System.Configuration.ConfigurationManager.AppSettings["nnunet_server_url"];
            nnUNetServicClient client = new nnUNetServicClient(nnunetServerUrl);

            try
            {
                
                string requesterId = "vtk_image_labeler_3d@varianEclipseTest";

                var metadata = new Dictionary<string, string>
        {
            { "requester_id", requesterId },
            { "image_id", imageId },
            { "user", "Jinkoo Kim" },
            { "email", "jinkoo.kim@stonybrookmedicine.edu" },
            { "institution", "Stony Brook" },
            { "notes", "submitted for prediction" },
                    {"dataset_id", datasetId }
        };


                requesterId = metadata["requester_id"];
                imageId = metadata["image_id"];

                var response = await client.PostImageForPredictionAsync(datasetId, imagePath, requesterId, imageId, metadata);

                helper.log($"dataset_updated={response}");

                return response;
            }
            catch (Exception ex)
            {
                helper.log($"Request failed: {ex.Message}");
                return null;
            }
        

        }

        private async void CheckStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (_image == null || _selectedTemplate == null)
            {
                MessageBox.Show("No image or template selected.");
                return;
            }

            await SegTemplateEditor.UpdateStatus(_image);
        }

        private async void ImportContoursButton_Click(object sender, RoutedEventArgs e)
        {
            helper.log("ImportContoursButton_Click()");

            if (_image == null || _selectedTemplate == null)
            {
                MessageBox.Show("No image or template selected.");
                return;
            }

            SegmentationTemplate template = SegTemplateEditor.GetTemplate();
            string dataDir = System.Configuration.ConfigurationManager.AppSettings["data_dir"];
            string casesDir = Path.Combine(dataDir, "seg", "cases");
            string reqImageId = $"{_image.Id}!{_image.UID}!{_image.FOR}!{global.vmsPatient.Id}";
            string caseDir = Path.Combine(casesDir, global.vmsPatient.Id, reqImageId);
            helper.log($"dataDir={dataDir}");
            helper.log($"casesDir={casesDir}");
            helper.log($"reqImageId={reqImageId}");
            helper.log($"caseDir={caseDir}");


            string nnunetServerUrl = System.Configuration.ConfigurationManager.AppSettings["nnunet_server_url"];
            helper.log($"nnunetServerUrl={nnunetServerUrl}");
            var client = new nnUNetServicClient(nnunetServerUrl);

            List<VMSStructureSet> sset_list = esapi.esapi.sset_list_of_image_id_FOR(_image.Id, _image.FOR, global.vmsPatient);
            if (sset_list.Count == 0)
            {
                MessageBox.Show("Image has no StructureSet");
                return;
            }
            else if (sset_list.Count > 1)
            {
                MessageBox.Show("Image has multiple StructureSets (ambiguous)");
                return;
            }

            VMSStructureSet sset = sset_list[0];
            helper.log($"sset={sset.Id}");

            global.vmsPatient.BeginModifications();

            Dictionary<string, string> newContourIds = new Dictionary<string, string>();
            foreach (ContourItem contour in template.ContourList)
            {
                helper.log($"Importing counter...Id={contour.Id}");

                string modelId = contour.ModelId;
                if (string.IsNullOrEmpty(modelId))
                    continue;

                string reqDir = Path.Combine(caseDir, modelId);
                string responseFile = Path.Combine(reqDir, "req.response.json");

                helper.log($"reqDir={reqDir}");
                helper.log($"responseFile={responseFile}");

                if (!File.Exists(responseFile))
                {
                    helper.log($"Missing response file: {responseFile}");
                    continue;
                }

                dynamic response = JsonConvert.DeserializeObject(File.ReadAllText(responseFile));
                string reqId = response?.req_id;
                if (string.IsNullOrEmpty(reqId))
                {
                    helper.log($"req_id missing in {responseFile}");
                    continue;
                }

                string datasetId = modelId.Split('.')[0];
                int imageNumber = 0;
                int contourNumber = contour.ModelLabelNumber;
                string coordinateSystem = "w";
                string jsonPath = Path.Combine(reqDir, $"contour_{contourNumber}.points_{coordinateSystem}.json");

                try
                {
                    if (!File.Exists(jsonPath))
                    {
                        helper.log($"Requesting contour from server for modelId={modelId}, label={contour.ModelLabelName}...");
                        var result = await client.GetContourPointsAsync(datasetId, reqId, imageNumber, contourNumber, coordinateSystem);

                        if (result is Newtonsoft.Json.Linq.JObject obj &&
                            obj.TryGetValue($"points_{coordinateSystem}", out var contourData))
                        {
                            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(contourData, Formatting.Indented));
                            helper.log($"Saved contour JSON to {jsonPath}");
                        }
                        else
                        {
                            helper.log($"Key points_{coordinateSystem} not found in server response for modelId={modelId}");
                            continue;
                        }
                    }
                    else
                    {
                        helper.log($"Contour file already exists, skipping download: {jsonPath}");
                    }

                    // contour name
                    string newContourId = contour.Id;
                    int count = 1;
                    while (esapi.esapi.s_of_id(newContourId, sset, false) != null)
                    {
                        newContourId = $"{contour.Id}_{count}";
                        count++;
                    }

                    // Load points into StructureSet
                    string dicomType = string.IsNullOrEmpty(contour.Type) ? "ORGAN" : contour.Type;
                    helper.log($"Using DICOM type '{dicomType}' for structure '{newContourId}'");
                    VMSStructure structure = esapi.esapi.find_or_add_s(dicomType, newContourId, sset);
                    if (structure != null)
                    {
                        esapi.esapi.s_load_contour_data_from_cont_json_file(structure, jsonPath);
                        structure.Color = contour.Color;  // Applies the template color to the structure
                        helper.log($"Loaded contour into StructureSet: {structure.Id}, color={contour.Color}");

                        newContourIds.Add(contour.Id, newContourId);
                    }
                    else
                    {
                        helper.log($"Failed to find or create structure for label: {contour.ModelLabelName}");
                    }
                }
                catch (Exception ex)
                {
                    helper.log($"Error processing contour for modelId={modelId}: {ex.Message}");
                    MessageBox.Show(
                        $"Failed to import contour for:\n\nModel ID: {modelId}\nLabel: {contour.ModelLabelName}\n\n{ex.Message}",
                        "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // save dictionary
            NewContourIdDictionary = newContourIds;

            global.vmsApplication.SaveModifications();
            MessageBox.Show($"Contours imported ({string.Join(",", newContourIds.Values)})");


        }

    }
}
