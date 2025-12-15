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

    private string _metSourceDir = "";
    private string _afRepoDir = "";
    private string _appWorkDir = "";

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
            string next = (value ?? "").Trim();
            if (SetProperty(ref _metSourceDir, next))
                OnConfigChanged();
        }
    }

    public string AfRepoDir
    {
        get => _afRepoDir;
        set
        {
            string next = (value ?? "").Trim();
            if (SetProperty(ref _afRepoDir, next))
                OnConfigChanged();
        }
    }

    public string AppWorkDir
    {
        get => _appWorkDir;
        set
        {
            string next = (value ?? "").Trim();
            if (SetProperty(ref _appWorkDir, next))
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

        try
        {
            LightningConfig? cfg = _configStore.TryLoad();

            if (cfg is not null)
            {
                MetSourceDir = cfg.MetSourceDir ?? "";
                AfRepoDir = cfg.AfRepoDir ?? "";
                AppWorkDir = cfg.AppWorkDir ?? "";

                if (_fsProvider.Current.FileExists(_paths.ConfigPath))
                    LastSavedAt = _fsProvider.Current.GetLastWriteTime(_paths.ConfigPath);
            }

            if (string.IsNullOrWhiteSpace(AppWorkDir))
                AppWorkDir = Path.Combine(_paths.ConfigDirectory, "work");

            Status = cfg is null
                ? "No config found yet. Pick folders to create one."
                : "Config loaded. Changes auto-save when valid.";
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
            AppWorkDir = AppWorkDir
        };

        ValidationErrors.Clear();
        var errors = LightningConfigValidator.Validate(cfg);
        for (int i = 0; i < errors.Count; i++)
            ValidationErrors.Add(errors[i]);

        IsValid = ValidationErrors.Count == 0;

        if (!IsValid)
        {
            Status = "Not saved (config invalid).";
            return;
        }

        IBoundaryPolicy runtimePolicy = new BoundaryPolicy(new[]
        {
            _paths.ConfigDirectory,
            cfg.MetSourceDir,
            cfg.AfRepoDir,
            cfg.AppWorkDir
        });

        BoundaryFileSystem runtimeFs = new BoundaryFileSystem(runtimePolicy);

        runtimeFs.EnsureDirectoryExists(cfg.AppWorkDir);

        _fsProvider.SetCurrent(runtimeFs);

        _configStore.Save(cfg);

        LastSavedAt = DateTime.Now;
        Status = $"Auto-saved at {LastSavedAt:HH:mm:ss}.";
    }
}
