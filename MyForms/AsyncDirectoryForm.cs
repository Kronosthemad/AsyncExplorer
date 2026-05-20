using System.Diagnostics;
using AsyncExplorer.Model;
using AsyncExplorer.Services;

namespace AsyncExplorer
	{
	public class AsyncDirectoryForm : Form
		{
		private ListView _fileList;
		private ImageList _smallIcons;
		private Button _btnScan;
		private Button _btnhome;
		private Button _btnDrives;
		private Button _btnBack;
		private Button _btnRoot;
		private Button _btncancel;
		private TextBox _pathInput;
		private Label _statusLabel;
		private Stack<string> _navigationStack = new Stack<string>();
		private Panel _searchpanel;
		private Panel _infopanel;
		private FlowLayoutPanel _sidepannel;
		private SplitContainer _mainSplit;
		private AsyncDirectoryController _controller;
		private ContextMenuStrip _contextMenu;

		public AsyncDirectoryForm()
			{
			this.Text = "Async Internal Explorer";
			this.Size = new Size(700, 550);

			// Setup ImageList
			_smallIcons = new ImageList
				{
				ColorDepth = ColorDepth.Depth32Bit,
				ImageSize = new Size(16, 16)
				};

			// Setup Context Menu
			_contextMenu = new ContextMenuStrip();
			var settingsItem = new ToolStripMenuItem("Settings...", null, settingsMenuItem_Click);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
			var refreshItem = new ToolStripMenuItem("Refresh", null, async (s, e) => await StartScanAsync(_pathInput.Text));
#pragma warning restore CS8602 // Dereference of a possibly null reference.

			var newItem = new ToolStripMenuItem("New");
			var newFolderItem = new ToolStripMenuItem("Folder", null, async (s, e) => await CreateNewFolderAsync());
			var newFileItem = new ToolStripMenuItem("File", null, async (s, e) => await CreateNewFileAsync());
			newItem.DropDownItems.Add(newFolderItem);
			newItem.DropDownItems.Add(newFileItem);

			var openItem = new ToolStripMenuItem("Open", null, (s, e) =>
				{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
					if (_fileList.SelectedItems.Count > 0 && _fileList.SelectedItems[0].Tag is FileItem selectedItem)
						{
						_controller.OpenSelectedItem(selectedItem);
						}
#pragma warning restore CS8602 // Dereference of a possibly null reference.
				});
			_contextMenu.Items.Add(settingsItem);
			_contextMenu.Items.Add(refreshItem);
			_contextMenu.Items.Add(newItem);
			_contextMenu.Items.Add(openItem);
			this.ContextMenuStrip = _contextMenu;

			// 1. Setup the Top Panel
			_searchpanel = new Panel
				{
				Height = 60,
				Dock = DockStyle.Top,
				Padding = new Padding(10),
				// Add a top margin so the search panel sits lower inside the right pane
				Margin = new Padding(10, 50, 10, 10)
				};

			// Use a FlowLayoutPanel so buttons stack vertically in a list on the left
			_sidepannel = new FlowLayoutPanel
				{
				Width = 60,
				Dock = DockStyle.Fill,
				Padding = new Padding(10),
				AutoScroll = true,
				FlowDirection = FlowDirection.TopDown,
				WrapContents = false
				};

			// 2. Setup the Bottom Panel
			_infopanel = new Panel
				{
				Height = 60,
				Dock = DockStyle.Bottom,
				Padding = new Padding(10)
				};

			// 3. Setup the ListView
			_fileList = new ListView
				{
				Dock = DockStyle.Fill,
				View = View.Details,
				FullRowSelect = true,
				HeaderStyle = ColumnHeaderStyle.None,
				SmallImageList = _smallIcons,
				MultiSelect = false,
				ContextMenuStrip = _contextMenu
				};
			_fileList.Columns.Add("Name", -2); // -2 means auto-fill

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
				Height = 36,
				Width = 100,
				Margin = new Padding(0, 0, 0, 6)
				};

			// Drives button
			_btnDrives = new Button
				{
				Text = "This PC",
				Height = 36,
				Width = 100,
				Margin = new Padding(0, 0, 0, 6)
				};

			// 7. Setup Back button
			_btnBack = new Button
				{
				Text = "Back",
				Height = 36,
				Width = 100,
				Margin = new Padding(0, 50, 0, 6) // add top margin so this first visible button isn't clipped
				};

			// 8. Setup Root button
			_btnRoot = new Button
				{
				Text = "Root",
				Height = 36,
				Width = 100,
				Margin = new Padding(0, 0, 0, 6),
				Enabled = true
				};

			_btncancel = new Button
				{
				Text = "Cancel",
				Width = 100,
				Enabled = true,
				Dock = DockStyle.Right
				};

			// 9. Setup Status Label
			_statusLabel = new Label
				{
				Text = "Ready",
				Dock = DockStyle.Bottom,
				AutoSize = false,
				Height = 20
				};

#pragma warning disable CS8602 // Dereference of a possibly null reference.
			// Wire up events (these will delegate to the controller)
			_btnScan.Click += async (s, e) => await _controller.StartScanAsync(_pathInput.Text);
			_btnhome.Click += async (s, e) => await _controller.GoHome();
			_btnDrives.Click += async (s, e) => await _controller.GoDrives();
			_btnBack.Click += async (s, e) => await _controller.GoBack();
			_btnRoot.Click += async (s, e) => await _controller.GoRoot();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
			_btncancel.Click += (s, e) => BtnCancel_Click(s);

			// --- Adding Controls to Panels ---
			_searchpanel.Controls.Add(_btnScan);
			_searchpanel.Controls.Add(_pathInput);

			_infopanel.Controls.Add(_statusLabel);
			_infopanel.Controls.Add(_btncancel);

			_sidepannel.Controls.Add(_btnBack);
			_sidepannel.Controls.Add(_btnRoot);
			_sidepannel.Controls.Add(_btnhome);
			_sidepannel.Controls.Add(_btnDrives);

			// Create a SplitContainer so the left side panel never overlaps the file list.
			// We'll set a small Panel1MinSize so the user can resize; the initial
			// SplitterDistance is applied later in Load to account for final layout.
			_mainSplit = new SplitContainer
				{
				Dock = DockStyle.Fill,
				Orientation = Orientation.Vertical,
				SplitterDistance = 140,
				IsSplitterFixed = false,
				Panel1MinSize = 30
				};

			// Put the side panel in the left pane. Add the search panel to the right pane
			// above the file list so it won't overlap the list.
			_mainSplit.Panel1.Controls.Add(_sidepannel);
			// Create a container for the right pane so the search panel docks above the file list
			var rightContainer = new Panel
				{
				Dock = DockStyle.Fill
				};

			// Ensure search panel docks to top inside the right container and file list fills below it
			_searchpanel.Dock = DockStyle.Top;
			_fileList.Dock = DockStyle.Fill;

			rightContainer.Controls.Add(_fileList);
			rightContainer.Controls.Add(_searchpanel);

			_mainSplit.Panel2.Controls.Add(rightContainer);

			// Instantiate controller now that controls are created
			_controller = new AsyncDirectoryController(_fileList, _pathInput, _statusLabel,
				_btnScan, _btnBack, _btnhome, _btnRoot, LogError);

			// Let controller wire-up listbox double-click
			_controller.SetupListView();

			// Add controls to the form: main split fills, then info panel docks to bottom
			this.Controls.Add(_mainSplit);   // Fill remaining
			this.Controls.Add(_infopanel);   // Bottom

			ApplyTheme();
			this.Load += Utility_Load;
			}

		private void ApplyTheme()
			{
			bool isDark = AppSettings.DarkMode;
			Color bgColor = isDark ? Color.FromArgb(30, 30, 30) : SystemColors.Control;
			Color fgColor = isDark ? Color.White : SystemColors.ControlText;
			Color listBg = isDark ? Color.FromArgb(45, 45, 48) : SystemColors.Window;
			Color btnBg = isDark ? Color.FromArgb(60, 60, 60) : SystemColors.Control;

			this.BackColor = bgColor;
			this.ForeColor = fgColor;

			_searchpanel.BackColor = bgColor;
			_infopanel.BackColor = bgColor;
			_sidepannel.BackColor = bgColor;

			_fileList.BackColor = listBg;
			_fileList.ForeColor = fgColor;

			_pathInput.BackColor = listBg;
			_pathInput.ForeColor = fgColor;

			_statusLabel.ForeColor = fgColor;

			foreach (Control c in new Control[] { _btnScan, _btnhome, _btnDrives, _btnBack, _btnRoot, _btncancel })
				{
				c.BackColor = btnBg;
				c.ForeColor = fgColor;
				if (c is Button btn)
					{
					btn.FlatStyle = isDark ? FlatStyle.Flat : FlatStyle.Standard;
					btn.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
					}
				}

			// Ensure Cancel button is always red
			_btncancel.BackColor = isDark ? Color.FromArgb(180, 0, 0) : Color.Tomato;
			_btncancel.ForeColor = Color.White;
			}

		private void SetInitialSplitterDistance()
			{
			// Compute desired left pane width based on button widths + padding so it only
			// starts as wide as the buttons inside it.
			int maxButtonWidth = 0;
			foreach (Control c in new Control[] { _btnBack, _btnRoot, _btnhome, _btnDrives })
				{
				if (c != null)
					{
					maxButtonWidth = Math.Max(maxButtonWidth, c.Width);
					}
				}
			int desiredLeftWidth = maxButtonWidth + _sidepannel.Padding.Left + _sidepannel.Padding.Right + 8; // small buffer
																											  // Account for a possible vertical scrollbar
			desiredLeftWidth += System.Windows.Forms.SystemInformation.VerticalScrollBarWidth;

			if (desiredLeftWidth < 60) desiredLeftWidth = 60;

			// Use BeginInvoke to set the splitter distance after initial layout so the
			// control sizes are accurate and the splitter remains adjustable.
			this.BeginInvoke(() =>
			{
				_mainSplit.SplitterDistance = desiredLeftWidth;
				_mainSplit.Panel1MinSize = 30; // keep user adjustment available
			});
			}

		private async Task StartScanAsync(string path, bool isBackNavigation = false)
			{
			// Delegate to controller
			await _controller.StartScanAsync(path, isBackNavigation);
			}

		private async Task CreateNewFolderAsync()
			{
			await _controller.CreateNewFolderAsync();
			}

		private async Task CreateNewFileAsync()
			{
			await _controller.CreateNewFileAsync();
			}

		private async void Utility_Load(object? sender, EventArgs e)
			{
			string homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			_pathInput.Text = homePath;
			await StartScanAsync(homePath);
			SetInitialSplitterDistance();
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

		private void BtnCancel_Click(object? sender)
			{
			this.Close();
			}

		private async void settingsMenuItem_Click(object? sender, EventArgs e)
			{
			using (var diag = new SettingsForm())
				{
				if (diag.ShowDialog() == DialogResult.OK)
					{
					ApplyTheme();
					// Refresh the current view if settings were saved
					await StartScanAsync(_pathInput.Text);
					}
				}
			}
		}
	}
