using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LIGHTNING.App.UI.ViewModels.Settings;

public sealed partial class SettingsVM : ObservableObject
{
    [ObservableProperty]
    private int selectedTabIndex;

    public SettingsVM()
    {
        GoToInstallSettingsCommand = new RelayCommand(() => SelectedTabIndex = 0);
        GoToDiagnosticsCommand = new RelayCommand(() => SelectedTabIndex = 1);
    }

    public IRelayCommand GoToInstallSettingsCommand { get; }
    public IRelayCommand GoToDiagnosticsCommand { get; }
}
