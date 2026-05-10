using System.Diagnostics;
namespace AsyncExplorer
    {

    public class AsyncDirectoryUtility : Form
        {
        private ListBox _fileList;
        private Button _btnScan;
        private Button _btnhome;
        private Button _btnBack;
        private TextBox _pathInput;
        private Label _statusLabel;
        private Stack<string> _navigationStack = new Stack<string>();
        private CheckBox _chkUseEventViewer;
        private Panel _searchpanel;
        private Panel _infopanel;

        public AsyncDirectoryUtility()
            {
            this.Text = "Async Internal Explorer";
            this.Size = new Size(700, 550); // Increased height slightly for better visibility

            // 1. Setup the Top Panel
            _searchpanel = new Panel
                {
                Height = 60,
                Dock = DockStyle.Top,
                Padding = new Padding(10)
                };

            // 2. Setup the Bottom Panel
            _infopanel = new Panel
                {
                Height = 80, // Increased to fit the button and checkbox comfortably
                Dock = DockStyle.Bottom,
                Padding = new Padding(10)
                };

            // 3. Setup the ListBox (The Centerpiece)
            _fileList = new ListBox
                {
                Dock = DockStyle.Fill, // Takes up all space between Top and Bottom panels
                Font = new Font("Consolas", 12),
                IntegralHeight = false // Prevents the box from resizing to fit font lines
                };

            // --- Control Setup ---

            _chkUseEventViewer = new CheckBox
                {
                Text = "Log to Event Viewer",
                Checked = AppSettings.UseEventViewer,
                Dock = DockStyle.Left,
                Width = 150
                };
            _chkUseEventViewer.CheckedChanged += (s, e) => AppSettings.UseEventViewer = _chkUseEventViewer.Checked;

            _pathInput = new TextBox
                {
                Text = @"C:\",
                Location = new Point(10, 15),
                Width = 400,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

            _btnScan = new Button
                {
                Text = "Scan",
                Location = new Point(120, 13),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
                };

            _btnhome = new Button
                {
                Text = "Home",
                Location = new Point(30, 13),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
                };

            _btnBack = new Button
                {
                Text = "Back",
                Dock = DockStyle.Right,
                Width = 100,
                Enabled = false
                };

            _statusLabel = new Label
                {
                Text = "Ready",
                Dock = DockStyle.Bottom,
                AutoSize = false,
                Height = 20
                };

            // Wire up events
            _btnScan.Click += async (s, e) => await StartScanAsync(_pathInput.Text);
            _btnhome.Click += (s, e) => GoHome();
            _btnBack.Click += (s, e) => GoBack();

            // --- Adding Controls to Panels ---
            _searchpanel.Controls.Add(_btnScan);
            _searchpanel.Controls.Add(_btnhome);
            _searchpanel.Controls.Add(_pathInput);

            _infopanel.Controls.Add(_chkUseEventViewer);
            _infopanel.Controls.Add(_btnBack);
            _infopanel.Controls.Add(_statusLabel);

            // --- Adding Panels to Form ---
            // Note: Add the Fill control LAST so it knows how much space is left
            this.Controls.Add(_fileList);
            this.Controls.Add(_searchpanel);
            this.Controls.Add(_infopanel);

            this.Load += Utility_Load;
            }

        private async Task StartScanAsync(string path, bool isBackNavigation = false)
            {

            if (!isBackNavigation && !string.IsNullOrEmpty(_pathInput.Text))
                {
                _navigationStack.Push(_pathInput.Text);
                _btnBack.Enabled = true;
                }
            _fileList.Items.Clear();
            _btnScan.Enabled = false; // Prevent double-clicks
            _statusLabel.Text = "Scanning...";

            // Progress reporter: This automatically runs on the UI thread
            var progress = new Progress<FileItem>(fileName =>
            {
                _fileList.Items.Add(fileName);
                // Keep the list scrolled to the bottom
                _fileList.TopIndex = _fileList.Items.Count - 1;
            });

            try
                {
                // Run the filesystem work on a background thread
                await Task.Run(() => PerformFilesystemWork(path, progress));
                _statusLabel.Text = $"Done! Found {_fileList.Items.Count} items.";
                }
            catch (Exception ex)
                {
                MessageBox.Show($"Error: {ex.Message}");
                LogError(ex);
                _statusLabel.Text = "Error occurred.";
                }
            finally
                {
                _btnScan.Enabled = true;
                SetupListBox();
                }
            }

        private void SetupListBox()
            {
            _fileList.MouseDoubleClick += OnItemDoubleClick;
            }

        private async void OnItemDoubleClick(object? sender, MouseEventArgs e)
            {
            // 1. Get the item that was clicked
            int index = _fileList.IndexFromPoint(e.Location);
            if (index == ListBox.NoMatches) return;

            object? obj = _fileList.Items[index];
            if (obj is not FileItem item) return;

            try
                {
                if (item.IsDirectory)
                    {
                    // If it's a directory, clear the list and scan the new path
                    _pathInput.Text = item.FullPath;
                    await StartScanAsync(item.FullPath);
                    }
                else
                    {
                    // If it's a file, tell the Windows Shell to open it
                    // This is equivalent to double-clicking it in File Explorer
                    ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                        FileName = item.FullPath,
                        UseShellExecute = true // Crucial: tells Windows to use the registered app
                        };
                    Process.Start(startInfo);
                    }
                }
            catch (Exception ex)
                {
                MessageBox.Show($"Could not open item: {ex.Message}");
                LogError(ex);
                }
            }

        private async void GoBack()
            {
            if (_navigationStack.Count == 0) return;
            string previousPath = _navigationStack.Pop();
            await StartScanAsync(previousPath, isBackNavigation: true);
            if (_navigationStack.Count == 0)
                {
                _btnBack.Enabled = false;
                }
            }

        private async void GoHome()
            {
            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            await StartScanAsync(homePath);
            }

        private async void Utility_Load(object? sender, EventArgs e)
            {
            string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _pathInput.Text = homePath;
            await StartScanAsync(homePath);
            }

        private void PerformFilesystemWork(string path, IProgress<FileItem> progress)
            {
            try
                {
                DirectoryInfo dir = new DirectoryInfo(path);
                if (!dir.Exists) return;

                foreach (var info in dir.GetFileSystemInfos())
                    {
                    try
                        {
                        progress.Report(new FileItem
                            {
                            Name = info.Name,
                            FullPath = info.FullName,
                            IsDirectory = (info.Attributes & FileAttributes.Directory) != 0
                            });
                        }
                    catch (UnauthorizedAccessException uae)
                        {
                        LogError(uae);
                        _statusLabel.Text = "Some Folders were skipped due to access restrictions.";
                        }
                    }
                }
            catch (Exception ex)
                {
                LogError(ex);
                MessageBox.Show($"A Critical error occurred check {ex.Message}");
                }
            }

        private void LogException(Exception ex)
            {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");

            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]" + Environment.NewLine +
                    $"Message: {ex.Message}" + Environment.NewLine +
                    $"StackTrace: {ex.StackTrace}" + Environment.NewLine +
                    new string('-', 40) + Environment.NewLine;

            File.AppendAllText(logPath, logEntry);
            }



        private void LogToWindowsEventLog(Exception ex)
            {
            string sourceName = "MySystemUtility";
            string logName = "Application";

            try
                {
                // 1. Create the source if it doesn't exist (Requires Admin)
                if (!EventLog.SourceExists(sourceName))
                    {
                    EventLog.CreateEventSource(sourceName, logName);
                    }

                // 2. Format the message
                string message = $"An unhandled exception occurred in the utility.\n\n" +
                                 $"Message: {ex.Message}\n" +
                                 $"Stack Trace: {ex.StackTrace}";

                // 3. Write the entry
                // We use 'Error' as the entry type so it gets the red 'X' icon in Event Viewer
                EventLog.WriteEntry(sourceName, message, EventLogEntryType.Error, 1001);
                }
            catch (System.Security.SecurityException)
                {
                // This happens if the app isn't running as Admin and the source doesn't exist.
                // Fall back to your text file logger here.
                LogError(ex);
                }
            catch (Exception)
                {
                // Final safety net
                }
            }

        private void LogError(Exception ex)
            {
            if (AppSettings.UseEventViewer)
                {
                LogToWindowsEventLog(ex);
                }
            else
                {
                LogException(ex);
                }
            }

        [STAThread]
        static void Main()
            {
            Application.ThreadException += (s, e) =>
            {
                new AsyncDirectoryUtility().LogError(e.Exception);
                MessageBox.Show($"UI error: {e.Exception.Message}");
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Exception ex = e.ExceptionObject as Exception ?? new Exception("Unknown error");
                new AsyncDirectoryUtility().LogError(ex);
                MessageBox.Show($"Unhandled error: {ex.Message}");
            };
            Application.EnableVisualStyles();
            Application.Run(new AsyncDirectoryUtility());
            }
        }
    }