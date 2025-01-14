// Be Naame Khoda
// FileName: FilePathItem.cs

using System;
using System.IO;

namespace RadioScheduler.Entities
{
    public class FilePathItem
    {
        public string Path { get; set; } // Path to the file or folder
        public string FolderPlayMode { get; set; } // Play mode for folders (e.g., "All" or "Single")

        /// <summary>
        /// Validates the FilePathItem.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(Path))
            {
                throw new ArgumentException("Path cannot be null or empty.");
            }

            if (Directory.Exists(Path) && string.IsNullOrEmpty(FolderPlayMode))
            {
                throw new ArgumentException("FolderPlayMode must be set for folders.");
            }

            if (!Directory.Exists(Path) && !File.Exists(Path))
            {
                throw new ArgumentException($"File or folder not found: {Path}");
            }
        }
    }
}