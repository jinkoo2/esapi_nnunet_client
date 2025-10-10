using esapi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static esapi.esapi;
using VMSCourse = VMS.TPS.Common.Model.API.Course;
using VMSHospital = VMS.TPS.Common.Model.API.Hospital;
using VMSImage = VMS.TPS.Common.Model.API.Image;
using VMSPatient = VMS.TPS.Common.Model.API.Patient;
using VMSPlanSetup = VMS.TPS.Common.Model.API.PlanSetup;
using VMSReferencePoint = VMS.TPS.Common.Model.API.ReferencePoint;
using VMSRegistration = VMS.TPS.Common.Model.API.Registration;
using VMSSeries = VMS.TPS.Common.Model.API.Series;
using VMSStructure = VMS.TPS.Common.Model.API.Structure;
using VMSStructureSet = VMS.TPS.Common.Model.API.StructureSet;
using VMSStudy = VMS.TPS.Common.Model.API.Study;
using Newtonsoft.Json;
using System.Collections;
using ControlzEx.Helpers;
using nnunet_client.models;

namespace nnunet_client.viewmodels
{
    public class AutoContourViewModel : BaseViewModel
    {
        // constructor
        public AutoContourViewModel() {
            nnUNetServerURL = global.nnunet_server_url;
            helper.log($"nnUNetServerURL={nnUNetServerURL}");
        }

        private VMSImage _image;
        public VMSImage Image
        {
            get => _image;
            set{

                if (_image == value) 
                    return;

                SetProperty<VMSImage>(ref _image, value);

                if(this.SegmentationTemplateEditorViewModel != null)
                    this.SegmentationTemplateEditorViewModel.Image = value;
            }
        }

        private ObservableCollection<SegmentationTemplate> _templates;
        public ObservableCollection<SegmentationTemplate> Templates
        {
            get => _templates;
            set => SetProperty<ObservableCollection<SegmentationTemplate>>(ref _templates, value);
        }


        private SegmentationTemplate _selectedTemplate;
        public SegmentationTemplate SelectedTemplate
        {
            get => _selectedTemplate;
            set {

                if(_selectedTemplate == value) return;

                SetProperty<SegmentationTemplate>(ref _selectedTemplate, value);

                Console.WriteLine("Setting selected template to SemenationTempalteEidorViewModel");
                foreach(SegmentationTemplate.ContourItem item in value.ContourList)
                {
                    Console.WriteLine(item.Id);
                }

                this.SegmentationTemplateEditorViewModel.Template = value;
            }
        }

        public void LoadTemplates(string templateDir)
        {
            TemplateManager<SegmentationTemplate> templateManager = new TemplateManager<SegmentationTemplate>();
            templateManager.LoadTemplates(templateDir);
            Templates = templateManager.Templates;
        }

        private SegmentationTemplateEditorViewModel _segmentationTemplateEditorViewModel = new SegmentationTemplateEditorViewModel();
        public SegmentationTemplateEditorViewModel SegmentationTemplateEditorViewModel
        {
            get => _segmentationTemplateEditorViewModel;
            set => SetProperty<SegmentationTemplateEditorViewModel>(ref _segmentationTemplateEditorViewModel, value);
        }

        public async Task SubmitPredictionRequests()
        {
            // Ensure image is selected
            if (_image == null)
                throw new Exception("Image not set");

            string dataDir = global.app_data_dir;
            string casesDir = Path.Combine(dataDir, "seg", "cases");
            string reqImageId = $"{_image.Id}!{_image.UID}!{_image.FOR}!{global.vmsPatient.Id}";
            string caseDir = Path.Combine(casesDir, global.vmsPatient.Id, reqImageId);

            helper.log($"caseDir={caseDir}");

            if (!Directory.Exists(caseDir))
            {
                helper.log("caseDir not found. Creating directory...");
                Directory.CreateDirectory(caseDir);
            }

            helper.log($"Exporting image from Eclipse to {caseDir} ...");
            esapi.exporter.export_image(_image, caseDir, "image");
            helper.log("Image exported.");

            SegmentationTemplate template = SelectedTemplate;

            List<string> uniqueModelIds = template.ContourList
                                                  .Select(c => c.ModelId)
                                                  .Where(id => !string.IsNullOrEmpty(id))
                                                  .Distinct()
                                                  .ToList();

            helper.log($"uniqueModelIds={string.Join(", ", uniqueModelIds)}");

            helper.log("Submitting requests...");
            foreach (string modelId in uniqueModelIds)
            {
                helper.log($"ModelId={modelId}");
                try
                {
                    string reqDir = Path.Combine(caseDir, modelId);
                    helper.log($"Creating request dir [{reqDir}]");
                    Directory.CreateDirectory(reqDir);

                    string datasetId = modelId.Split('.')[0]; // Removes .lowres or similar suffix
                    string imagePath = Path.Combine(caseDir, "image.mha");

                    helper.log($"Posting Image for prediction...");
                    var response = await PostImageForPredection(imagePath, reqImageId, datasetId);
                    helper.log($"Response for {modelId}: {response}");

                    // save response
                    string json = JsonConvert.SerializeObject(response, Formatting.Indented);
                    string responseFile = Path.Combine(reqDir, "req.response.json");
                    helper.log($"Saving response to {responseFile}...");
                    File.WriteAllText(responseFile, json);

                    // save template
                    string templateFile = Path.Combine(reqDir, "template.json");
                    helper.log($"Saving template to {templateFile}...");
                    File.WriteAllText(templateFile, JsonConvert.SerializeObject(template, Formatting.Indented));
                }
                catch (Exception ex)
                {
                    helper.log($"Error during prediction submission for {modelId}: {ex.Message}");
                }
            }

            helper.log("Done submitting all prediction requests.");
        }

        private string _nnUNetServerURL;
        public string nnUNetServerURL
        {
            get { return _nnUNetServerURL; }
            set=>SetProperty<string>(ref _nnUNetServerURL, value);
        }

        private async Task<dynamic> PostImageForPredection(string imagePath, string imageId, string datasetId)
        {
            helper.log($"PostImageForPredection(imagePath={imagePath},imageId={imageId},datasetId={datasetId})");

            if (nnUNetServerURL == null || nnUNetServerURL.Length == 0)
                throw new Exception("nnUNetServerURL not set");

            // submit for auto contouring using nnunet api client
            nnunet.nnUNetServicClient client = new nnunet.nnUNetServicClient(nnUNetServerURL);

            try
            {
                string requesterId = global.nnunet_requester_id;

                var metadata = new Dictionary<string, string>
                {
                    { "requester_id", requesterId },
                    { "image_id", imageId },
                    { "user", global.nnunet_request_user_name },
                    { "email", global.nnunet_request_user_email },
                    { "institution", global.nnunet_request_user_institution },
                    { "notes", "submitted for prediction" },
                            {"dataset_id", datasetId }
                };


                requesterId = metadata["requester_id"];
                imageId = metadata["image_id"];

                var response = await client.PostImageForPredictionAsync(datasetId, imagePath, requesterId, imageId, metadata);

                helper.log($"PostImageForPredictionAsync response={response}");

                return response;
            }
            catch (Exception ex)
            {
                helper.log($"Request failed: {ex.Message}");
                return null;
            }


        }

        public async Task<Dictionary<string, string>> ImportPredictedContours()
        {
            helper.log("ImportContoursButton_Click()");

            if (Image == null || SelectedTemplate == null)
            {
                throw new Exception("No image or template selected.");
            }

            SegmentationTemplate template = this.SegmentationTemplateEditorViewModel.Template;
            string dataDir = global.app_data_dir;
            string casesDir = Path.Combine(dataDir, "seg", "cases");
            string reqImageId = $"{_image.Id}!{_image.UID}!{_image.FOR}!{global.vmsPatient.Id}";
            string caseDir = Path.Combine(casesDir, global.vmsPatient.Id, reqImageId);
            helper.log($"dataDir={dataDir}");
            helper.log($"casesDir={casesDir}");
            helper.log($"reqImageId={reqImageId}");
            helper.log($"caseDir={caseDir}");

            helper.log($"nnunetServerUrl={nnUNetServerURL}");
            var client = new nnunet.nnUNetServicClient(nnUNetServerURL);

            List<VMSStructureSet> sset_list = esapi.esapi.sset_list_of_image_id_FOR(_image.Id, _image.FOR, global.vmsPatient);
            if (sset_list.Count == 0)
            {
                throw new Exception("Image has no StructureSet");
            }
            else if (sset_list.Count > 1)
            {
                throw new Exception("Image has multiple StructureSets (ambiguous)");
            }

            VMSStructureSet sset = sset_list[0];
            helper.log($"sset={sset.Id}");

            global.vmsPatient.BeginModifications();

            Dictionary<string, string> newContourIds = new Dictionary<string, string>();
            foreach (SegmentationTemplate.ContourItem contour in template.ContourList)
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
                        helper.log($"Requesting contour points from server for modelId={modelId}, label={contour.ModelLabelName}...");
                        var result = await client.GetContourPointsAsync(datasetId, reqId, imageNumber, contourNumber, coordinateSystem);

                        if (result is Newtonsoft.Json.Linq.JObject obj &&
                            obj.TryGetValue($"points_{coordinateSystem}", out var contourData))
                        {
                            helper.log($"Saved contour JSON to {jsonPath}...");
                            File.WriteAllText(jsonPath, JsonConvert.SerializeObject(contourData, Formatting.Indented));
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

                    // contour name (increase number if exists)
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
                    throw new Exception($"Failed to import contour for:\n\nModel ID: {modelId}\nLabel: {contour.ModelLabelName}\n\n{ex.Message}");
                }
            }

            global.vmsApplication.SaveModifications();

            helper.log($"Contours imported ({string.Join(",", newContourIds.Values)})");

            return newContourIds;
        }



    }
}
