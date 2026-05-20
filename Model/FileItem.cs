using AsyncExplorer.Services;

namespace AsyncExplorer.Model
    {
    public class FileItem
        {
        required public string Name { get; set; }
        required public string FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public override string ToString()
            {
            return Name;
            }
        }
    }
