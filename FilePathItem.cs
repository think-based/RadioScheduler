      // Be Naame Khoda
// FileName: FilePathItem.cs

using System;
using System.IO;

public class FilePathItem
{
    public string Path { get; set; }
    public string FolderPlayMode { get; set; }
    public string Text { get; set; }
    public string LastPlayedFile { get; set; }

    /// <summary>
    /// Validates the FilePathItem.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrEmpty(Path) && string.IsNullOrEmpty(Text))
        {
            throw new ArgumentException("Either Path or Text must be provided.");
        }

        if (!string.IsNullOrEmpty(Path) && Directory.Exists(Path) && string.IsNullOrEmpty(FolderPlayMode))
        {
            throw new ArgumentException("FolderPlayMode must be set for folders.");
        }
        if (!string.IsNullOrEmpty(Path) && Directory.Exists(Path) && !(FolderPlayMode == "All" || FolderPlayMode == "Random" || FolderPlayMode == "Que"))
        {
            throw new ArgumentException("FolderPlayMode must be  'All' or 'Random' or 'Que' for folders.");
        }

        if (!string.IsNullOrEmpty(Path) && !Directory.Exists(Path) && !File.Exists(Path))
        {
            throw new ArgumentException($"File or folder not found: {Path}");
        }
    }
}