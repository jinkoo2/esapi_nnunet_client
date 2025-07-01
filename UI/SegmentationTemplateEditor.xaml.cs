using esapi;
using Newtonsoft.Json;
using nnunet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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



    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class SegmentationTemplateEditor : UserControl
    {

        private SegmentationTemplate _template = null;


        public SegmentationTemplateEditor()
        {
            InitializeComponent();
        }

        public void SetTemplate(SegmentationTemplate template)
        {
            _template = template;

            TemplateNameBox.Text = template.Name;
            DescriptionBox.Text = template.Description;
            ContourListGrid.ItemsSource = template.ContourList;
        }

        public SegmentationTemplate GetTemplate() 
        { 
            return _template;
        }
        


        public async Task<dynamic> UpdateStatus(VMSImage _image)
        {
            if (_image == null)
            {
               // MessageBox.Show("Invalid image!");
                return "ERROR";
            }

            if (_template == null)
            {
                //MessageBox.Show("Invalid template!");
                return "ERROR";
            }

            string dataDir = System.Configuration.ConfigurationManager.AppSettings["data_dir"];
            string casesDir = helper.join(helper.join(dataDir, "seg"), "cases");
            string reqImageId = $"{_image.Id}!{_image.UID}!{_image.FOR}!{global.vmsPatient.Id}";
            string caseDir = helper.join(helper.join(casesDir, global.vmsPatient.Id), reqImageId);

            string nnunetServerUrl = System.Configuration.ConfigurationManager.AppSettings["nnunet_server_url"];
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

            ContourListGrid.Items.Refresh();
            return "OK";
        }

    }
}
