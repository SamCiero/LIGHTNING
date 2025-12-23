using System;
using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LIGHTNING.App.ViewModels;

/// <summary>
/// Window-level view model: owns navigation and top-level commands.
/// Keeps tab switching and menu/toolbar commands out of individual tab VMs.
/// </summary>
public sealed class MainVM : ObservableObject
{
    private readonly SetupVM _setup;

    private int _selectedTabIndex;

    public MainVM(SetupVM setup)
    {
        _setup = setup ?? throw new ArgumentNullException(nameof(setup));

        // When SetupVM.Status changes, notify bindings that MainVM.Status changed too.
        _setup.PropertyChanged += OnSetupPropertyChanged;

        GoToSettingsCommand = new RelayCommand(() => SelectedTabIndex = 0);
        GoToInstallSettingsCommand = new RelayCommand(() => SelectedTabIndex = 1);
        GoToDiagnosticsCommand = new RelayCommand(() => SelectedTabIndex = 2);

        // Delegate commands you already have on SetupVM.
        OpenConfigFolderCommand = new RelayCommand(() =>
        {
            if (_setup.OpenConfigFolderCommand.CanExecute(null))
                _setup.OpenConfigFolderCommand.Execute(null);
        });

        // “Revalidate” in your current codebase can reasonably be “run the current validation pass again”.
        // If you later add SetupVM.RevalidateCommand, just delegate to it the same way.
        RevalidateCommand = new RelayCommand(() =>
        {
            // Minimal behavior today: re-run the same logic by triggering Initialize,
            // which loads+validates and then calls RevalidateAndMaybeSave.
            _setup.Initialize();
        });

        ExitCommand = new RelayCommand(() => Application.Current.Shutdown());

        AboutCommand = new RelayCommand(() =>
        {
            MessageBox.Show(
                "LIGHTNING\n\nLocally-Integrated, Git-Hosted Tool: Normalized Indexing/Naming Generator",
                "About LIGHTNING",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        });
    }

    /// <summary>
    /// Selected tab index in the MainWindow TabControl.
    /// </summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set => SetProperty(ref _selectedTabIndex, value);
    }

    /// <summary>
    /// Window-level status string. Mirrors SetupVM.Status for now.
    /// </summary>
    public string Status => _setup.Status;

    // Top-level commands (menu/toolbar)
    public IRelayCommand GoToSettingsCommand { get; }
    public IRelayCommand GoToInstallSettingsCommand { get; }
    public IRelayCommand GoToDiagnosticsCommand { get; }

    public IRelayCommand OpenConfigFolderCommand { get; }
    public IRelayCommand RevalidateCommand { get; }

    public IRelayCommand ExitCommand { get; }
    public IRelayCommand AboutCommand { get; }

    private void OnSetupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SetupVM.Status))
        {
            OnPropertyChanged(nameof(Status));
        }
    }
}
