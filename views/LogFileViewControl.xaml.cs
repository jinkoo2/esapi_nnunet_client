using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading; // Required for Dispatcher

namespace nnunet_client.views
{
    public partial class LogFileViewControl : UserControl
    {
        private DispatcherTimer _timer;
        private string _logFilePath;
        private FileSystemWatcher _watcher;

        public LogFileViewControl()
        {
            InitializeComponent();
        }

        // Helper method to ensure the ScrollToEnd() call is executed after the UI updates
        private void DeferScrollToEnd()
        {
            // BeginInvoke runs the action with a low priority (Background), ensuring 
            // the layout (which updates the scrollable height) finishes first.
            this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                LogTextBox.ScrollToEnd();
            }));
        }

        // Polling logic
        public void StartLogPolling(string logFilePath)
        {
            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir); // Ensure directory exists

                string fileName = $"log-{DateTime.Now:yyyy.MM.dd-HH.mm.ss}.txt";
                logFilePath = Path.Combine(logDir, fileName);
            }

            _logFilePath = logFilePath;

            Console.WriteLine($"Log file = {logFilePath}");

            // Ensure the file exists
            if (!File.Exists(_logFilePath))
                File.WriteAllText(_logFilePath, "");

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(3000);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Code here runs on the UI thread
            RefreshLogView();
        }

        private void RefreshLogView()
        {
            //Console.WriteLine($"Polling...{_logFilePath}");

            if (File.Exists(_logFilePath))
            {
                // Attempt to read file content (may need retries if file is locked)
                string[] lines = ReadAllLinesWithRetry(_logFilePath);

                LogTextBox.Text = string.Join(Environment.NewLine, lines);

                // CRITICAL FIX: Defer the scroll action
                DeferScrollToEnd();
            }
        }

        public void StopLogPolling()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
                _timer = null;
            }
        }

        // FileSystemWatcher logic
        public void StartMonitoring(string logFilePath)
        {
            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir); // Ensure directory exists

                string fileName = $"log-{DateTime.Now:yyyy.MM.dd-HH.mm.ss}.txt";
                logFilePath = Path.Combine(logDir, fileName);
            }

            _logFilePath = logFilePath;

            Console.WriteLine($"Log file = {logFilePath}");

            // Ensure the file exists
            if (!File.Exists(_logFilePath))
                File.WriteAllText(_logFilePath, "");

            _watcher = new FileSystemWatcher(Path.GetDirectoryName(_logFilePath))
            {
                Filter = Path.GetFileName(_logFilePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _watcher.Changed += OnLogFileChanged;
            _watcher.EnableRaisingEvents = true;
        }

        private void OnLogFileChanged(object sender, FileSystemEventArgs e)
        {
            // The FileSystemWatcher event runs on a worker thread. We must invoke the UI update.
            try
            {
                // Use the retry logic instead of a fixed Thread.Sleep, as it's more robust
                string[] lines = ReadAllLinesWithRetry(_logFilePath);

                // Use the Dispatcher to update the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LogTextBox.Text = string.Join(Environment.NewLine, lines);

                    // CRITICAL FIX: Defer the scroll action
                    DeferScrollToEnd();
                });
            }
            catch (Exception ex)
            {
                // Log or handle file access/other exceptions gracefully
                Console.WriteLine($"Error reading log file: {ex.Message}");
            }
        }

        public void StopMonitoring()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        public void AppendLine(string line)
        {
            System.IO.File.AppendAllText(_logFilePath, line + Environment.NewLine);
        }

        // Added helper for robust file reading in case of locks
        private string[] ReadAllLinesWithRetry(string path)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    // This is a blocking file read, which is necessary here
                    return File.ReadAllLines(path);
                }
                catch (IOException ex) when (ex.Message.Contains("being used by another process"))
                {
                    // Wait a short time before retrying
                    Thread.Sleep(100);
                }
            }
            return new string[] { $"ERROR: Could not read log file: {_logFilePath}" };
        }

        public string LogFilePath
        {
            get
            {
                return _logFilePath;
            }
        }
    }
}
