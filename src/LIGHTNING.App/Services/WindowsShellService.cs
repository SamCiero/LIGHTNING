using System;
using System.Diagnostics;
using System.IO;

namespace LIGHTNING.App.Services;

public sealed class WindowsShellService : IShellService
{
    public void OpenFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return;

        string full = Path.GetFullPath(folderPath);
        Directory.CreateDirectory(full);

        Process.Start(new ProcessStartInfo
        {
            FileName = full,
            UseShellExecute = true
        });
    }
}
