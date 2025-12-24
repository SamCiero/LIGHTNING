using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LIGHTNING.App.UI.ViewModels.SetupWizard;
using LIGHTNING.App.UI.ViewModels.Settings;

namespace LIGHTNING.App.UI.ViewModels.Shell;

public sealed class ShellVM : ObservableObject, IDisposable
{
    private const int StatusMaxChars = 512;

    private readonly SetupWizardVM _setup;
    private readonly SettingsVM _settings;

    private object _currentPage;
    private bool _isSetupConfirmed;
    private bool _disposed;

    private readonly RelayCommand _confirmSetupCommand;

    public ShellVM(SetupWizardVM setup)
    {
        _setup = setup ?? throw new ArgumentNullException(nameof(setup));
        _settings = new SettingsVM();

        _currentPage = _setup;               // startup: SetupWizard is the only page
        _isSetupConfirmed = false;

        _setup.PropertyChanged += OnSetupPropertyChanged;

        // If ValidationErrors is observable, keep Confirm enabled state live.
        try
        {
            var errors = _setup.ValidationErrors;
            if (errors is INotifyCollectionChanged ncc)
                ncc.CollectionChanged += (_, __) => OnSetupReadinessChanged();
        }
        catch
        {
            // If SetupWizardVM doesn't expose ValidationErrors as a property,
            // remove the readiness gating below and wire it to a property you do have.
        }

        GoToSettingsCommand = new RelayCommand(
            execute: () => CurrentPage = _settings,
            canExecute: () => IsSetupConfirmed);

        GoToInstallSettingsCommand = new RelayCommand(
            execute: () =>
            {
                CurrentPage = _settings;
                _settings.SelectedTabIndex = 0;
            },
            canExecute: () => IsSetupConfirmed);

        GoToDiagnosticsCommand = new RelayCommand(
            execute: () =>
            {
                CurrentPage = _settings;
                _settings.SelectedTabIndex = 1;
            },
            canExecute: () => IsSetupConfirmed);

        OpenConfigFolderCommand = new RelayCommand(() =>
        {
            if (_setup.OpenConfigFolderCommand.CanExecute(null))
                _setup.OpenConfigFolderCommand.Execute(null);
        });

        RevalidateCommand = new RelayCommand(() => _setup.Initialize());

        ExitCommand = new RelayCommand(() => Application.Current.Shutdown());

        AboutCommand = new RelayCommand(() =>
        {
            MessageBox.Show(
                "LIGHTNING\n\nLocally-Integrated, Git-Hosted Tool: Normalized Indexing/Naming Generator",
                "About LIGHTNING",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        });

        _confirmSetupCommand = new RelayCommand(
            execute: ConfirmSetup,
            canExecute: CanConfirmSetup);

        ConfirmSetupCommand = _confirmSetupCommand;
    }

    /// <summary>Current page VM (SetupWizardVM, SettingsVM, later DashboardVM).</summary>
    public object CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    /// <summary>
    /// Setup gate: until confirmed, user cannot navigate away from SetupWizard.
    /// (We’ll wire this to “valid + saved + user clicked Confirm” properly next.)
    /// </summary>
    public bool IsSetupConfirmed
    {
        get => _isSetupConfirmed;
        private set
        {
            if (SetProperty(ref _isSetupConfirmed, value))
            {
                OnPropertyChanged(nameof(SetupMenuVisibility));
                NotifyNavCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Temporary: show a Setup menu only while setup is not confirmed.
    /// Remove once Confirm lives inside the SetupWizard page.
    /// </summary>
    public Visibility SetupMenuVisibility => IsSetupConfirmed ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>Status line (clamped for UI safety).</summary>
    public string Status => ClampStatus(_setup.Status);

    // Commands
    public IRelayCommand GoToSettingsCommand { get; }
    public IRelayCommand GoToInstallSettingsCommand { get; }
    public IRelayCommand GoToDiagnosticsCommand { get; }

    public IRelayCommand ConfirmSetupCommand { get; }

    public IRelayCommand OpenConfigFolderCommand { get; }
    public IRelayCommand RevalidateCommand { get; }

    public IRelayCommand ExitCommand { get; }
    public IRelayCommand AboutCommand { get; }

    private bool CanConfirmSetup()
    {
        // Current gating: “valid” = no validation errors.
        // TODO: tighten to “valid AND saved” once SetupWizardVM exposes an explicit IsSaved/LastSavedUtc.
        try
        {
            return _setup.ValidationErrors.Count == 0;
        }
        catch
        {
            return false;
        }
    }

    private void ConfirmSetup()
    {
        // This is the “user clicked Confirm” latch.
        // Next step: you’ll also ensure SetupWizardVM actually saved the config before enabling this.
        IsSetupConfirmed = true;

        // After confirmation, default to Settings for now (until Dashboard exists).
        CurrentPage = _settings;
    }

    private void NotifyNavCanExecuteChanged()
    {
        // CommunityToolkit RelayCommand supports NotifyCanExecuteChanged.
        (GoToSettingsCommand as RelayCommand)?.NotifyCanExecuteChanged();
        (GoToInstallSettingsCommand as RelayCommand)?.NotifyCanExecuteChanged();
        (GoToDiagnosticsCommand as RelayCommand)?.NotifyCanExecuteChanged();
        _confirmSetupCommand.NotifyCanExecuteChanged();
    }

    private void OnSetupReadinessChanged()
    {
        _confirmSetupCommand.NotifyCanExecuteChanged();
    }

    private void OnSetupPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SetupWizardVM.Status))
            OnPropertyChanged(nameof(Status));

        // If SetupWizardVM later exposes a “Saved/Valid” property, hook it here and call:
        // OnSetupReadinessChanged();
    }

    private static string ClampStatus(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        s = s.Replace("\r", " ").Replace("\n", " ");
        return s.Length <= StatusMaxChars ? s : s[..StatusMaxChars] + "…";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _setup.PropertyChanged -= OnSetupPropertyChanged;
    }
}
