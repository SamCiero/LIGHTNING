using Ookii.Dialogs.Wpf;

namespace LIGHTNING.App.Services;

public sealed class OokiiDialogService : IDialogService
{
    public string? PickFolder(string title)
    {
        var dlg = new VistaFolderBrowserDialog
        {
            Description = title,
            UseDescriptionForTitle = true
        };

        return dlg.ShowDialog() == true ? dlg.SelectedPath : null;
    }
}
