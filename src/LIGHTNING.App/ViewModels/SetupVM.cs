using System;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LIGHTNING.Adapters.Filesystem;
using LIGHTNING.App.Services;
using LIGHTNING.Core.Config;
using LIGHTNING.Core.Policies;
using LIGHTNING.Core.Services;

namespace LIGHTNING.App.ViewModels;

public sealed partial class SetupVM : ObservableObject
{
    private readonly IConfigStore _configStore;
    private readonly IDialogService _dialogs;
    private readonly IShellService _shell;
    private readonly IMutableBoundaryFileSystemProvider _fsProvider;
    private readonly IAppPaths _paths;

    private bool _suspendAutoSave;

    private string _metSourceDir = string.Empty;
    private string _afRepoDir = string.Empty;
    private string _appWorkDir = string.Empty;

    private bool _enableVsCodeIntegration;

    private string _status = "Not saved yet.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValidationSummary))]
    private bool isValid;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValidationSummary))]
    private DateTime? lastSavedAt;

    public SetupVM(
        IConfigStore configStore,
        IDialogService dialogs,
        IShellService shell,
        IMutableBoundaryFileSystemProvider fsProvider,
        IAppPaths paths)
    {
        _configStore = configStore;
        _dialogs = dialogs;
        _shell = shell;
        _fsProvider = fsProvider;
        _paths = paths;

        BrowseMetSourceDirCommand = new RelayCommand(BrowseMetSourceDir);
        BrowseAfRepoDirCommand = new RelayCommand(BrowseAfRepoDir);
        BrowseAppWorkDirCommand = new RelayCommand(BrowseAppWorkDir);
        OpenConfigFolderCommand = new RelayCommand(OpenConfigFolder);

        ValidationErrors = new ObservableCollection<string>();
        ConfigPath = _paths.ConfigPath;
    }

    public string MetSourceDir
    {
        get => _metSourceDir;
        set
        {
            string next = (value ?? string.Empty).Trim();
            if (SetProperty(ref _metSourceDir, next))
                OnConfigChanged();
        }
    }

    public string AfRepoDir
    {
        get => _afRepoDir;
        set
        {
            string next = (value ?? string.Empty).Trim();
            if (SetProperty(ref _afRepoDir, next))
                OnConfigChanged();
        }
    }

    public string AppWorkDir
    {
        get => _appWorkDir;
        set
        {
            string next = (value ?? string.Empty).Trim();
            if (SetProperty(ref _appWorkDir, next))
                OnConfigChanged();
        }
    }

    /// <summary>
    /// Toggle for the optional VS Code integration.  Changing this value triggers
    /// immediate revalidation and auto‑save when the config is valid.
    /// </summary>
    public bool EnableVsCodeIntegration
    {
        get => _enableVsCodeIntegration;
        set
        {
            if (SetProperty(ref _enableVsCodeIntegration, value))
                OnConfigChanged();
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public string ConfigPath { get; }

    public string ValidationSummary =>
        !IsValid
            ? "❌ Invalid (not saved)."
            : LastSavedAt is null
                ? $"✅ Valid (will save to: {ConfigPath})"
                : $"✅ Valid (saved {LastSavedAt:yyyy-MM-dd HH:mm:ss}).";

    public ObservableCollection<string> ValidationErrors { get; }

    public IRelayCommand BrowseMetSourceDirCommand { get; }
    public IRelayCommand BrowseAfRepoDirCommand { get; }
    public IRelayCommand BrowseAppWorkDirCommand { get; }
    public IRelayCommand OpenConfigFolderCommand { get; }

    public void Initialize()
    {
        _suspendAutoSave = true;

        bool hadParseError = false;

        LightningConfig? cfg = null;
        try
        {
            cfg = _configStore.TryLoad();
        }
        catch (Exception ex)
        {
            hadParseError = true;
            // Surface parse errors as status; leave cfg null so defaults apply
            Status = $"Failed to parse config: {ex.Message}";
        }

        try
        {
            if (cfg is not null)
            {
                MetSourceDir = cfg.MetSourceDir ?? string.Empty;
                AfRepoDir = cfg.AfRepoDir ?? string.Empty;
                AppWorkDir = cfg.AppWorkDir ?? string.Empty;
                EnableVsCodeIntegration = cfg.EnableVsCodeIntegration;

                if (_fsProvider.Current.FileExists(_paths.ConfigPath))
                    LastSavedAt = _fsProvider.Current.GetLastWriteTime(_paths.ConfigPath);
            }

            if (string.IsNullOrWhiteSpace(AppWorkDir))
                AppWorkDir = Path.Combine(_paths.ConfigDirectory, "work");

            if (!hadParseError)
            {
                Status = cfg is null
                    ? "No config found yet. Pick folders to create one."
                    : "Config loaded. Changes auto-save when valid.";
            }
        }
        finally
        {
            _suspendAutoSave = false;
        }

        RevalidateAndMaybeSave();
    }

    private void BrowseMetSourceDir()
    {
        string? picked = _dialogs.PickFolder("Select MET_SOURCE_DIR");
        if (!string.IsNullOrWhiteSpace(picked))
            MetSourceDir = picked;
    }

    private void BrowseAfRepoDir()
    {
        string? picked = _dialogs.PickFolder("Select AF_REPO_DIR");
        if (!string.IsNullOrWhiteSpace(picked))
            AfRepoDir = picked;
    }

    private void BrowseAppWorkDir()
    {
        string? picked = _dialogs.PickFolder("Select APP_WORK_DIR");
        if (!string.IsNullOrWhiteSpace(picked))
            AppWorkDir = picked;
    }

    private void OpenConfigFolder()
    {
        string? dir = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrWhiteSpace(dir))
            _shell.OpenFolder(dir);
    }

    private void OnConfigChanged()
    {
        if (_suspendAutoSave)
            return;

        RevalidateAndMaybeSave();
    }

    private void RevalidateAndMaybeSave()
    {
        LightningConfig cfg = new()
        {
            Version = 1,
            MetSourceDir = MetSourceDir,
            AfRepoDir = AfRepoDir,
            AppWorkDir = AppWorkDir,
            EnableVsCodeIntegration = EnableVsCodeIntegration
        };

        ValidationErrors.Clear();
        var errors = LightningConfigValidator.Validate(cfg);
        foreach (string err in errors)
            ValidationErrors.Add(err);

        IsValid = ValidationErrors.Count == 0;

        if (!IsValid)
        {
            Status = "Not saved (config invalid).";
            return;
        }

        // Build runtime boundary with config dir and allowed roots
        IBoundaryPolicy runtimePolicy = new BoundaryPolicy(new[]
        {
            _paths.ConfigDirectory,
            cfg.MetSourceDir,
            cfg.AfRepoDir,
            cfg.AppWorkDir
        });

        BoundaryFileSystem runtimeFs = new BoundaryFileSystem(runtimePolicy);

        // Ensure work directory exists (and thus is allowed)
        runtimeFs.EnsureDirectoryExists(cfg.AppWorkDir);

        // Update global FS and save config
        _fsProvider.SetCurrent(runtimeFs);
        _configStore.Save(cfg);

        LastSavedAt = DateTime.Now;
        Status = $"Auto-saved at {LastSavedAt:HH:mm:ss}.";
    }
}
