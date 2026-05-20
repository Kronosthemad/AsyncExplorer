using System.Diagnostics;
using AsyncExplorer.Model;

namespace AsyncExplorer.Services
	{
	public class AsyncDirectoryController
		{
		private readonly ListView _fileList;
		private readonly TextBox _pathInput;
		private readonly Label _statusLabel;
		private readonly Button _btnScan;
		private readonly Button _btnBack;
		private readonly Button _btnHome;
		private readonly Button _btnRoot;
		private readonly Action<Exception> _logError;
		private readonly Stack<string> _navigationStack = new Stack<string>();

		public AsyncDirectoryController(ListView fileList, TextBox pathInput, Label statusLabel,
			Button btnScan, Button btnBack, Button btnHome, Button btnRoot, Action<Exception> logError)
			{
			_fileList = fileList;
			_pathInput = pathInput;
			_statusLabel = statusLabel;
			_btnScan = btnScan;
			_btnBack = btnBack;
			_btnHome = btnHome;
			_btnRoot = btnRoot;
			_logError = logError;
			}

		public void SetupListView()
			{
			_fileList.MouseDoubleClick -= OnItemDoubleClick;
			_fileList.MouseDoubleClick += OnItemDoubleClick;
			}

		private void OnItemDoubleClick(object? sender, MouseEventArgs e)
			{
			ListViewItem? item = _fileList.GetItemAt(e.X, e.Y);
			if (item == null || item.Tag is not FileItem fileItem) return;

			OpenSelectedItem(fileItem);
			}

		public void OpenSelectedItem(FileItem item)
			{
			try
				{
				if (item.IsDirectory)
					{
					_ = StartScanAsync(item.FullPath);
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
				_logError(ex);
				MessageBox.Show($"Could not open item: {ex.Message}");
				}
			}

		public async Task StartScanAsync(string path, bool isBackNavigation = false)
			{
			string currentUIPath = _pathInput.Text;
			string normalizedTarget = path == "::DRIVES::" ? "This PC" : path;

			if (!isBackNavigation && !string.IsNullOrEmpty(currentUIPath))
				{
				// Only push to stack if we are actually moving to a new location
				if (currentUIPath != normalizedTarget)
					{
					_navigationStack.Push(currentUIPath);
					_btnBack.Enabled = true;
					}
				}

			// Update the address bar automatically
			_pathInput.Text = normalizedTarget;

			_fileList.Items.Clear();
			_fileList.SmallImageList?.Images.Clear();
			_btnScan.Enabled = false;
			_statusLabel.Text = path == "::DRIVES::" ? "Listing Drives..." : "Scanning...";

			var progress = new Progress<ListViewItem>(item =>
			{
				_fileList.Items.Add(item);
				_fileList.EnsureVisible(_fileList.Items.Count - 1);
			});

			try
				{
				await Task.Run(() => PerformFilesystemWork(path, progress));
				_statusLabel.Text = path == "::DRIVES::" ? $"Found {_fileList.Items.Count} drives." : $"Done! Found {_fileList.Items.Count} items.";
				}
			catch (Exception ex)
				{
				MessageBox.Show($"Error: {ex.Message}");
				_logError(ex);
				_statusLabel.Text = "Error occurred.";
				}
			finally
				{
				_btnScan.Enabled = true;
				SetupListView();
				if (AppSettings.HomeOrRoot)
					{
					_btnHome.Enabled = true;
					_btnRoot.Enabled = true;
					}
				else
					{
					_btnHome.Enabled = false;
					_btnRoot.Enabled = false;
					}
				}
			}

		private void PerformFilesystemWork(string path, IProgress<ListViewItem> progress)
			{
			try
				{
				if (path == "::DRIVES::")
					{
					foreach (var drive in DriveInfo.GetDrives())
						{
						if (drive.IsReady)
							{
							string driveName = $"{drive.Name} {(string.IsNullOrEmpty(drive.VolumeLabel) ? "" : $"({drive.VolumeLabel})")}";
							string fullPath = drive.RootDirectory.FullName;
							
							var item = new FileItem
								{
								Name = driveName,
								FullPath = fullPath,
								IsDirectory = true
								};

							var lvItem = CreateListViewItem(item);
							progress.Report(lvItem);
							}
						}
					return;
					}

				DirectoryInfo dir = new DirectoryInfo(path);
				if (!dir.Exists) return;

				foreach (var info in dir.GetFileSystemInfos())
					{
					try
						{
						// Skip hidden files or files starting with a dot if the setting is disabled
						if (!AppSettings.ShowHiddenFiles && ((info.Attributes & FileAttributes.Hidden) != 0 || info.Name.StartsWith(".", StringComparison.Ordinal)))
							{
							continue;
							}

						var item = new FileItem
							{
							Name = info.Name,
							FullPath = info.FullName,
							IsDirectory = (info.Attributes & FileAttributes.Directory) != 0
							};

						var lvItem = CreateListViewItem(item);
						progress.Report(lvItem);
						}
					catch (UnauthorizedAccessException uae)
						{
						_logError(uae);
						}
					}
				}
			catch (Exception ex)
				{
				_logError(ex);
				}
			}

		private ListViewItem CreateListViewItem(FileItem item)
			{
			var lvItem = new ListViewItem(item.Name)
				{
				Tag = item
				};

			if (AppSettings.UseIcons && _fileList.SmallImageList != null)
				{
				string imageKey = item.IsDirectory ? "folder" : Path.GetExtension(item.FullPath).ToLower();
				if (string.IsNullOrEmpty(imageKey)) imageKey = "file";

				// We need to ensure the icon is in the ImageList. 
				// Since we're on a background thread, we need to Invoke to add to ImageList.
				if (!_fileList.SmallImageList.Images.ContainsKey(imageKey))
					{
					_fileList.Invoke((MethodInvoker)delegate
					{
						if (!_fileList.SmallImageList.Images.ContainsKey(imageKey))
							{
							Icon? icon = IconHelper.GetIcon(item.FullPath, item.IsDirectory);
							if (icon != null)
								{
								_fileList.SmallImageList.Images.Add(imageKey, icon);
								}
							}
					});
					}
				lvItem.ImageKey = imageKey;
				}

			return lvItem;
			}

		public async Task GoBack()
			{
			if (_navigationStack.Count == 0) return;
			string previousPath = _navigationStack.Pop();
			await StartScanAsync(previousPath, isBackNavigation: true);
			if (_navigationStack.Count == 0)
				{
				_btnBack.Enabled = false;
				}
			}

		public async Task GoHome()
			{
			string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			await StartScanAsync(homePath);
			if (AppSettings.HomeOrRoot)
				{
				_btnHome.Enabled = true;
				_btnRoot.Enabled = true;
				}
			else
				{
				_btnHome.Enabled = false;
				_btnRoot.Enabled = false;
				}
			}

		public async Task GoRoot()
			{
			string currentPath = _pathInput.Text;
			string? rootPath = null;

			if (!string.IsNullOrEmpty(currentPath) && currentPath != "This PC")
				{
				try
					{
					rootPath = Path.GetPathRoot(currentPath);
					}
				catch { }
				}

			// Fallback to system drive root if current path is invalid or has no root
			if (string.IsNullOrEmpty(rootPath))
				{
				rootPath = Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\";
				}

			await StartScanAsync(rootPath);

			if (AppSettings.HomeOrRoot)
				{
				_btnHome.Enabled = true;
				_btnRoot.Enabled = true;
				}
			else
				{
				_btnHome.Enabled = false;
				_btnRoot.Enabled = false;
				}
			}

		public async Task GoDrives()
			{
			await StartScanAsync("::DRIVES::");
			}

		public async Task CreateNewFolderAsync()
			{
			string currentPath = _pathInput.Text;
			if (currentPath == "::DRIVES::" || currentPath == "This PC" || !Directory.Exists(currentPath))
				{
				MessageBox.Show("Cannot create a folder in this location.");
				return;
				}

			string? folderName = ShowInputDialog("Enter folder name:", "New Folder");
			if (string.IsNullOrWhiteSpace(folderName)) return;

			string newPath = Path.Combine(currentPath, folderName);
			if (Directory.Exists(newPath))
				{
				MessageBox.Show("Folder already exists.");
				return;
				}

			try
				{
				Directory.CreateDirectory(newPath);
				await StartScanAsync(currentPath);
				}
			catch (Exception ex)
				{
				_logError(ex);
				MessageBox.Show($"Error creating folder: {ex.Message}");
				}
			}

		public async Task CreateNewFileAsync()
			{
			string currentPath = _pathInput.Text;
			if (currentPath == "::DRIVES::" || currentPath == "This PC" || !Directory.Exists(currentPath))
				{
				MessageBox.Show("Cannot create a file in this location.");
				return;
				}

			string? fileName = ShowInputDialog("Enter file name:", "New File");
			if (string.IsNullOrWhiteSpace(fileName)) return;

			string newPath = Path.Combine(currentPath, fileName);
			if (File.Exists(newPath))
				{
				MessageBox.Show("File already exists.");
				return;
				}

			try
				{
				using (File.Create(newPath)) { }
				await StartScanAsync(currentPath);
				}
			catch (Exception ex)
				{
				_logError(ex);
				MessageBox.Show($"Error creating file: {ex.Message}");
				}
			}

		private string? ShowInputDialog(string text, string caption)
			{
			bool isDark = AppSettings.DarkMode;
			Color bgColor = isDark ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
			Color fgColor = isDark ? Color.White : SystemColors.ControlText;
			Color inputBg = isDark ? Color.FromArgb(45, 45, 48) : SystemColors.Window;
			Color btnBg = isDark ? Color.FromArgb(60, 60, 60) : SystemColors.Control;

			Form prompt = new Form()
				{
				Width = 400,
				Height = 160,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				Text = caption,
				StartPosition = FormStartPosition.CenterParent,
				MaximizeBox = false,
				MinimizeBox = false,
				BackColor = bgColor,
				ForeColor = fgColor
				};

			Label textLabel = new Label() { Left = 20, Top = 20, Text = text, Width = 350, ForeColor = fgColor };
			TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 350, BackColor = inputBg, ForeColor = fgColor };
			
			Button confirmation = new Button() 
				{ 
				Text = "OK", 
				Left = 270, 
				Width = 100, 
				Top = 90, 
				DialogResult = DialogResult.OK,
				BackColor = btnBg,
				ForeColor = fgColor,
				FlatStyle = isDark ? FlatStyle.Flat : FlatStyle.Standard
				};
			
			Button cancel = new Button() 
				{ 
				Text = "Cancel", 
				Left = 160, 
				Width = 100, 
				Top = 90, 
				DialogResult = DialogResult.Cancel,
				BackColor = btnBg,
				ForeColor = fgColor,
				FlatStyle = isDark ? FlatStyle.Flat : FlatStyle.Standard
				};

			confirmation.Click += (sender, e) => { prompt.Close(); };
			cancel.Click += (sender, e) => { prompt.Close(); };

			prompt.Controls.Add(textBox);
			prompt.Controls.Add(confirmation);
			prompt.Controls.Add(cancel);
			prompt.Controls.Add(textLabel);
			prompt.AcceptButton = confirmation;
			prompt.CancelButton = cancel;

			return prompt.ShowDialog(_fileList.FindForm()) == DialogResult.OK ? textBox.Text : null;
			}
		}
	}
