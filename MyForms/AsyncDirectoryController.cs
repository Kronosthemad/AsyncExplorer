using System.Diagnostics;
using AsyncExplorer.Model;

namespace AsyncExplorer
	{
	public class AsyncDirectoryController
		{
		private readonly ListBox _fileList;
		private readonly TextBox _pathInput;
		private readonly Label _statusLabel;
		private readonly Button _btnScan;
		private readonly Button _btnBack;
		private readonly Button _btnHome;
		private readonly Button _btnRoot;
		private readonly Action<Exception> _logError;
		private readonly Stack<string> _navigationStack = new Stack<string>();

		public AsyncDirectoryController(ListBox fileList, TextBox pathInput, Label statusLabel,
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

		public void SetupListBox()
			{
			_fileList.MouseDoubleClick -= OnItemDoubleClick;
			_fileList.MouseDoubleClick += OnItemDoubleClick;
			}

		private void OnItemDoubleClick(object? sender, MouseEventArgs e)
			{
			int index = _fileList.IndexFromPoint(e.Location);
			if (index == ListBox.NoMatches) return;

			object? obj = _fileList.Items[index];
			if (obj is not FileItem item) return;

			OpenSelectedItem(item);
			}

		public void OpenSelectedItem(FileItem item)
			{
			try
				{
				if (item.IsDirectory)
					{
					_pathInput.Text = item.FullPath;
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
				_logError(ex);
				_statusLabel.Text = "Error occurred.";
				}
			finally
				{
				_btnScan.Enabled = true;
				SetupListBox();
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
						_logError(uae);
						}
					}
				}
			catch (Exception ex)
				{
				_logError(ex);
				}
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
			string rootPath = Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\";
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
		}
	}
