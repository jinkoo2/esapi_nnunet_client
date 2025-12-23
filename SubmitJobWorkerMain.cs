using System;
using System.Threading;
using nnunet_client.services;

namespace nnunet_client
{
    /// <summary>
    /// Main entry point for the Submit Job Worker program.
    /// This should be compiled as a separate executable that runs continuously,
    /// processing jobs from the queue.
    /// </summary>
    public class SubmitJobWorkerMain
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // Load configuration
                global.load_config();

                // Optional: allow specifying queue directory via command line
                string queueDirectory = null;
                if (args.Length > 0)
                {
                    queueDirectory = args[0];
                    Console.WriteLine($"Using custom queue directory: {queueDirectory}");
                }

                // Create and start worker
                var worker = new SubmitJobWorker(queueDirectory);
                
                Console.WriteLine("Submit Job Worker started. Press Ctrl+C to stop.");
                Console.WriteLine($"Queue directory: {worker.QueueDirectory}");
                Console.WriteLine();

                // Handle Ctrl+C gracefully
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    Console.WriteLine("\nStopping worker...");
                    worker.Stop();
                };

                // Start processing jobs
                worker.Start();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("--- Worker Main Exception ---");
                Console.Error.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}

