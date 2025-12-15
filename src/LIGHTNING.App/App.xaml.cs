using System.Windows;
using LIGHTNING.Adapters.Config;
using LIGHTNING.Adapters.Filesystem;
using LIGHTNING.App.Services;
using LIGHTNING.App.ViewModels;
using LIGHTNING.Core.Policies;
using LIGHTNING.Core.Services;

namespace LIGHTNING.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        IAppPaths paths = new DefaultAppPaths();

        // Initial boundary: AppData only (so config can load on first run).
        IBoundaryPolicy configOnlyPolicy = new BoundaryPolicy(new[] { paths.ConfigDirectory });
        BoundaryFileSystem configOnlyFs = new BoundaryFileSystem(configOnlyPolicy);

        IMutableBoundaryFileSystemProvider fsProvider = new MutableBoundaryFileSystemProvider(configOnlyFs);

        IConfigStore configStore = new YamlConfigStore(fsProvider, paths);
        IDialogService dialogs = new OokiiDialogService();
        IShellService shell = new WindowsShellService();

        SetupVM setupVm = new SetupVM(configStore, dialogs, shell, fsProvider, paths);
        setupVm.Initialize();

        MainWindow window = new MainWindow { DataContext = setupVm };
        window.Show();
    }
}
