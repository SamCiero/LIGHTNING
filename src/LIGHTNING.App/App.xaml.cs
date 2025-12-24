// src/LIGHTNING.App/App.xaml.cs
using System.Windows;
using LIGHTNING.Adapters.Config;
using LIGHTNING.Adapters.Filesystem;
using LIGHTNING.App.Services;
using LIGHTNING.App.UI.ViewModels.SetupWizard;
using LIGHTNING.App.UI.ViewModels.Shell;
using LIGHTNING.App.UI.Views.Shell;
using LIGHTNING.Core.Policies;
using LIGHTNING.Core.Services;

namespace LIGHTNING.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnMainWindowClose;

        IAppPaths paths = new DefaultAppPaths();

        // Initial boundary: AppData only (so config can load on first run).
        IBoundaryPolicy configOnlyPolicy = new BoundaryPolicy(new[] { paths.ConfigDirectory });
        BoundaryFileSystem configOnlyFs = new BoundaryFileSystem(configOnlyPolicy);

        IMutableBoundaryFileSystemProvider fsProvider = new MutableBoundaryFileSystemProvider(configOnlyFs);

        IConfigStore configStore = new YamlConfigStore(fsProvider, paths);
        IDialogService dialogs = new OokiiDialogService();
        IShellService shell = new WindowsShellService();

        var setupVm = new SetupWizardVM(configStore, dialogs, shell, fsProvider, paths);
        setupVm.Initialize();

        var shellVm = new ShellVM(setupVm);

        var window = new ShellWindow { DataContext = shellVm };
        MainWindow = window;
        window.Show();
    }
}
