using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace LIGHTNING.App.ViewModels;

public sealed class DesignSetupVM
{
    public DesignSetupVM()
    {
        // No-op commands (designer wants ICommand; these keep bindings happy)
        BrowseMetSourceDirCommand = new RelayCommand(() => MetSourceDir = MetSourceDir);
        BrowseAfRepoDirCommand = new RelayCommand(() => AfRepoDir = AfRepoDir);
        BrowseAppWorkDirCommand = new RelayCommand(() => AppWorkDir = AppWorkDir);
        OpenConfigFolderCommand = new RelayCommand(() => _ = ConfigPath);
    }

    public string Status { get; set; } = "Config loaded. Changes auto-save when valid.";

    // MUST be settable to satisfy TwoWay TextBox bindings in the designer (XDG0010)
    public string MetSourceDir { get; set; } = @"C:\met-source";
    public string AfRepoDir { get; set; } = @"C:\repos\af";
    public string AppWorkDir { get; set; } = @"C:\work\lightning";

    public string ConfigPath { get; set; } =
        @"C:\Users\<USER>\AppData\Local\EmpyreanCodex\LIGHTNING\config.yml";

    public ObservableCollection<string> ValidationErrors { get; } = new()
    {
        "Example error: MetSourceDir and AfRepoDir must not overlap.",
        "Example error: AfRepoDir does not exist: C:\\repos\\af"
    };

    public string ValidationSummary =>
        ValidationErrors.Count == 0
            ? "✅ Valid (saved 2025-12-15 07:12:03)."
            : "❌ Invalid (not saved).";

    public ICommand BrowseMetSourceDirCommand { get; }
    public ICommand BrowseAfRepoDirCommand { get; }
    public ICommand BrowseAppWorkDirCommand { get; }
    public ICommand OpenConfigFolderCommand { get; }
}
