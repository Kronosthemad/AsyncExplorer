namespace AsyncExplorer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.ThreadException += (s, e) =>
            {
                new AsyncDirectoryForm().LogError(e.Exception);
                MessageBox.Show($"UI error: {e.Exception.Message}");
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Exception ex = e.ExceptionObject as Exception ?? new Exception("Unknown error");
                new AsyncDirectoryForm().LogError(ex);
                MessageBox.Show($"Unhandled error: {ex.Message}");
            };

            ApplicationConfiguration.Initialize();
            Application.Run(new AsyncDirectoryForm());
        }
    }
}
