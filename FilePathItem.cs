// Be Naame Khoda
// FileName: FilePathItem.cs

using System;

namespace RadioScheduler.Entities
{
    public class FilePathItem
    {
        public string Path { get; set; } // Path to the file or folder
        public string FolderPlayMode { get; set; } // Play mode for folders (e.g., "All" or "Single")
        public string Text { get; set; } // Text for TTS (optional)
        public TimeSpan Duration { get; set; } // Duration of the file or TTS

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

            if (!string.IsNullOrEmpty(Path) && !Directory.Exists(Path) && !File.Exists(Path))
            {
                throw new ArgumentException($"File or folder not found: {Path}");
            }
        }
    }
}