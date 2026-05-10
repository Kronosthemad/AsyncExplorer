namespace AsyncExplorer
    {
    public class FileItem
        {
        required public string Name { get; set; }
        required public string FullPath { get; set; }
        public bool IsDirectory { get; set; }

        public override string ToString() => $"{(IsDirectory ? "[DIR] " : "[FILE]")} {Name}";
        }
    }