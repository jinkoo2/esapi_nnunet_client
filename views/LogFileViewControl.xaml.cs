// LogFileViewControl.xaml.cs
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;


namespace nnunet_client.views
{
    public partial class LogFileViewControl : UserControl
    {


        private DispatcherTimer _timer;

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
                string[] lines = File.ReadAllLines(_logFilePath);
                LogTextBox.Text = string.Join(Environment.NewLine, lines);
                LogTextBox.ScrollToEnd();
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


        private string _logFilePath;
        public string LogFilePath
        {
            get
            {
                return _logFilePath;
            }
        }        

        private FileSystemWatcher _watcher;

        public LogFileViewControl()
        {
            InitializeComponent();
        }

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
            try
            {
                Thread.Sleep(100); // Allow file to unlock
                string[] lines = File.ReadAllLines(_logFilePath);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    LogTextBox.Text = string.Join(Environment.NewLine, lines);
                    LogTextBox.ScrollToEnd();
                });
            }
            catch (IOException) { }
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

    }
}
