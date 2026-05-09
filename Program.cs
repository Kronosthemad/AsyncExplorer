using System.Diagnostics;
namespace AsyncExplorer;

public class FileItem
    {
    required public string Name { get; set; }
    required public string FullPath { get; set; }
    public bool IsDirectory { get; set; }

    public override string ToString() => $"{(IsDirectory ? "[DIR] " : "[FILE]")} {Name}";
    }
public class AsyncDirectoryUtility : Form
    {
    private ListBox _fileList;
    private Button _btnScan;
    private TextBox _pathInput;
    private Label _statusLabel;

    public AsyncDirectoryUtility()
        {
        this.Text = "Async Internal Explorer";
        this.Size = new Size(500, 450);

        _pathInput = new TextBox { Text = @"C:\Windows", Location = new Point(10, 10), Width = 360 };
        _btnScan = new Button { Text = "Scan", Location = new Point(380, 10), Width = 80 };

        // Wire up the async click event
        _btnScan.Click += async (s, e) => await StartScanAsync(_pathInput.Text);

        _fileList = new ListBox
            {
            Location = new Point(10, 45),
            Size = new Size(460, 300),
            Font = new Font("Consolas", 9)
            };

        _statusLabel = new Label
            {
            Text = "Ready",
            Location = new Point(10, 355),
            AutoSize = true
            };

        this.Controls.AddRange(new Control[] { _pathInput, _btnScan, _fileList, _statusLabel });
        }

    private async Task StartScanAsync(string path)
        {
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

    private async void OnItemDoubleClick(object sender, MouseEventArgs e)
        {
        // 1. Get the item that was clicked
        int index = _fileList.IndexFromPoint(e.Location);
        if (index == ListBox.NoMatches) return;

        var item = (FileItem)_fileList.Items[index];

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
            }
        }


    private void PerformFilesystemWork(string path, IProgress<FileItem> progress)
        {
        DirectoryInfo dir = new DirectoryInfo(path);
        if (!dir.Exists) return;

        foreach (var info in dir.GetFileSystemInfos())
            {
            progress.Report(new FileItem
                {
                Name = info.Name,
                FullPath = info.FullName,
                IsDirectory = (info.Attributes & FileAttributes.Directory) != 0
                });
            }
        }

    [STAThread]
    static void Main() => Application.Run(new AsyncDirectoryUtility());
    }