using esapi;
using Newtonsoft.Json;
using nnunet;
using System;
using System.Collections.Generic;
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
using nnunet_client.models;
using System.Collections.ObjectModel;
using System.Windows.Forms;

namespace nnunet_client.viewmodels
{
    public class SegmentationTemplateEditorViewModel : BaseViewModel
    {
        private SegmentationTemplate _template = new SegmentationTemplate();
        public SegmentationTemplate Template
        {
            get => _template;
            set => SetProperty<SegmentationTemplate>(ref _template, value);
        }

        private VMSImage _image;
        public VMSImage Image
        {
            get => _image;
            set
            {
                if (_image != value)
                {
                    // clear status for new image
                    helper.log_for_debug("Clearing Status for a new image set");
                    ClearStatus();

                    SetProperty<VMSImage>(ref _image, value, nameof(Image));
                }
            }
        }

        public void ClearStatus()
        {
            if (_template != null)
            {
                foreach (var contour in _template.ContourList)
                {
                    contour.Status = "";
                }
            }
        }

        public async Task<dynamic> UpdateStatus()
        {
            if (_image == null)
            {
                ClearStatus();
                return "ERROR";
            }

            if (_template == null)
            {
                return "ERROR";
            }

            string dataDir = global.appConfig.app_data_dir;
            string casesDir = helper.join(helper.join(dataDir, "seg"), "cases");
            string reqImageId = $"{_image.Id}!{_image.UID}!{_image.FOR}!{global.vmsPatient.Id}";
            string caseDir = helper.join(helper.join(casesDir, global.vmsPatient.Id), reqImageId);

            string nnunetServerUrl = global.appConfig.nnunet_server_url;
            nnUNetServicClient client = new nnUNetServicClient(nnunetServerUrl);

            // Cache to avoid repeated server calls
            Dictionary<string, string> modelIdToStatus = new Dictionary<string, string>();

            foreach (var contour in _template.ContourList)
            {
                if (string.IsNullOrWhiteSpace(contour.ModelId))
                {
                    contour.Status = "No Model";
                    continue;
                }

                string modelId = contour.ModelId;
                if (modelIdToStatus.TryGetValue(modelId, out string cachedStatus))
                {
                    contour.Status = cachedStatus;
                    continue;
                }

                string datasetId = modelId.Split('.')[0];
                string reqDir = helper.join(caseDir, modelId);
                string responseFile = helper.join(reqDir, "req.response.json");

                if (!Directory.Exists(reqDir) || !File.Exists(responseFile))
                {
                    contour.Status = "Not Submitted";
                    modelIdToStatus[modelId] = "Not Submitted";
                    continue;
                }

                try
                {
                    dynamic responseObj = JsonConvert.DeserializeObject(File.ReadAllText(responseFile));
                    string reqId = responseObj?.req_id;

                    if (string.IsNullOrEmpty(reqId))
                    {
                        contour.Status = "Invalid Req";
                        modelIdToStatus[modelId] = "Invalid Req";
                        continue;
                    }

                    dynamic prediction = await client.GetPredictionAsync(datasetId, reqId);
                    bool completed = prediction?.completed == true;
                    //int count = prediction?.output_labels?.Count ?? 0; // this will be always 1 for this type of request
                    string status = completed ? $"Done ({reqId})" : $"In-Queue ({reqId})";
                    contour.Status = status;
                    modelIdToStatus[modelId] = status;
                }
                catch (Exception ex)
                {
                    string errStatus = $"Error: {ex.Message}";
                    contour.Status = errStatus;
                    modelIdToStatus[modelId] = errStatus;
                }
            }

            return "OK";
        }
    }
}
