using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using nnunet_client.models;
using nnunet_client.services;
using nnunet;
using static esapi.esapi;
using itk.simple;
using VMS.TPS.Common.Model.API;
using VMSStructure = VMS.TPS.Common.Model.API.Structure;
using VMSStructureSet = VMS.TPS.Common.Model.API.StructureSet;
using VMSImage = VMS.TPS.Common.Model.API.Image;

namespace nnunet_client
{
    /// <summary>
    /// Worker program that processes jobs from the submit queue.
    /// This should be run as a separate process/service.
    /// </summary>
    public class SubmitJobWorker
    {
        private readonly JobQueueService _jobQueueService;
        private readonly nnUNetServicClient _client;
        private readonly string _nnUNetServerURL;
        private bool _shouldStop = false;

        public string QueueDirectory => _jobQueueService.QueueDirectory;

        public SubmitJobWorker(string queueDirectory = null)
        {
            _jobQueueService = new JobQueueService(queueDirectory);
            _nnUNetServerURL = global.appConfig.nnunet_server_url;
            _client = new nnUNetServicClient(_nnUNetServerURL, global.appConfig.nnunet_server_auth_token);
        }

        public void Start()
        {
            helper.log("SubmitJobWorker started. Processing jobs from queue...");
            helper.log($"Queue directory: {_jobQueueService.QueueDirectory}");

            while (!_shouldStop)
            {
                try
                {
                    ProcessNextJob();
                }
                catch (Exception ex)
                {
                    helper.log($"Error in worker loop: {ex.Message}");
                }

                // Sleep for a bit before checking for more jobs
                Thread.Sleep(5000); // Check every 5 seconds
            }

            helper.log("SubmitJobWorker stopped.");
        }

        public void Stop()
        {
            _shouldStop = true;
        }

        private void ProcessNextJob()
        {
            var pendingJobs = _jobQueueService.GetPendingJobs();
            if (pendingJobs.Count == 0)
            {
                return; // No jobs to process
            }

            var job = pendingJobs[0]; // Process oldest job first
            helper.log($"Processing job: {job.JobId}");

            // Update status to Processing
            _jobQueueService.UpdateJobStatus(job.JobId, "Processing");

            try
            {
                ProcessJob(job);
                _jobQueueService.UpdateJobStatus(job.JobId, "Completed");
                helper.log($"Job {job.JobId} completed successfully.");
            }
            catch (Exception ex)
            {
                helper.log($"Job {job.JobId} failed: {ex.Message}");
                _jobQueueService.UpdateJobStatus(job.JobId, "Failed", ex.Message);
            }
        }

        private void ProcessJob(SubmitJob job)
        {
            helper.log($"Processing job {job.JobId}: Dataset={job.DatasetId}, ImagesFor={job.ImagesFor}");

            // Open patient and get structure set
            using (var app = VMS.TPS.Common.Model.API.Application.CreateApplication())
            {
                var patient = app.OpenPatientById(job.PatientId);
                if (patient == null)
                {
                    throw new Exception($"Patient {job.PatientId} not found");
                }

                // Find structure set
                VMSStructureSet structureSet = null;
                foreach (var sset in patient.StructureSets)
                {
                    if (sset.Id == job.StructureSetId || sset.UID == job.StructureSetUID)
                    {
                        structureSet = sset;
                        break;
                    }
                }

                if (structureSet == null)
                {
                    throw new Exception($"Structure set {job.StructureSetId} (UID: {job.StructureSetUID}) not found");
                }

                var image = structureSet.Image;
                if (image == null)
                {
                    throw new Exception("Structure set has no associated image");
                }

                // Create temporary directory for images
                string tempDir = Path.Combine(Path.GetTempPath(), $"nnunet_submit_{job.JobId}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Export base image using export_image (exports as .mha)
                    helper.log($"Exporting base image using export_image...");
                    esapi.exporter.export_image(image, tempDir, "base_image");
                    string baseImagePath = Path.Combine(tempDir, "base_image.mha");

                    // Convert base image to Int32 with compression
                    helper.log($"Converting base image to Int32 with compression...");
                    string baseImageInt32Path = Path.Combine(tempDir, "base_image_int32.mha");
                    ConvertImageToInt32(baseImagePath, baseImageInt32Path);

                    // Export each structure as mask image
                    helper.log($"Exporting structure masks...");
                    List<(string path, int labelValue)> maskInfoList = new List<(string, int)>();
                    foreach (var mapping in job.LabelMappings)
                    {
                        var structure = esapi.esapi.s_of_id(mapping.StructureId, structureSet, false);
                        if (structure == null)
                        {
                            helper.log($"Warning: Structure {mapping.StructureId} not found, skipping...");
                            continue;
                        }

                        string maskName = $"mask_{mapping.LabelName}";
                        helper.log($"Exporting mask for {mapping.LabelName} (structure: {structure.Id}, label value: {mapping.LabelValue})...");
                        esapi.exporter.export_structure_as_mask_image(image, structure, tempDir, maskName);
                        string maskPath = Path.Combine(tempDir, maskName + ".mha");
                        maskInfoList.Add((maskPath, mapping.LabelValue));
                    }

                    // Combine masks into one label image with Int32 pixel type
                    string labelsPath = Path.Combine(tempDir, "labels.mha");
                    helper.log($"Combining masks into label image at {labelsPath}...");
                    CombineMasksIntoLabelImage(maskInfoList, labelsPath);

                    // Post to server
                    helper.log($"Posting image and labels to server (datasetId={job.DatasetId}, imagesFor={job.ImagesFor})...");
                    var response = _client.PostImageAndLabelsAsync(job.DatasetId, job.ImagesFor, baseImageInt32Path, labelsPath).Result;
                    helper.log($"Submission successful: {response}");
                }
                finally
                {
                    // Clean up temporary files
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, true);
                            helper.log($"Cleaned up temporary directory: {tempDir}");
                        }
                    }
                    catch (Exception ex)
                    {
                        helper.log($"Warning: Could not delete temporary directory: {ex.Message}");
                    }

                    app.ClosePatient();
                }
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
    }
}

