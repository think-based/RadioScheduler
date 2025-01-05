//Be Naame Khoda
//FileName: FileHashHelper.cs

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class FileHashHelper
{
    public static string CalculateFileHash(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}