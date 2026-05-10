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
        private Panel _searchpanel;
        private Panel _infopanel;
        private ContextMenuStrip _contextMenu;

        public AsyncDirectoryUtility()
            {
            this.Text = "Async Internal Explorer";
            this.Size = new Size(700, 550);

            // Setup Context Menu
            _contextMenu = new ContextMenuStrip();
            var settingsItem = new ToolStripMenuItem("Settings...", null, settingsMenuItem_Click);
            _contextMenu.Items.Add(settingsItem);
            this.ContextMenuStrip = _contextMenu;

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
                Height = 60,
                Dock = DockStyle.Bottom,
                Padding = new Padding(10)
                };

            // 3. Setup the ListBox
            _fileList = new ListBox
                {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 12),
                IntegralHeight = false,
                ContextMenuStrip = _contextMenu
                };

            // 4. Setup search bar
            _pathInput = new TextBox
                {
                Text = @"C:\",
                Location = new Point(10, 15),
                Width = 400,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };

            // 5. Setup Scan button
            _btnScan = new Button
                {
                Text = "Scan",
                Location = new Point(120, 13),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
            // 6. Setup Home button
            _btnhome = new Button
                {
                Text = "Home",
                Location = new Point(30, 13),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
                };

            // 7. Setup Back button
            _btnBack = new Button
                {
                Text = "Back",
                Dock = DockStyle.Right,
                Width = 100,
                Enabled = false
                };

            // 8. Setup Status Label
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

            _infopanel.Controls.Add(_btnBack);
            _infopanel.Controls.Add(_statusLabel);

            // --- Adding Panels to Form ---
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
            _btnScan.Enabled = false;
            _statusLabel.Text = "Scanning...";

            var progress = new Progress<FileItem>(fileName =>
            {
                _fileList.Items.Add(fileName);
                _fileList.TopIndex = _fileList.Items.Count - 1;
            });

            try
                {
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
            _fileList.MouseDoubleClick -= OnItemDoubleClick; // Avoid multiple subscriptions
            _fileList.MouseDoubleClick += OnItemDoubleClick;
            }

        private async void OnItemDoubleClick(object? sender, MouseEventArgs e)
            {
            int index = _fileList.IndexFromPoint(e.Location);
            if (index == ListBox.NoMatches) return;

            object? obj = _fileList.Items[index];
            if (obj is not FileItem item) return;

            try
                {
                if (item.IsDirectory)
                    {
                    _pathInput.Text = item.FullPath;
                    await StartScanAsync(item.FullPath);
                    }
                else
                    {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                        FileName = item.FullPath,
                        UseShellExecute = true
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
            if (AppSettings.HomeOrRoot)
                {
                string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                await StartScanAsync(homePath);
                }
            else
                {
                string rootPath = Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\";
                await StartScanAsync(rootPath);
                }
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
                        if (!AppSettings.ShowHiddenFiles && (info.Attributes & FileAttributes.Hidden) != 0)
                            {
                            continue;
                            }

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
                        }
                    }
                }
            catch (Exception ex)
                {
                LogError(ex);
                }
            }

        private void LogException(Exception ex)
            {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]" + Environment.NewLine +
                    $"Message: {ex.Message}" + Environment.NewLine +
                    $"StackTrace: {ex.StackTrace}" + Environment.NewLine +
                    new string('-', 40) + Environment.NewLine;

            try { File.AppendAllText(logPath, logEntry); } catch { }
            }

        private void LogToWindowsEventLog(Exception ex)
            {
            string sourceName = "AsyncExplorer";
            string logName = "Application";

            try
                {
                if (!EventLog.SourceExists(sourceName))
                    {
                    EventLog.CreateEventSource(sourceName, logName);
                    }

                string message = $"Message: {ex.Message}\nStack Trace: {ex.StackTrace}";
                EventLog.WriteEntry(sourceName, message, EventLogEntryType.Error, 1001);
                }
            catch
                {
                LogException(ex);
                }
            }

        public void LogError(Exception ex)
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

        private void settingsMenuItem_Click(object? sender, EventArgs e)
            {
            using (var diag = new SettingsForm())
                {
                diag.ShowDialog();
                // Settings are saved in the dialog
                }
            }
        }
    }
