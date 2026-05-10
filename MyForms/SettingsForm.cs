using AsyncExplorer.Model;

namespace AsyncExplorer
	{
	public partial class SettingsForm : Form
		{
		private CheckBox _chkUseEventViewer;
		private CheckBox _chkShowHidden;
		private CheckBox _chkUseIcons;
		private CheckBox _chkHomeOrRoot;
		private Button _btnOK;
		private Label _lblInfo;

		public SettingsForm()
			{
			this.Text = "Settings";
			this.Size = new Size(350, 350);
			this.FormBorderStyle = FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.StartPosition = FormStartPosition.CenterParent;

			_lblInfo = new Label
				{
				Text = "Application Settings:",
				Location = new Point(20, 20),
				AutoSize = true,
				Font = new Font(this.Font, FontStyle.Bold)
				};

			_chkUseEventViewer = new CheckBox
				{
				Text = "Use Windows Event Viewer for errors",
				Location = new Point(20, 50),
				Width = 250,
				Checked = AppSettings.UseEventViewer
				};

			_chkShowHidden = new CheckBox
				{
				Text = "Show Hidden Files",
				Location = new Point(20, 80),
				Width = 250,
				Checked = AppSettings.ShowHiddenFiles
				};

			_chkUseIcons = new CheckBox
				{
				Text = "Use Icons in File List",
				Location = new Point(20, 110),
				Width = 250,
				Checked = AppSettings.UseIcons,
				};

			_chkHomeOrRoot = new CheckBox
				{
				Text = "Show Home/Root in Navigation",
				Location = new Point(20, 140),
				Width = 250,
				Checked = AppSettings.HomeOrRoot
				};

			_btnOK = new Button
				{
				Text = "OK",
				Location = new Point(100, 170),
				DialogResult = DialogResult.OK,
				Size = new Size(80, 30)
				};
			_btnOK.Click += BtnOK_Click;

			this.Controls.Add(_lblInfo);
			this.Controls.Add(_chkUseEventViewer);
			this.Controls.Add(_chkShowHidden);
			this.Controls.Add(_chkUseIcons);
			this.Controls.Add(_chkHomeOrRoot);
			this.Controls.Add(_btnOK);

			this.AcceptButton = _btnOK;
			}

		private void BtnOK_Click(object? sender, EventArgs e)
			{
			AppSettings.UseEventViewer = _chkUseEventViewer.Checked;
			AppSettings.ShowHiddenFiles = _chkShowHidden.Checked;
			AppSettings.UseIcons = _chkUseIcons.Checked;
			this.Close();
			}
		}
	}
